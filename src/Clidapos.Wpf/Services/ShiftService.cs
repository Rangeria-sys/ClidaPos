using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class ShiftService
    {
        public async Task<bool> IsShiftOpenAsync()
        {
            return await GetOpenPeriodAsync() != null;
        }

        public async Task<WorkPeriodStart?> GetOpenPeriodAsync()
        {
            using var db = new ClidaposDbContext();

            var latestStart = await db.WorkPeriodStarts
                .OrderByDescending(x => x.ID)
                .FirstOrDefaultAsync();

            if (latestStart == null)
                return null;

            var hasEnd = await db.WorkPeriodEnds.AnyAsync(e => e.Id == latestStart.ID);
            return hasEnd ? null : latestStart;
        }

        // The most recent period regardless of open/closed status - lets Report
        // show something useful even right after a period has been closed.
        public async Task<WorkPeriodStart?> GetLatestPeriodAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.WorkPeriodStarts.OrderByDescending(x => x.ID).FirstOrDefaultAsync();
        }

        public async Task StartPeriodAsync()
        {
            using var db = new ClidaposDbContext();
            db.WorkPeriodStarts.Add(new WorkPeriodStart
            {
                WPStart = DateTime.Now,
                Status = "Open"
            });
            await db.SaveChangesAsync();
        }

        public async Task<bool> EndPeriodAsync()
        {
            using var db = new ClidaposDbContext();

            var latestStart = await db.WorkPeriodStarts
                .OrderByDescending(x => x.ID)
                .FirstOrDefaultAsync();

            if (latestStart == null) return false;

            var alreadyClosed = await db.WorkPeriodEnds.AnyAsync(e => e.Id == latestStart.ID);
            if (alreadyClosed) return false;

            db.WorkPeriodEnds.Add(new WorkPeriodEnd
            {
                Id = latestStart.ID,
                WPEnd = DateTime.Now
            });

            latestStart.Status = "Closed";

            await db.SaveChangesAsync();
            return true;
        }
    }
}