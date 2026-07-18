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
        /// <summary>
        /// A shift is "open" if the most recent WorkPeriodStart row
        /// has no matching WorkPeriodEnd row.
        /// </summary>
        public async Task<bool> IsShiftOpenAsync()
        {
            return await GetOpenPeriodAsync() != null;
        }

        /// <summary>
        /// Returns the currently open work period, or null if none is open.
        /// Sales are stamped against this period.
        /// </summary>
        public async Task<WorkPeriodStart?> GetOpenPeriodAsync()
        {
            using var db = new ClidaposDbContext();

            var latestStart = await db.WorkPeriodStarts
                .OrderByDescending(x => x.ID)
                .FirstOrDefaultAsync();

            if (latestStart == null)
                return null;

            var hasEnd = await db.WorkPeriodEnds
                .AnyAsync(e => e.Id == latestStart.ID);

            return hasEnd ? null : latestStart;
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

        /// <summary>
        /// Closes the open work period. Returns false if nothing was open.
        /// </summary>
        public async Task<bool> EndPeriodAsync()
        {
            using var db = new ClidaposDbContext();

            var latestStart = await db.WorkPeriodStarts
                .OrderByDescending(x => x.ID)
                .FirstOrDefaultAsync();

            if (latestStart == null)
                return false;

            var alreadyClosed = await db.WorkPeriodEnds
                .AnyAsync(e => e.Id == latestStart.ID);

            if (alreadyClosed)
                return false;

            db.WorkPeriodEnds.Add(new WorkPeriodEnd
            {
                Id = latestStart.ID,          // shares the PK of the start row
                WPEnd = DateTime.Now
            });

            latestStart.Status = "Closed";

            await db.SaveChangesAsync();
            return true;
        }
    }
}