using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class WalletTypeService
    {
        public async Task<List<WalletType>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Set<WalletType>().OrderBy(w => w.Type).ToListAsync();
        }

        public async Task AddAsync(string type)
        {
            using var db = new ClidaposDbContext();
            var trimmed = type.Trim();

            var exists = await db.Set<WalletType>().AnyAsync(w => w.Type.Trim().ToUpper() == trimmed.ToUpper());
            if (exists)
                throw new System.InvalidOperationException($"Wallet type \"{trimmed}\" already exists.");

            db.Set<WalletType>().Add(new WalletType { Type = trimmed });
            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(string type)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<WalletType>().FirstOrDefaultAsync(w => w.Type.Trim() == type.Trim());
            if (existing != null)
            {
                db.Set<WalletType>().Remove(existing);
                await db.SaveChangesAsync();
            }
        }
    }
}