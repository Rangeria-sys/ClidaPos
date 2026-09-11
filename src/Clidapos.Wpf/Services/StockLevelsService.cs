using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;

namespace Clidapos.Wpf.Services
{
    public class StockLevelRow
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public string Category { get; set; } = "";
        public string Warehouse { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public int ReorderPoint { get; set; }
        public bool IsLowStock => Qty <= ReorderPoint;

        // ExpiryDate is stored as free text in the database (nchar(50)) - "yyyy-MM-dd"
        // is the format this app writes and reads consistently. Null means no expiry
        // date has ever been recorded for this product's current stock.
        public DateTime? ExpiryDate { get; set; }
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.Today;
        public bool IsExpiringSoon => ExpiryDate.HasValue
            && !IsExpired
            && ExpiryDate.Value.Date <= DateTime.Today.AddDays(7);
    }

    public class StockAnalyticsRow
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Value => Qty * Price;

        /// <summary>Average units sold per day over the trailing window - null
        /// means no sales at all in that window, so a days-left projection
        /// can't be computed (not the same as 0, which would mean "about to
        /// run out").</summary>
        public decimal? DailySalesRate { get; set; }

        /// <summary>Null when DailySalesRate is null or zero (can't project).
        /// 0 means already out of stock.</summary>
        public decimal? DaysLeft => Qty <= 0
            ? 0
            : (DailySalesRate is > 0 ? Math.Round(Qty / DailySalesRate.Value, 1) : null);
    }

    public class StockAnalyticsSummary
    {
        public decimal ValueAtRetail { get; set; }
        public decimal ValueAtCost { get; set; }
        public decimal MarginPercent => ValueAtRetail == 0 ? 0 : Math.Round((ValueAtRetail - ValueAtCost) / ValueAtRetail * 100, 0);
        public int ProductCount { get; set; }
        public decimal UnitsOnHand { get; set; }

        public List<StockAnalyticsRow> ReorderSoon { get; set; } = new();
        public List<StockAnalyticsRow> ValueAtRisk { get; set; } = new();
        public List<CategoryBreakdownRow> ByCategory { get; set; } = new();
        public List<StockAnalyticsRow> DeadStock { get; set; } = new();
    }

    public class CategoryBreakdownRow
    {
        public string Category { get; set; } = "";
        public int ProductCount { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class StockLevelsService
    {
        public const string ExpiryDateFormat = "yyyy-MM-dd";

        /// <summary>
        /// Current on-hand quantity for every product, broken out by warehouse -
        /// one row per product+warehouse combination, sourced straight from
        /// ProductOpeningStock (the same table Items, Purchase Entry, and Sales all use).
        /// </summary>
        public async Task<List<StockLevelRow>> GetStockLevelsAsync()
        {
            using var db = new ClidaposDbContext();

            var stocks = await db.ProductOpeningStocks.ToListAsync();
            var products = await db.Products.ToListAsync();
            var productLookup = products.ToDictionary(p => p.PID);

            return stocks
                .Where(s => productLookup.ContainsKey(s.ProductID))
                .Select(s => new StockLevelRow
                {
                    ProductID = s.ProductID,
                    ProductName = productLookup[s.ProductID].ProductName.Trim(),
                    ProductCode = productLookup[s.ProductID].ProductCode.Trim(),
                    Category = productLookup[s.ProductID].Category?.Trim() ?? "",
                    Warehouse = s.Warehouse.Trim(),
                    Qty = s.Qty,
                    Price = productLookup[s.ProductID].Price,
                    ReorderPoint = productLookup[s.ProductID].ReorderPoint,
                    ExpiryDate = ParseExpiryDate(s.HasExpiryDate, s.ExpiryDate)
                })
                .OrderBy(r => r.ProductName)
                .ThenBy(r => r.Warehouse)
                .ToList();
        }

        public static DateTime? ParseExpiryDate(string? hasExpiryDate, string? expiryDateText)
        {
            if (!string.Equals(hasExpiryDate?.Trim(), "Y", StringComparison.OrdinalIgnoreCase))
                return null;

            return DateTime.TryParseExact(expiryDateText?.Trim(), ExpiryDateFormat,
                null, System.Globalization.DateTimeStyles.None, out var parsed)
                ? parsed
                : null;
        }

        private const int SalesVelocityWindowDays = 7;
        private const int DeadStockThresholdDays = 30;

        /// <summary>Everything needed for the analytical Stock Report - value
        /// at cost/margin from the latest recorded buying price per product,
        /// sales velocity from the trailing 7 days matched by product name
        /// (the same Dish-name pattern GetSalesReportAsync's Top Items already
        /// uses, since SaleItem has no ProductID to join on directly),
        /// days-left projections, value-at-risk ranking (below reorder point,
        /// highest value first), and dead stock (no sale in 30+ days).</summary>
        public async Task<StockAnalyticsSummary> GetStockAnalyticsAsync()
        {
            using var db = new ClidaposDbContext();

            var levels = await GetStockLevelsAsync();
            var buyingPrices = await new PurchaseService().GetLatestBuyingPricesAsync();

            var windowStart = DateTime.Today.AddDays(-SalesVelocityWindowDays);
            var recentSales = await (
                from item in db.SaleItems
                join bill in db.SaleBills on item.BillID equals bill.Id
                where bill.BillDate >= windowStart
                select new { Name = (item.Dish ?? "").Trim(), Qty = item.Quantity ?? 0 }
            ).ToListAsync();

            var qtySoldByName = recentSales
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty), StringComparer.OrdinalIgnoreCase);

            var deadStockCutoff = DateTime.Today.AddDays(-DeadStockThresholdDays);
            var lastSaleByName = await (
                from item in db.SaleItems
                join bill in db.SaleBills on item.BillID equals bill.Id
                group bill.BillDate by (item.Dish ?? "").Trim() into g
                select new { Name = g.Key, LastSale = g.Max() }
            ).ToDictionaryAsync(x => x.Name, x => x.LastSale, StringComparer.OrdinalIgnoreCase);

            // One row per product, aggregating quantity across warehouses -
            // the mockup's per-product tables don't split by warehouse.
            var byProduct = levels
                .GroupBy(l => l.ProductID)
                .Select(g =>
                {
                    var first = g.First();
                    var totalQty = g.Sum(x => x.Qty);
                    qtySoldByName.TryGetValue(first.ProductName, out var soldQty);

                    return new StockAnalyticsRow
                    {
                        ProductID = first.ProductID,
                        ProductName = first.ProductName,
                        Qty = totalQty,
                        Price = first.Price,
                        DailySalesRate = qtySoldByName.ContainsKey(first.ProductName)
                            ? Math.Round(soldQty / SalesVelocityWindowDays, 2)
                            : null
                    };
                })
                .ToList();

            var summary = new StockAnalyticsSummary
            {
                ValueAtRetail = byProduct.Sum(p => p.Value),
                ValueAtCost = byProduct.Sum(p => p.Qty * (buyingPrices.TryGetValue(p.ProductID, out var cp) ? cp : p.Price)),
                ProductCount = byProduct.Count,
                UnitsOnHand = byProduct.Sum(p => p.Qty)
            };

            // Reorder soon - ranked by days left ascending, only products
            // where a projection is actually possible (has recent sales).
            summary.ReorderSoon = byProduct
                .Where(p => p.DaysLeft != null)
                .OrderBy(p => p.DaysLeft)
                .Take(10)
                .ToList();

            // Value at risk - below reorder point, ranked by value descending.
            var reorderPointByProduct = levels
                .GroupBy(l => l.ProductID)
                .ToDictionary(g => g.Key, g => g.First().ReorderPoint);

            summary.ValueAtRisk = byProduct
                .Where(p => reorderPointByProduct.TryGetValue(p.ProductID, out var rp) && p.Qty <= rp)
                .OrderByDescending(p => p.Value)
                .ToList();

            // Dead stock - never sold, or last sale older than the cutoff.
            summary.DeadStock = byProduct
                .Where(p => !lastSaleByName.TryGetValue(p.ProductName, out var lastSale) || lastSale < deadStockCutoff)
                .OrderByDescending(p => p.Value)
                .ToList();

            summary.ByCategory = levels
                .GroupBy(l => string.IsNullOrWhiteSpace(l.Category) ? "(uncategorized)" : l.Category)
                .Select(g => new CategoryBreakdownRow
                {
                    Category = g.Key,
                    ProductCount = g.Select(l => l.ProductID).Distinct().Count(),
                    TotalValue = g.Sum(l => l.Qty * l.Price)
                })
                .OrderByDescending(c => c.TotalValue)
                .ToList();

            return summary;
        }
    }
}