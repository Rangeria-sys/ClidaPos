using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class CategoryService
    {
        public async Task<List<string>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();
            var categories = await db.RMCategories.ToListAsync();
            return categories.Select(c => c.CategoryName.Trim()).OrderBy(c => c).ToList();
        }

        public async Task EnsureExistsAsync(string categoryName)
        {
            var trimmed = categoryName.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return;

            using var db = new ClidaposDbContext();

            var exists = await db.RMCategories
                .AnyAsync(c => c.CategoryName.Trim().ToUpper() == trimmed.ToUpper());

            if (!exists)
            {
                db.RMCategories.Add(new RMCategory { CategoryName = trimmed });
                await db.SaveChangesAsync();
            }
        }

        public async Task RenameAsync(string oldName, string newName)
        {
            var from = oldName.Trim();
            var to = newName.Trim();
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return;
            if (from == to) return;

            using var db = new ClidaposDbContext();

            var clash = await db.RMCategories.AnyAsync(c =>
                c.CategoryName.Trim().ToUpper() == to.ToUpper() &&
                c.CategoryName.Trim().ToUpper() != from.ToUpper());
            if (clash)
                throw new InvalidOperationException($"A category named \"{to}\" already exists.");

            // ON UPDATE CASCADE updates every product that uses this category.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.RMCategory SET CategoryName = {to} WHERE LTRIM(RTRIM(CategoryName)) = {from}");
        }

        public async Task RemoveAsync(string categoryName)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.RMCategories
                .FirstOrDefaultAsync(c => c.CategoryName.Trim() == categoryName.Trim());

            if (existing != null)
            {
                db.RMCategories.Remove(existing);
                await db.SaveChangesAsync();
            }
        }
    }
}