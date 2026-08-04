using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class SupplierService
    {
        public async Task<List<Supplier>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Suppliers.OrderBy(s => s.Name).ToListAsync();
        }

        public async Task<int> GetNextIdAsync()
        {
            using var db = new ClidaposDbContext();
            var maxId = await db.Suppliers.Select(s => (int?)s.ID).MaxAsync();
            return (maxId ?? 0) + 1;
        }

        public async Task AddAsync(Supplier supplier)
        {
            using var db = new ClidaposDbContext();
            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Supplier supplier)
        {
            using var db = new ClidaposDbContext();
            db.Suppliers.Update(supplier);
            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Suppliers.FindAsync(id);
            if (existing != null)
            {
                db.Suppliers.Remove(existing);
                await db.SaveChangesAsync();
            }
        }
    }
}