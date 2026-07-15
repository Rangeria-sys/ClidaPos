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
    }
}