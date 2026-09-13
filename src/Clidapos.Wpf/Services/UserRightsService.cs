using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class UserRightsService
    {
        /// <summary>
        /// The app's actual navigable modules - matches Back Office's tile set,
        /// so "who can access what" maps directly onto real screens rather than
        /// an arbitrary made-up list.
        /// </summary>
        public static readonly string[] Modules =
        {
            "Master Setting", "Business Profile", "User Security Roles", "Backup Setting", "Category Setup",
            "Items", "Purchase Entry", "Stock Levels", "Stock Adjustment", "Stock Transfer", "Supplier", "Warehouse Management",
            "Employee Registration", "HR & Payroll", "Customers", "Customer Ledger", "Supplier Ledger",
            "Expense Log", "Expense Master", "Finance & Banking", "Loyalty & Membership", "Vouchers & Promotions",
            "Sales Reports", "Stock Reports", "Purchase Reports", "Expense Reports", "Accounting Reports", "System Logs",
            "Front Office Report"
        };

        /// <summary>
        /// One row per known module for this user - any module without a saved
        /// row yet gets a blank in-memory placeholder (all rights unchecked,
        /// ID = 0) rather than being left out, so every screen always shows in
        /// the grid.
        /// </summary>
        public async Task<List<UserRight>> GetForUserAsync(string userId)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.UserRights
                .Where(r => r.UserID != null && r.UserID.Trim() == userId.Trim())
                .ToListAsync();

            var byModule = existing
                .GroupBy(r => (r.ModuleName ?? "").Trim())
                .ToDictionary(g => g.Key, g => g.First());

            return Modules.Select(m => byModule.TryGetValue(m, out var row)
                ? row
                : new UserRight { ModuleName = m, UserID = userId, UR_Save = false, UR_Update = false, UR_Delete = false, UR_View = false }
            ).ToList();
        }

        /// <summary>Upserts every row in the grid for this user - rows already saved (ID > 0) are updated in place, brand-new ones are inserted.</summary>
        public async Task SaveForUserAsync(string userId, List<UserRight> rights)
        {
            using var db = new ClidaposDbContext();

            foreach (var right in rights)
            {
                right.UserID = userId;
                right.UR_Save ??= false;
                right.UR_Update ??= false;
                right.UR_Delete ??= false;
                right.UR_View ??= false;

                if (right.ID > 0)
                {
                    db.UserRights.Attach(right);
                    db.Entry(right).State = EntityState.Modified;
                }
                else
                {
                    db.UserRights.Add(right);
                }
            }

            await db.SaveChangesAsync();
        }

        /// <summary>Called after a user's login ID is renamed, so their saved permission rows follow them instead of orphaning under the old ID.</summary>
        public async Task RenameUserAsync(string oldUserId, string newUserId)
        {
            using var db = new ClidaposDbContext();
            var rows = await db.UserRights
                .Where(r => r.UserID != null && r.UserID.Trim() == oldUserId.Trim())
                .ToListAsync();
            foreach (var row in rows) row.UserID = newUserId;
            await db.SaveChangesAsync();
        }

        /// <summary>Called after a user is deleted, to clean up their now-orphaned permission rows.</summary>
        public async Task DeleteForUserAsync(string userId)
        {
            using var db = new ClidaposDbContext();
            var rows = await db.UserRights
                .Where(r => r.UserID != null && r.UserID.Trim() == userId.Trim())
                .ToListAsync();
            db.UserRights.RemoveRange(rows);
            await db.SaveChangesAsync();
        }
    }
}
