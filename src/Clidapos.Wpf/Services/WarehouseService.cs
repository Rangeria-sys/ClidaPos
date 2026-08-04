using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class WarehouseService
    {
        public const string DefaultWarehouseName = "Main Store";
        public const string DefaultWarehouseType = "Store";

        public async Task EnsureDefaultWarehouseAsync()
        {
            using var db = new ClidaposDbContext();

            var typeExists = await db.WarehouseTypes
                .AnyAsync(t => t.Type.Trim() == DefaultWarehouseType);

            if (!typeExists)
            {
                db.WarehouseTypes.Add(new WarehouseType { Type = DefaultWarehouseType });
                await db.SaveChangesAsync();
            }

            var warehouseExists = await db.Warehouses
                .AnyAsync(w => w.WarehouseName.Trim() == DefaultWarehouseName);

            if (!warehouseExists)
            {
                db.Warehouses.Add(new Warehouse
                {
                    WarehouseName = DefaultWarehouseName,
                    Address = "N/A",
                    WarehouseType = DefaultWarehouseType,
                    City = "N/A"
                });
                await db.SaveChangesAsync();
            }
        }

        public async Task<List<Warehouse>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Warehouses.OrderBy(w => w.WarehouseName).ToListAsync();
        }

        public async Task AddAsync(Warehouse warehouse)
        {
            using var db = new ClidaposDbContext();

            var exists = await db.Warehouses
                .AnyAsync(w => w.WarehouseName.Trim().ToUpper() == warehouse.WarehouseName.Trim().ToUpper());
            if (exists)
                throw new InvalidOperationException($"A warehouse named \"{warehouse.WarehouseName.Trim()}\" already exists.");

            db.Warehouses.Add(warehouse);
            await db.SaveChangesAsync();
        }

        // originalName is the name the record currently has in the database (before any edits in this save).
        // warehouse.WarehouseName may be different if the user renamed it.
        public async Task UpdateAsync(string originalName, Warehouse warehouse)
        {
            using var db = new ClidaposDbContext();

            var from = originalName.Trim();
            var to = warehouse.WarehouseName.Trim();

            if (!string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            {
                var clash = await db.Warehouses.AnyAsync(w =>
                    w.WarehouseName.Trim().ToUpper() == to.ToUpper());
                if (clash)
                    throw new InvalidOperationException($"A warehouse named \"{to}\" already exists.");

                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE dbo.Warehouse SET WarehouseName = {to} WHERE LTRIM(RTRIM(WarehouseName)) = {from}");
            }

            var existing = await db.Warehouses
                .FirstOrDefaultAsync(w => w.WarehouseName.Trim() == to);

            if (existing != null)
            {
                existing.Address = warehouse.Address;
                existing.WarehouseType = warehouse.WarehouseType;
                existing.City = warehouse.City;
                await db.SaveChangesAsync();
            }
        }

        public async Task RemoveAsync(string warehouseName)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Warehouses
                .FirstOrDefaultAsync(w => w.WarehouseName.Trim() == warehouseName.Trim());

            if (existing != null)
            {
                db.Warehouses.Remove(existing);
                await db.SaveChangesAsync();
            }
        }
    }
}