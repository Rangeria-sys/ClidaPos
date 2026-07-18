using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class UnitService
    {
        public async Task<List<string>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();
            var units = await db.UnitMasters.ToListAsync();
            return units.Select(u => u.Unit.Trim()).OrderBy(u => u).ToList();
        }

        public async Task EnsureExistsAsync(string unit)
        {
            var trimmed = unit.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return;

            using var db = new ClidaposDbContext();

            var exists = await db.UnitMasters
                .AnyAsync(u => u.Unit.Trim().ToUpper() == trimmed.ToUpper());

            if (!exists)
            {
                db.UnitMasters.Add(new UnitMaster { Unit = trimmed });
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

            var clash = await db.UnitMasters.AnyAsync(u =>
                u.Unit.Trim().ToUpper() == to.ToUpper() &&
                u.Unit.Trim().ToUpper() != from.ToUpper());
            if (clash)
                throw new InvalidOperationException($"A unit named \"{to}\" already exists.");

            // ON UPDATE CASCADE updates every product that uses this unit.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.UnitMaster SET Unit = {to} WHERE LTRIM(RTRIM(Unit)) = {from}");
        }

        public async Task RemoveAsync(string unit)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.UnitMasters
                .FirstOrDefaultAsync(u => u.Unit.Trim() == unit.Trim());

            if (existing != null)
            {
                db.UnitMasters.Remove(existing);
                await db.SaveChangesAsync();
            }
        }
    }
}