using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class RegistrationService
    {
        public async Task<List<Registration>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Registrations.OrderBy(r => r.Name).ToListAsync();
        }

        public async Task<Registration?> GetByUserIdAsync(string userId)
        {
            using var db = new ClidaposDbContext();
            return await db.Registrations
                .FirstOrDefaultAsync(r => r.UserID.Trim() == userId.Trim());
        }

        public async Task AddAsync(Registration user)
        {
            using var db = new ClidaposDbContext();

            var exists = await db.Registrations
                .AnyAsync(r => r.UserID.Trim().ToUpper() == user.UserID.Trim().ToUpper());
            if (exists)
                throw new InvalidOperationException($"A user with ID \"{user.UserID.Trim()}\" already exists.");

            db.Registrations.Add(user);
            await db.SaveChangesAsync();
        }

        // originalUserId is the ID the record currently has in the database (before any edits in this save).
        // user.UserID may be different if the user ID itself was renamed.
        public async Task UpdateAsync(string originalUserId, Registration user)
        {
            using var db = new ClidaposDbContext();

            var from = originalUserId.Trim();
            var to = user.UserID.Trim();

            if (!string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            {
                var clash = await db.Registrations.AnyAsync(r =>
                    r.UserID.Trim().ToUpper() == to.ToUpper());
                if (clash)
                    throw new InvalidOperationException($"A user with ID \"{to}\" already exists.");

                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE dbo.Registration SET UserID = {to} WHERE LTRIM(RTRIM(UserID)) = {from}");
            }

            var existing = await db.Registrations
                .FirstOrDefaultAsync(r => r.UserID.Trim() == to);

            if (existing != null)
            {
                existing.UserType = user.UserType;
                existing.Password = user.Password;
                existing.Name = user.Name;
                existing.Active = user.Active;
                existing.ContactNo = user.ContactNo;
                existing.EmailID = user.EmailID;
                existing.SSN = user.SSN;
                existing.PayrollType = user.PayrollType;
                existing.CardNo = user.CardNo;
                existing.AutoLogout = user.AutoLogout;
                await db.SaveChangesAsync();
            }
        }

        public async Task RemoveAsync(string userId)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Registrations
                .FirstOrDefaultAsync(r => r.UserID.Trim() == userId.Trim());

            if (existing != null)
            {
                db.Registrations.Remove(existing);
                await db.SaveChangesAsync();
            }
        }
    }
}