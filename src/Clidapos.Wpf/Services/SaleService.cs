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

    public class VoidResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; } = "";
        public List<string> StockWarnings { get; set; } = new();
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

        /// <summary>Prefix match on name or code, for typing at the till - starts filtering from the first letter.</summary>
        public async Task<List<Product>> SearchAsync(string term)
        {
            var t = (term ?? "").Trim();
            if (t.Length == 0) return new List<Product>();

            using var db = new ClidaposDbContext();
            return await db.Products
                .Where(p => p.ProductName.StartsWith(t) || p.ProductCode.StartsWith(t))
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

        /// <summary>Most recent completed sales, newest first - powers the Get Data / sales history screen.</summary>
        public async Task<List<SaleBill>> GetRecentSalesAsync(int count = 50)
        {
            using var db = new ClidaposDbContext();
            return await db.SaleBills
                .OrderByDescending(b => b.BillDate)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>The line items belonging to one past sale, for the receipt detail view.</summary>
        public async Task<List<SaleItem>> GetSaleItemsAsync(int billId)
        {
            using var db = new ClidaposDbContext();
            return await db.SaleItems
                .Where(i => i.BillID == billId)
                .ToListAsync();
        }

        /// <summary>
        /// Writes the bill, its lines, and deducts stock - all inside one transaction,
        /// so a failure part-way through leaves nothing behind. discountPercent applies
        /// to the whole sale (0-100), taken off the VAT-inclusive subtotal.
        /// </summary>
        public async Task<SaleResult> SaveSaleAsync(
            List<CartLine> lines,
            Registration cashier,
            string paymentMode,
            decimal amountReceived,
            string customerName,
            string phoneNo,
            decimal discountPercent = 0)
        {
            if (lines == null || lines.Count == 0)
                return new SaleResult { Ok = false, Error = "The cart is empty." };

            if (lines.Any(l => l.Quantity <= 0))
                return new SaleResult { Ok = false, Error = "Every line needs a quantity above zero." };

            if (discountPercent < 0 || discountPercent > 100)
                discountPercent = 0;

            await _warehouseService.EnsureDefaultWarehouseAsync();

            var vatPercent = AppSettings.VatPercent;
            var subtotal = Math.Round(lines.Sum(l => l.Amount), 2);
            var discountAmount = Math.Round(subtotal * discountPercent / 100m, 2);
            var grandTotal = Math.Round(subtotal - discountAmount, 2);

            // VAT-inclusive: tax is extracted from the post-discount total, not added on top.
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
                    SubTotal = subtotal,
                    TADiscountPer = discountPercent,
                    TADiscountAmt = discountAmount,
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

        /// <summary>
        /// Removes a completed sale from active records, archives it to the DeletedInvoices
        /// audit trail with a mandatory reason, and restores stock. Stock is matched by product
        /// NAME (the sales table has no ProductID column) - if a product was renamed or deleted
        /// since the sale, that line's stock cannot be auto-restored and is reported as a warning.
        /// </summary>
        public async Task<VoidResult> VoidSaleAsync(int billId, string reason, Registration operatorUser)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return new VoidResult { Ok = false, Error = "A reason is required to void a sale." };

            using var db = new ClidaposDbContext();
            using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                var bill = await db.SaleBills.FirstOrDefaultAsync(b => b.Id == billId);
                if (bill == null)
                    return new VoidResult { Ok = false, Error = "That sale could not be found." };

                var items = await db.SaleItems.Where(i => i.BillID == billId).ToListAsync();
                var warnings = new List<string>();

                db.DeletedInvoices.Add(new DeletedInvoice
                {
                    BillNo = bill.BillNo,
                    BillDate = bill.BillDate,
                    GrandTotal = bill.GrandTotal,
                    Operator = bill.Operator,
                    PaymentMode = bill.PaymentMode,
                    Reason = reason.Trim(),
                    DeletedDate = DateTime.Now,
                    BillType = "TA",
                    Canceled_Deleted = "Deleted"
                });

                foreach (var item in items)
                {
                    db.DeletedInvoiceJoins.Add(new DeletedInvoiceJoin
                    {
                        BillNo = bill.BillNo,
                        ItemName = item.Dish,
                        Qty = item.Quantity,
                        TotalAmount = item.TotalAmount
                    });

                    var name = (item.Dish ?? "").Trim();
                    if (name.Length == 0 || item.Quantity == null || item.Quantity <= 0)
                        continue;

                    var product = await db.Products.FirstOrDefaultAsync(p => p.ProductName.Trim() == name);
                    if (product == null)
                    {
                        warnings.Add($"\"{name}\" is no longer in the catalog - stock not restored for this line.");
                        continue;
                    }

                    var stockRow = await db.ProductOpeningStocks
                        .Where(s => s.ProductID == product.PID)
                        .OrderByDescending(s => s.PS_ID)
                        .FirstOrDefaultAsync();

                    if (stockRow != null)
                    {
                        stockRow.Qty += item.Quantity.Value;
                    }
                    else
                    {
                        db.ProductOpeningStocks.Add(new ProductOpeningStock
                        {
                            ProductID = product.PID,
                            Warehouse = WarehouseService.DefaultWarehouseName,
                            Qty = item.Quantity.Value,
                            HasExpiryDate = "N"
                        });
                    }
                }

                db.SaleItems.RemoveRange(items);
                db.SaleBills.Remove(bill);

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return new VoidResult { Ok = true, StockWarnings = warnings };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new VoidResult { Ok = false, Error = ex.InnerException?.Message ?? ex.Message };
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

            if (remaining > 0)
                rows[0].Qty -= remaining;
        }
    }
}