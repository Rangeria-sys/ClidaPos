using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class StockAdjustmentRow
    {
        public int SA_ID { get; set; }
        public DateTime? Date { get; set; }
        public string ProductName { get; set; } = "";
        public string Warehouse { get; set; } = "";
        public string AdjustmentType { get; set; } = "";
        public decimal Qty { get; set; }
        public string Reason { get; set; } = "";
    }

    public class StockAdjustmentService
    {
        /// <summary>Adjustment history, newest first, with product names joined in for display.</summary>
        public async Task<List<StockAdjustmentRow>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();

            var adjustments = await db.StockAdjustments
                .OrderByDescending(a => a.SA_ID)
                .ToListAsync();

            var products = await db.Products.ToListAsync();
            var productLookup = products.ToDictionary(p => p.PID);

            return adjustments.Select(a => new StockAdjustmentRow
            {
                SA_ID = a.SA_ID,
                Date = a.Date,
                ProductName = a.ProductID.HasValue && productLookup.ContainsKey(a.ProductID.Value)
                    ? productLookup[a.ProductID.Value].ProductName.Trim()
                    : "(unknown product)",
                Warehouse = (a.Warehouse ?? "").Trim(),
                AdjustmentType = (a.AdjustmentType ?? "").Trim(),
                Qty = a.Qty ?? 0,
                Reason = (a.Reason ?? "").Trim()
            }).ToList();
        }

        /// <summary>
        /// Logs the adjustment and applies it to ProductOpeningStock in the same
        /// transaction - "Increase" adds to stock, "Decrease" subtracts.
        /// </summary>
        public async Task<string> SaveAdjustmentAsync(
            int productId, string warehouseName, string adjustmentType, decimal qty, string reason)
        {
            if (qty <= 0)
                return "Quantity must be greater than zero.";

            if (string.IsNullOrWhiteSpace(reason))
                return "A reason is required for every adjustment.";

            if (adjustmentType != "Increase" && adjustmentType != "Decrease")
                return "Pick Increase or Decrease.";

            using var db = new ClidaposDbContext();
            using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                var maxId = await db.StockAdjustments.Select(a => (int?)a.SA_ID).MaxAsync() ?? 0;

                db.StockAdjustments.Add(new StockAdjustment
                {
                    SA_ID = maxId + 1,
                    Date = DateTime.Now,
                    Warehouse = warehouseName.Trim(),
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
                        Warehouse = warehouseName.Trim(),
                        Qty = delta,
                        HasExpiryDate = "N"
                    });
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return "";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ex.InnerException?.Message ?? ex.Message;
            }
        }
    }
}