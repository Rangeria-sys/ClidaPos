using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class HeldSaleService
    {
        public async Task<int> HoldAsync(List<CartLine> cart, string operatorName,
            decimal discountPercent, string customerName, string label)
        {
            using var db = new ClidaposDbContext();

            var maxSaleId = await db.Set<HeldSale>().Select(s => (int?)s.Id).MaxAsync() ?? 0;
            var sale = new HeldSale
            {
                Id = maxSaleId + 1,
                HeldDate = DateTime.Now,
                Operator = operatorName.Trim(),
                DiscountPercent = discountPercent,
                CustomerName = customerName.Trim(),
                Label = label.Trim()
            };
            db.Set<HeldSale>().Add(sale);

            var maxItemId = await db.Set<HeldSaleItem>().Select(i => (int?)i.Id).MaxAsync() ?? 0;
            foreach (var line in cart)
            {
                maxItemId++;
                db.Set<HeldSaleItem>().Add(new HeldSaleItem
                {
                    Id = maxItemId,
                    HeldSaleId = sale.Id,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    ProductCode = line.ProductCode,
                    Category = line.Category,
                    Rate = line.Rate,
                    Quantity = line.Quantity
                });
            }

            await db.SaveChangesAsync();
            return sale.Id;
        }

        public async Task<List<HeldSaleWithItems>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();

            var sales = await db.Set<HeldSale>().OrderByDescending(s => s.HeldDate).ToListAsync();
            var allItems = await db.Set<HeldSaleItem>().ToListAsync();

            return sales.Select(s => new HeldSaleWithItems
            {
                Sale = s,
                Items = allItems.Where(i => i.HeldSaleId == s.Id).ToList()
            }).ToList();
        }

        /// <summary>Deletes items first (child), then the sale (parent) in two
        /// separate SaveChanges calls - avoids the FK constraint violation that
        /// happens when EF tries to delete parent before child in a single batch.</summary>
        public async Task DeleteAsync(int heldSaleId)
        {
            using var db = new ClidaposDbContext();

            // Step 1: delete child rows first and commit immediately
            var items = await db.Set<HeldSaleItem>()
                .Where(i => i.HeldSaleId == heldSaleId)
                .ToListAsync();

            if (items.Count > 0)
            {
                db.Set<HeldSaleItem>().RemoveRange(items);
                await db.SaveChangesAsync();
            }

            // Step 2: now safe to delete the parent row
            var sale = await db.Set<HeldSale>().FirstOrDefaultAsync(s => s.Id == heldSaleId);
            if (sale != null)
            {
                db.Set<HeldSale>().Remove(sale);
                await db.SaveChangesAsync();
            }
        }
    }
}