using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class WarehouseTypeService
    {
        public async Task<List<string>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();
            var types = await db.WarehouseTypes.ToListAsync();
            return types.Select(t => t.Type.Trim()).OrderBy(t => t).ToList();
        }

        public async Task EnsureExistsAsync(string typeName)
        {
            var trimmed = typeName.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return;

            using var db = new ClidaposDbContext();

            var exists = await db.WarehouseTypes
                .AnyAsync(t => t.Type.Trim().ToUpper() == trimmed.ToUpper());

            if (!exists)
            {
                db.WarehouseTypes.Add(new WarehouseType { Type = trimmed });
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

            var clash = await db.WarehouseTypes.AnyAsync(t =>
                t.Type.Trim().ToUpper() == to.ToUpper() &&
                t.Type.Trim().ToUpper() != from.ToUpper());
            if (clash)
                throw new InvalidOperationException($"A warehouse type named \"{to}\" already exists.");

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.WarehouseType SET Type = {to} WHERE LTRIM(RTRIM(Type)) = {from}");
        }

        public async Task RemoveAsync(string typeName)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.WarehouseTypes
                .FirstOrDefaultAsync(t => t.Type.Trim() == typeName.Trim());

            if (existing != null)
            {
                db.WarehouseTypes.Remove(existing);
                await db.SaveChangesAsync();
            }
        }
    }
}