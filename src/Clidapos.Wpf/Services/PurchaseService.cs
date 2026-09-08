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
        public string? ExpiryDate { get; set; }
    }

    public class PurchaseResult
    {
        public bool Ok { get; set; }
        public string InvoiceNo { get; set; } = "";
        public decimal GrandTotal { get; set; }
        public string Error { get; set; } = "";
    }

    public class PurchaseHistoryRow
    {
        public int PurchaseId { get; set; }
        public string InvoiceNo { get; set; } = "";
        public DateTime Date { get; set; }
        public string SupplierName { get; set; } = "";
        public string PurchaseType { get; set; } = "";
        public decimal GrandTotal { get; set; }
    }

    public class PurchaseLineHistoryRow
    {
        public string ProductName { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
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

        /// <summary>
        /// Every real restock made through Purchase Entry, newest first - deliberately
        /// excludes the "Initial" entries Items creates when a Buying Price is set on
        /// a new product, since those aren't restocks and don't belong in this history.
        /// </summary>
        public async Task<List<PurchaseHistoryRow>> GetHistoryAsync()
        {
            using var db = new ClidaposDbContext();

            var purchases = await db.Purchases
                .Where(p => p.PurchaseType == "Restock")
                .OrderByDescending(p => p.Date)
                .ToListAsync();
            var suppliers = await db.Suppliers.ToListAsync();
            var supplierLookup = suppliers.ToDictionary(s => s.ID);

            return purchases.Select(p => new PurchaseHistoryRow
            {
                PurchaseId = p.ST_ID,
                InvoiceNo = p.InvoiceNo.Trim(),
                Date = p.Date,
                SupplierName = supplierLookup.TryGetValue(p.Supplier_ID, out var s) ? (s.Name?.Trim() ?? "") : "",
                PurchaseType = p.PurchaseType?.Trim() ?? "",
                GrandTotal = p.GrandTotal
            }).ToList();
        }

        /// <summary>The individual products received on one specific purchase - the drill-down behind a history row.</summary>
        public async Task<List<PurchaseLineHistoryRow>> GetPurchaseLinesAsync(int purchaseId)
        {
            using var db = new ClidaposDbContext();

            var lines = await db.PurchaseJoins.Where(j => j.PurchaseID == purchaseId).ToListAsync();
            var products = await db.Products.ToListAsync();
            var productLookup = products.ToDictionary(p => p.PID);

            return lines.Select(l => new PurchaseLineHistoryRow
            {
                ProductName = productLookup.TryGetValue(l.ProductID, out var p) ? p.ProductName.Trim() : "(unknown product)",
                Qty = l.Qty,
                Price = l.Price,
                Amount = l.TotalAmount
            }).ToList();
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

            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line.ExpiryDate) &&
                    !DateTime.TryParseExact(line.ExpiryDate.Trim(), StockLevelsService.ExpiryDateFormat,
                        null, System.Globalization.DateTimeStyles.None, out _))
                {
                    return new PurchaseResult
                    {
                        Ok = false,
                        Error = $"'{line.ProductName}' has an invalid expiry date - use yyyy-MM-dd (e.g. 2026-12-31), or leave it blank."
                    };
                }
            }

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
                supplierName = supplier?.Name?.Trim();

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

                    await AddStockAsync(db, line.ProductId, warehouseName, line.Qty,
                        string.IsNullOrWhiteSpace(line.ExpiryDate) ? null : line.ExpiryDate.Trim());
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

        /// <summary>Adds qty to the existing stock row for this product+warehouse, or creates one. If an expiry date string ("yyyy-MM-dd") is given, it's recorded on that row.</summary>
        private static async Task AddStockAsync(ClidaposDbContext db, int productId, string warehouseName, decimal qty, string? expiryDate = null)
        {
            var existing = await db.ProductOpeningStocks
                .FirstOrDefaultAsync(s => s.ProductID == productId && s.Warehouse.Trim() == warehouseName.Trim());

            if (existing != null)
            {
                existing.Qty += qty;

                if (expiryDate != null)
                {
                    existing.HasExpiryDate = "Y";
                    existing.ExpiryDate = expiryDate;
                }
            }
            else
            {
                db.ProductOpeningStocks.Add(new ProductOpeningStock
                {
                    ProductID = productId,
                    Warehouse = warehouseName,
                    Qty = qty,
                    HasExpiryDate = expiryDate != null ? "Y" : "N",
                    ExpiryDate = expiryDate
                });
            }
        }
    }
}