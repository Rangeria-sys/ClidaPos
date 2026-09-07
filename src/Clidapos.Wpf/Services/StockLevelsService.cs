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
    }
}