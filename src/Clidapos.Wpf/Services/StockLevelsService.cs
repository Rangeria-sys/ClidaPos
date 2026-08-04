using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;

namespace Clidapos.Wpf.Services
{
    public class StockLevelRow
    {
        public string ProductName { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public string Category { get; set; } = "";
        public string Warehouse { get; set; } = "";
        public decimal Qty { get; set; }
    }

    public class StockLevelsService
    {
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
                    ProductName = productLookup[s.ProductID].ProductName.Trim(),
                    ProductCode = productLookup[s.ProductID].ProductCode.Trim(),
                    Category = productLookup[s.ProductID].Category?.Trim() ?? "",
                    Warehouse = s.Warehouse.Trim(),
                    Qty = s.Qty
                })
                .OrderBy(r => r.ProductName)
                .ThenBy(r => r.Warehouse)
                .ToList();
        }
    }
}