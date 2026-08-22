using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class StockAdjustmentResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; } = "";
    }

    public class StockAdjustmentRow
    {
        public DateTime? Date { get; set; }
        public string ProductName { get; set; } = "";
        public string Warehouse { get; set; } = "";
        public string AdjustmentType { get; set; } = "";
        public decimal Qty { get; set; }
        public string Reason { get; set; } = "";
    }

    public class StockAdjustmentService
    {
        /// <summary>Current on-hand quantity for one product at one warehouse. 0 if no stock row exists yet.</summary>
        public async Task<decimal> GetCurrentQtyAsync(int productId, string warehouseName)
        {
            using var db = new ClidaposDbContext();
            var stock = await db.ProductOpeningStocks
                .FirstOrDefaultAsync(s => s.ProductID == productId && s.Warehouse.Trim() == warehouseName.Trim());
            return stock?.Qty ?? 0;
        }

        /// <summary>Joined against Product so the history shows real product names, not just IDs.</summary>
        public async Task<List<StockAdjustmentRow>> GetHistoryAsync()
        {
            using var db = new ClidaposDbContext();

            var adjustments = await db.StockAdjustments.OrderByDescending(a => a.Date).ToListAsync();
            var products = await db.Products.ToListAsync();
            var productLookup = products.ToDictionary(p => p.PID);

            return adjustments.Select(a => new StockAdjustmentRow
            {
                Date = a.Date,
                ProductName = a.ProductID.HasValue && productLookup.ContainsKey(a.ProductID.Value)
                    ? productLookup[a.ProductID.Value].ProductName.Trim()
                    : "(unknown product)",
                Warehouse = a.Warehouse?.Trim() ?? "",
                AdjustmentType = a.AdjustmentType?.Trim() ?? "",
                Qty = a.Qty ?? 0,
                Reason = a.Reason?.Trim() ?? ""
            }).ToList();
        }

        /// <summary>
        /// Records a stock adjustment and applies it immediately to ProductOpeningStock -
        /// "Increase" adds qty, "Decrease" subtracts it - all inside one transaction.
        /// </summary>
        public async Task<StockAdjustmentResult> SaveAdjustmentAsync(
            int productId, string warehouseName, string adjustmentType, decimal qty, string reason)
        {
            if (qty <= 0)
                return new StockAdjustmentResult { Ok = false, Error = "Quantity must be greater than zero." };

            if (string.IsNullOrWhiteSpace(reason))
                return new StockAdjustmentResult { Ok = false, Error = "A reason is required for every adjustment." };

            if (string.IsNullOrWhiteSpace(warehouseName))
                return new StockAdjustmentResult { Ok = false, Error = "Pick a warehouse." };

            using var db = new ClidaposDbContext();
            using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                var maxId = await db.StockAdjustments.Select(a => (int?)a.SA_ID).MaxAsync() ?? 0;

                db.StockAdjustments.Add(new StockAdjustment
                {
                    SA_ID = maxId + 1,
                    Date = DateTime.Now,
                    Warehouse = warehouseName,
                    ProductID = productId,
                    AdjustmentType = adjustmentType,
                    Qty = qty,
                    Reason = reason.Trim()
                });

                var stockRow = await db.ProductOpeningStocks
                    .FirstOrDefaultAsync(s => s.ProductID == productId && s.Warehouse.Trim() == warehouseName.Trim());

                var delta = adjustmentType == "Increase" ? qty : -qty;

                if (stockRow != null)
                {
                    stockRow.Qty += delta;
                }
                else
                {
                    db.ProductOpeningStocks.Add(new ProductOpeningStock
                    {
                        ProductID = productId,
                        Warehouse = warehouseName,
                        Qty = delta,
                        HasExpiryDate = "N"
                    });
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return new StockAdjustmentResult { Ok = true };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new StockAdjustmentResult { Ok = false, Error = ex.InnerException?.Message ?? ex.Message };
            }
        }
    }
}