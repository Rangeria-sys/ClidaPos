using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class ProductService
    {
        private readonly WarehouseService _warehouseService = new();

        public async Task<List<Product>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Products.OrderBy(p => p.ProductName).ToListAsync();
        }

        public async Task<int> GetNextIdAsync()
        {
            using var db = new ClidaposDbContext();
            var maxId = await db.Products.Select(p => (int?)p.PID).MaxAsync();
            return (maxId ?? 0) + 1;
        }

        public async Task<decimal> GetQuantityAsync(int productId)
        {
            using var db = new ClidaposDbContext();
            return await db.ProductOpeningStocks
                .Where(s => s.ProductID == productId)
                .SumAsync(s => (decimal?)s.Qty) ?? 0;
        }

        /// <summary>Current stock quantity for every product in one query - used by the Items Excel export instead of one round trip per product.</summary>
        public async Task<Dictionary<int, decimal>> GetAllQuantitiesAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.ProductOpeningStocks
                .GroupBy(s => s.ProductID)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(s => s.Qty) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Qty);
        }

        public async Task SetQuantityAsync(int productId, decimal qty)
        {
            await _warehouseService.EnsureDefaultWarehouseAsync();

            using var db = new ClidaposDbContext();
            var existing = await db.ProductOpeningStocks
                .FirstOrDefaultAsync(s => s.ProductID == productId && s.Warehouse == WarehouseService.DefaultWarehouseName);

            if (existing != null)
            {
                existing.Qty = qty;
            }
            else
            {
                db.ProductOpeningStocks.Add(new ProductOpeningStock
                {
                    ProductID = productId,
                    Warehouse = WarehouseService.DefaultWarehouseName,
                    Qty = qty,
                    HasExpiryDate = "N"
                });
            }

            await db.SaveChangesAsync();
        }

        public async Task AddAsync(Product product)
        {
            using var db = new ClidaposDbContext();
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            using var db = new ClidaposDbContext();
            db.Products.Update(product);
            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int pid)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Products.FindAsync(pid);
            if (existing != null)
            {
                db.Products.Remove(existing);
            }

            var stockRows = await db.ProductOpeningStocks.Where(s => s.ProductID == pid).ToListAsync();
            db.ProductOpeningStocks.RemoveRange(stockRows);

            await db.SaveChangesAsync();
        }
    }
}