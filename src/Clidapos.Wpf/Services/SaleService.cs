using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class SaleResult
    {
        public bool Ok { get; set; }
        public string BillNo { get; set; } = "";
        public decimal GrandTotal { get; set; }
        public decimal Change { get; set; }
        public string Error { get; set; } = "";
    }

    public class SaleService
    {
        private readonly WarehouseService _warehouseService = new();

        /// <summary>Exact product-code match - used when a barcode is scanned.</summary>
        public async Task<Product?> FindByCodeAsync(string code)
        {
            var c = (code ?? "").Trim();
            if (c.Length == 0) return null;

            using var db = new ClidaposDbContext();
            return await db.Products.FirstOrDefaultAsync(p => p.ProductCode.Trim() == c);
        }

        /// <summary>Fuzzy search on name or code, for typing at the till.</summary>
        public async Task<List<Product>> SearchAsync(string term)
        {
            var t = (term ?? "").Trim();
            if (t.Length == 0) return new List<Product>();

            using var db = new ClidaposDbContext();
            return await db.Products
                .Where(p => p.ProductName.Contains(t) || p.ProductCode.Contains(t))
                .OrderBy(p => p.ProductName)
                .Take(50)
                .ToListAsync();
        }

        public async Task<decimal> GetStockAsync(int productId)
        {
            using var db = new ClidaposDbContext();
            return await db.ProductOpeningStocks
                .Where(s => s.ProductID == productId)
                .SumAsync(s => (decimal?)s.Qty) ?? 0;
        }

        /// <summary>
        /// Writes the bill, its lines, and deducts stock - all inside one transaction,
        /// so a failure part-way through leaves nothing behind.
        /// </summary>
        public async Task<SaleResult> SaveSaleAsync(
            List<CartLine> lines,
            Registration cashier,
            string paymentMode,
            decimal amountReceived,
            string customerName,
            string phoneNo)
        {
            if (lines == null || lines.Count == 0)
                return new SaleResult { Ok = false, Error = "The cart is empty." };

            if (lines.Any(l => l.Quantity <= 0))
                return new SaleResult { Ok = false, Error = "Every line needs a quantity above zero." };

            await _warehouseService.EnsureDefaultWarehouseAsync();

            var vatPercent = AppSettings.VatPercent;
            var grandTotal = Math.Round(lines.Sum(l => l.Amount), 2);

            // VAT-inclusive: tax is extracted from the price, not added on top.
            var taxable = vatPercent > 0
                ? Math.Round(grandTotal / (1 + (vatPercent / 100m)), 2)
                : grandTotal;
            var vatAmount = Math.Round(grandTotal - taxable, 2);

            var isCash = paymentMode.Trim().Equals("Cash", StringComparison.OrdinalIgnoreCase);
            var change = isCash ? Math.Round(amountReceived - grandTotal, 2) : 0m;

            if (isCash && amountReceived < grandTotal)
                return new SaleResult { Ok = false, Error = "Amount received is less than the total due." };

            using var db = new ClidaposDbContext();
            using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                var nextId = (await db.SaleBills.Select(b => (int?)b.Id).MaxAsync() ?? 0) + 1;
                var billNo = "S" + nextId.ToString("D6");

                var bill = new SaleBill
                {
                    Id = nextId,
                    BillNo = billNo,
                    BillDate = DateTime.Now,
                    SubTotal = grandTotal,
                    TADiscountPer = 0,
                    TADiscountAmt = 0,
                    GrandTotal = grandTotal,
                    Cash = isCash ? amountReceived : 0,
                    Change = change,
                    Card = isCash ? 0 : grandTotal,
                    Operator = cashier.UserID.Trim(),
                    PaymentMode = paymentMode.Trim(),
                    CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName.Trim(),
                    PhoneNo = string.IsNullOrWhiteSpace(phoneNo) ? null : phoneNo.Trim(),
                    TA_Status = "Paid",
                    TaxType = "Inclusive",
                    TotalTaxableAmount = taxable,
                    TotalTaxAmount = vatAmount
                };
                db.SaleBills.Add(bill);
                await db.SaveChangesAsync();

                foreach (var line in lines)
                {
                    var lineTotal = Math.Round(line.Amount, 2);
                    var lineTaxable = vatPercent > 0
                        ? Math.Round(lineTotal / (1 + (vatPercent / 100m)), 2)
                        : lineTotal;

                    db.SaleItems.Add(new SaleItem
                    {
                        BillID = bill.Id,
                        Dish = line.ProductName,
                        Rate = line.Rate,
                        Quantity = line.Quantity,
                        Amount = lineTotal,
                        VATPer = vatPercent,
                        VATAmount = Math.Round(lineTotal - lineTaxable, 3),
                        DiscountPer = 0,
                        DiscountAmount = 0,
                        TotalAmount = lineTotal,
                        Category = string.IsNullOrWhiteSpace(line.Category) ? null : line.Category,
                        ItemStatus = "Sold"
                    });

                    await DeductStockAsync(db, line.ProductId, line.Quantity);
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return new SaleResult
                {
                    Ok = true,
                    BillNo = billNo,
                    GrandTotal = grandTotal,
                    Change = change
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new SaleResult
                {
                    Ok = false,
                    Error = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        /// <summary>Takes quantity off stock rows oldest-first; allows going negative rather than blocking a sale.</summary>
        private static async Task DeductStockAsync(ClidaposDbContext db, int productId, decimal qty)
        {
            var rows = await db.ProductOpeningStocks
                .Where(s => s.ProductID == productId)
                .OrderBy(s => s.PS_ID)
                .ToListAsync();

            if (rows.Count == 0)
            {
                db.ProductOpeningStocks.Add(new ProductOpeningStock
                {
                    ProductID = productId,
                    Warehouse = WarehouseService.DefaultWarehouseName,
                    Qty = -qty,
                    HasExpiryDate = "N"
                });
                return;
            }

            var remaining = qty;
            foreach (var row in rows)
            {
                if (remaining <= 0) break;

                var take = Math.Min(row.Qty, remaining);
                if (take > 0)
                {
                    row.Qty -= take;
                    remaining -= take;
                }
            }

            // Anything left over goes negative on the first row, so the shortfall is visible.
            if (remaining > 0)
                rows[0].Qty -= remaining;
        }
    }
}