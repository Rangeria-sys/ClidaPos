using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class ClockResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; } = "";
        public bool JustClockedIn { get; set; }
        public DateTime? ClockInTime { get; set; }
        public DateTime? ClockOutTime { get; set; }
        public TimeSpan? Duration { get; set; }
    }

    public class ClockService
    {
        /// <summary>Finds an open (not yet clocked out) entry for this user, if any.</summary>
        public async Task<ClockEntry?> GetOpenEntryAsync(string userId)
        {
            using var db = new ClidaposDbContext();
            return await db.Set<ClockEntry>()
                .Where(c => c.UserID.Trim() == userId.Trim() && c.ClockOutTime == null)
                .OrderByDescending(c => c.ClockInTime)
                .FirstOrDefaultAsync();
        }

        /// <summary>One call that does the right thing either way: if the user has
        /// an open entry, this clocks them out; otherwise it clocks them in.</summary>
        public async Task<ClockResult> ToggleAsync(string userId, string userName)
        {
            using var db = new ClidaposDbContext();

            var open = await db.Set<ClockEntry>()
                .Where(c => c.UserID.Trim() == userId.Trim() && c.ClockOutTime == null)
                .OrderByDescending(c => c.ClockInTime)
                .FirstOrDefaultAsync();

            if (open != null)
            {
                open.ClockOutTime = DateTime.Now;
                await db.SaveChangesAsync();

                return new ClockResult
                {
                    Ok = true,
                    JustClockedIn = false,
                    ClockInTime = open.ClockInTime,
                    ClockOutTime = open.ClockOutTime,
                    Duration = open.ClockOutTime - open.ClockInTime
                };
            }

            var maxId = await db.Set<ClockEntry>().Select(c => (int?)c.Id).MaxAsync() ?? 0;
            var now = DateTime.Now;

            var entry = new ClockEntry
            {
                Id = maxId + 1,
                UserID = userId.Trim(),
                UserName = userName.Trim(),
                WorkDate = now.Date,
                ClockInTime = now
            };
            db.Set<ClockEntry>().Add(entry);
            await db.SaveChangesAsync();

            return new ClockResult
            {
                Ok = true,
                JustClockedIn = true,
                ClockInTime = entry.ClockInTime
            };
        }
    }
}