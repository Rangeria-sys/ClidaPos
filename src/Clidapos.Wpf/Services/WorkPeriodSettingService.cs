using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class WorkPeriodSettingService
    {
        public async Task<WorkPeriodSetting> GetOrCreateAsync()
        {
            using var db = new ClidaposDbContext();
            var s = await db.Set<WorkPeriodSetting>().FirstOrDefaultAsync();
            if (s != null) return s;
            s = new WorkPeriodSetting
            {
                DefaultStartTime = "08:00",
                DefaultEndTime = "20:00",
                AutoCloseEnabled = "N",
                ReminderMinutesBeforeClose = 15
            };
            db.Set<WorkPeriodSetting>().Add(s);
            await db.SaveChangesAsync();
            return s;
        }

        public async Task SaveAsync(WorkPeriodSetting setting)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<WorkPeriodSetting>().FirstOrDefaultAsync(x => x.Id == setting.Id);
            if (existing == null) return;
            existing.DefaultStartTime = setting.DefaultStartTime;
            existing.DefaultEndTime = setting.DefaultEndTime;
            existing.AutoCloseEnabled = setting.AutoCloseEnabled;
            existing.ReminderMinutesBeforeClose = setting.ReminderMinutesBeforeClose;
            await db.SaveChangesAsync();
        }
    }
}