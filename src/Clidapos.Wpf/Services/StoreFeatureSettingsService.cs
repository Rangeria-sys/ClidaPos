using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    /// <summary>Per-shop feature toggles - a singleton, one row. Read fresh from
    /// the database every time, so a change made in Back Office takes effect
    /// immediately without restarting the app.</summary>
    public class StoreFeatureSettingsService
    {
        public async Task<StoreFeatureSettings> GetOrCreateAsync()
        {
            using var db = new ClidaposDbContext();
            var s = await db.Set<StoreFeatureSettings>().FirstOrDefaultAsync();
            if (s != null) return s;

            s = new StoreFeatureSettings();
            db.Set<StoreFeatureSettings>().Add(s);
            await db.SaveChangesAsync();
            return s;
        }

        public async Task UpdateAsync(StoreFeatureSettings settings)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<StoreFeatureSettings>().FirstOrDefaultAsync(x => x.Id == settings.Id);
            if (existing == null) return;

            existing.EnableMpesaSTKPush = settings.EnableMpesaSTKPush;
            existing.EnableLoyaltyProgram = settings.EnableLoyaltyProgram;
            existing.EnableBankPayment = settings.EnableBankPayment;
            existing.EnableCreditPayment = settings.EnableCreditPayment;
            await db.SaveChangesAsync();
        }
    }
}
