using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    /// <summary>Every method here is scoped to the terminal it's called from
    /// (Environment.MachineName), since all terminals share one central
    /// database. Without this scoping, one till having an open period would
    /// incorrectly block or interfere with every other till in the store.</summary>
    public class ShiftService
    {
        private static string TerminalId => Environment.MachineName;

        public async Task<bool> IsShiftOpenAsync()
        {
            return await GetOpenPeriodAsync() != null;
        }

        public async Task<WorkPeriodStart?> GetOpenPeriodAsync()
        {
            using var db = new ClidaposDbContext();

            var latestStart = await db.WorkPeriodStarts
                .Where(x => x.TerminalID == TerminalId)
                .OrderByDescending(x => x.ID)
                .FirstOrDefaultAsync();

            if (latestStart == null)
                return null;

            var hasEnd = await db.WorkPeriodEnds.AnyAsync(e => e.Id == latestStart.ID);
            return hasEnd ? null : latestStart;
        }

        // The most recent period on this terminal, regardless of open/closed
        // status - lets Report show something useful even right after a
        // period has been closed.
        public async Task<WorkPeriodStart?> GetLatestPeriodAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.WorkPeriodStarts
                .Where(x => x.TerminalID == TerminalId)
                .OrderByDescending(x => x.ID)
                .FirstOrDefaultAsync();
        }

        /// <summary>Every period ever recorded on this terminal, most recent
        /// first - for a picker letting the user browse previous work
        /// periods' reports, not just today's.</summary>
        public async Task<System.Collections.Generic.List<WorkPeriodStart>> GetAllPeriodsAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.WorkPeriodStarts
                .Where(x => x.TerminalID == TerminalId)
                .OrderByDescending(x => x.ID)
                .ToListAsync();
        }

        /// <summary>Starts a new period on this terminal. Returns false without
        /// starting anything if one is already open on this terminal - the
        /// period is store-wide for the day, not tied to one cashier; shift
        /// handoffs happen by logging out/in, tracked separately via Clock
        /// In/Out. cashierUserId/cashierName just record who happened to open
        /// it, for the report - not an access restriction.</summary>
        public async Task<bool> StartPeriodAsync(string? cashierUserId, string? cashierName, decimal? openingCash = null)
        {
            using var db = new ClidaposDbContext();

            var open = await GetOpenPeriodAsync();
            if (open != null) return false;

            db.WorkPeriodStarts.Add(new WorkPeriodStart
            {
                WPStart = DateTime.Now,
                Status = "Open",
                OpeningCash = openingCash,
                CashierUserID = cashierUserId,
                CashierName = cashierName,
                TerminalID = TerminalId
            });
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EndPeriodAsync(decimal? closingCash = null)
        {
            using var db = new ClidaposDbContext();

            var latestStart = await db.WorkPeriodStarts
                .Where(x => x.TerminalID == TerminalId)
                .OrderByDescending(x => x.ID)
                .FirstOrDefaultAsync();

            if (latestStart == null) return false;

            var alreadyClosed = await db.WorkPeriodEnds.AnyAsync(e => e.Id == latestStart.ID);
            if (alreadyClosed) return false;

            db.WorkPeriodEnds.Add(new WorkPeriodEnd
            {
                Id = latestStart.ID,
                WPEnd = DateTime.Now,
                ClosingCash = closingCash
            });

            latestStart.Status = "Closed";

            await db.SaveChangesAsync();
            return true;
        }
    }
}
