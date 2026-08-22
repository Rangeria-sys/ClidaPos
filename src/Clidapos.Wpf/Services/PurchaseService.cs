using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class PurchaseLine
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Amount => Qty * Price;
    }

    public class PurchaseResult
    {
        public bool Ok { get; set; }
        public string InvoiceNo { get; set; } = "";
        public decimal GrandTotal { get; set; }
        public string Error { get; set; } = "";
    }

    public class PurchaseService
    {
        private const string DefaultSupplierCode = "SUPP-DEFAULT";
        private readonly WarehouseService _warehouseService = new();
        private readonly SupplierLedgerService _supplierLedgerService = new();

        public async Task<int> EnsureDefaultSupplierAsync()
        {
            using var db = new ClidaposDbContext();

            var existing = await db.Suppliers
                .FirstOrDefaultAsync(s => s.SupplierID.Trim() == DefaultSupplierCode);

            if (existing != null)
                return existing.ID;

            var maxId = await db.Suppliers.Select(s => (int?)s.ID).MaxAsync() ?? 0;

            var newSupplier = new Supplier
            {
                ID = maxId + 1,
                SupplierID = DefaultSupplierCode,
                Name = "Unspecified Supplier"
            };
            db.Suppliers.Add(newSupplier);
            await db.SaveChangesAsync();

            return newSupplier.ID;
        }

        public async Task RecordBuyingPriceAsync(int productId, decimal qty, decimal buyingPrice)
        {
            if (buyingPrice <= 0)
                return;

            var supplierId = await EnsureDefaultSupplierAsync();
            await _warehouseService.EnsureDefaultWarehouseAsync();

            using var db = new ClidaposDbContext();

            var maxPurchaseId = await db.Purchases.Select(p => (int?)p.ST_ID).MaxAsync() ?? 0;
            var total = qty * buyingPrice;

            var purchase = new Purchase
            {
                ST_ID = maxPurchaseId + 1,
                InvoiceNo = $"INIT-{maxPurchaseId + 1}",
                Date = DateTime.Now,
                PurchaseType = "Initial",
                Supplier_ID = supplierId,
                SubTotal = total,
                DiscountPer = 0,
                Discount = 0,
                PreviousDue = 0,
                FreightCharges = 0,
                OtherCharges = 0,
                Total = total,
                RoundOff = 0,
                GrandTotal = total,
                TotalPayment = total,
                PaymentDue = 0
            };
            db.Purchases.Add(purchase);

            var line = new PurchaseJoin
            {
                PurchaseID = purchase.ST_ID,
                ProductID = productId,
                Qty = qty,
                Price = buyingPrice,
                TotalAmount = total,
                Warehouse = WarehouseService.DefaultWarehouseName
            };
            db.PurchaseJoins.Add(line);

            await db.SaveChangesAsync();
        }

        public async Task<decimal?> GetLatestBuyingPriceAsync(int productId)
        {
            using var db = new ClidaposDbContext();

            var latest = await db.PurchaseJoins
                .Where(j => j.ProductID == productId)
                .OrderByDescending(j => j.SP_ID)
                .FirstOrDefaultAsync();

            return latest?.Price;
        }

        // Latest buying price for ALL products in two queries (no per-row round trips).
        public async Task<Dictionary<int, decimal>> GetLatestBuyingPricesAsync()
        {
            using var db = new ClidaposDbContext();

            var latestIds = await db.PurchaseJoins
                .GroupBy(j => j.ProductID)
                .Select(g => g.Max(j => j.SP_ID))
                .ToListAsync();

            return await db.PurchaseJoins
                .Where(j => latestIds.Contains(j.SP_ID))
                .ToDictionaryAsync(j => j.ProductID, j => j.Price);
        }

        /// <summary>
        /// Records a real stock-receiving purchase: writes the Purchase header, one
        /// PurchaseJoin row per line, and adds the received quantity to stock for the
        /// chosen warehouse - all inside one transaction. After the transaction commits,
        /// also posts a real Credit entry to the Supplier Ledger (money now owed) -
        /// this is best-effort and never rolls back or fails the purchase itself.
        /// </summary>
        public async Task<PurchaseResult> SavePurchaseAsync(
            int supplierId,
            string warehouseName,
            string invoiceNo,
            List<PurchaseLine> lines,
            decimal discountPercent,
            decimal freightCharges,
            decimal otherCharges)
        {
            if (lines == null || lines.Count == 0)
                return new PurchaseResult { Ok = false, Error = "Add at least one item to the purchase." };

            if (lines.Any(l => l.Qty <= 0))
                return new PurchaseResult { Ok = false, Error = "Every line needs a quantity above zero." };

            if (string.IsNullOrWhiteSpace(invoiceNo))
                return new PurchaseResult { Ok = false, Error = "Invoice number is required." };

            if (string.IsNullOrWhiteSpace(warehouseName))
                return new PurchaseResult { Ok = false, Error = "Pick a warehouse to receive the stock into." };

            using var db = new ClidaposDbContext();
            using var tx = await db.Database.BeginTransactionAsync();

            string? supplierCode = null;
            string? supplierName = null;

            try
            {
                var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.ID == supplierId);
                supplierCode = supplier?.SupplierID.Trim();
                supplierName = supplier?.Name.Trim();

                var subtotal = Math.Round(lines.Sum(l => l.Amount), 2);
                var discountAmount = Math.Round(subtotal * discountPercent / 100m, 2);
                var total = Math.Round(subtotal - discountAmount + freightCharges + otherCharges, 2);

                var maxPurchaseId = await db.Purchases.Select(p => (int?)p.ST_ID).MaxAsync() ?? 0;

                var purchase = new Purchase
                {
                    ST_ID = maxPurchaseId + 1,
                    InvoiceNo = invoiceNo.Trim(),
                    Date = DateTime.Now,
                    PurchaseType = "Restock",
                    Supplier_ID = supplierId,
                    SubTotal = subtotal,
                    DiscountPer = discountPercent,
                    Discount = discountAmount,
                    PreviousDue = 0,
                    FreightCharges = freightCharges,
                    OtherCharges = otherCharges,
                    Total = total,
                    RoundOff = 0,
                    GrandTotal = total,
                    TotalPayment = total,
                    PaymentDue = 0
                };
                db.Purchases.Add(purchase);
                await db.SaveChangesAsync();

                foreach (var line in lines)
                {
                    db.PurchaseJoins.Add(new PurchaseJoin
                    {
                        PurchaseID = purchase.ST_ID,
                        ProductID = line.ProductId,
                        Qty = line.Qty,
                        Price = line.Price,
                        TotalAmount = Math.Round(line.Amount, 2),
                        Warehouse = warehouseName
                    });

                    await AddStockAsync(db, line.ProductId, warehouseName, line.Qty);
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                // Ledger posting happens after the purchase is safely committed -
                // best-effort, never allowed to undo a purchase that already succeeded.
                if (!string.IsNullOrWhiteSpace(supplierCode))
                {
                    try
                    {
                        await _supplierLedgerService.PostPurchaseEntryAsync(
                            supplierCode!, supplierName ?? "", purchase.InvoiceNo.Trim(), total);
                    }
                    catch
                    {
                        // Ledger posting failure never invalidates a completed purchase.
                    }
                }

                return new PurchaseResult { Ok = true, InvoiceNo = purchase.InvoiceNo.Trim(), GrandTotal = total };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new PurchaseResult { Ok = false, Error = ex.InnerException?.Message ?? ex.Message };
            }
        }

        /// <summary>Adds qty to the existing stock row for this product+warehouse, or creates one.</summary>
        private static async Task AddStockAsync(ClidaposDbContext db, int productId, string warehouseName, decimal qty)
        {
            var existing = await db.ProductOpeningStocks
                .FirstOrDefaultAsync(s => s.ProductID == productId && s.Warehouse.Trim() == warehouseName.Trim());

            if (existing != null)
            {
                existing.Qty += qty;
            }
            else
            {
                db.ProductOpeningStocks.Add(new ProductOpeningStock
                {
                    ProductID = productId,
                    Warehouse = warehouseName,
                    Qty = qty,
                    HasExpiryDate = "N"
                });
            }
        }
    }
}