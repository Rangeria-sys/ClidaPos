using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;

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
            using var db = new ClidaposDbContext();

            var latestStart = await db.WorkPeriodStarts
                .OrderByDescending(x => x.ID)
                .FirstOrDefaultAsync();

            if (latestStart == null)
                return false;

            var hasEnd = await db.WorkPeriodEnds
                .AnyAsync(e => e.Id == latestStart.ID);

            return !hasEnd;
        }

        public async Task StartPeriodAsync()
        {
            using var db = new ClidaposDbContext();
            db.WorkPeriodStarts.Add(new Entities.WorkPeriodStart
            {
                WPStart = System.DateTime.Now,
                Status = "Open"
            });
            await db.SaveChangesAsync();
        }
    }

}