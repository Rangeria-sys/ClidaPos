using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class IntegrationSettingsService
    {
        // ---------- M-Pesa: genuinely a singleton, one row ----------
        public async Task<MpesaSetting> GetOrCreateMpesaAsync()
        {
            using var db = new ClidaposDbContext();
            var s = await db.Set<MpesaSetting>().FirstOrDefaultAsync();
            if (s != null) return s;
            s = new MpesaSetting { Environment = "Sandbox - Paybill" };
            db.Set<MpesaSetting>().Add(s);
            await db.SaveChangesAsync();
            return s;
        }

        public async Task SaveMpesaAsync(MpesaSetting setting)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<MpesaSetting>().FirstOrDefaultAsync(x => x.Id == setting.Id);
            if (existing == null) return;
            existing.ConsumerKey = setting.ConsumerKey;
            existing.ConsumerSecret = setting.ConsumerSecret;
            existing.Shortcode = setting.Shortcode;
            existing.PassKey = setting.PassKey;
            existing.AccountNumber = setting.AccountNumber;
            existing.Environment = setting.Environment;
            await db.SaveChangesAsync();
        }

        // ---------- Email: a real list of named server configs ----------
        public async Task<List<EmailSetting>> GetAllEmailAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Set<EmailSetting>().OrderBy(e => e.ServerName).ToListAsync();
        }

        public async Task<int> GetNextEmailIdAsync()
        {
            using var db = new ClidaposDbContext();
            var maxId = await db.Set<EmailSetting>().Select(e => (int?)e.Id).MaxAsync();
            return (maxId ?? 0) + 1;
        }

        public async Task AddEmailAsync(EmailSetting setting)
        {
            using var db = new ClidaposDbContext();
            if (setting.IsDefault?.Trim().ToUpper() == "Y")
                await ClearOtherEmailDefaultsAsync(db);

            db.Set<EmailSetting>().Add(setting);
            await db.SaveChangesAsync();
        }

        public async Task UpdateEmailAsync(EmailSetting setting)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<EmailSetting>().FirstOrDefaultAsync(x => x.Id == setting.Id);
            if (existing == null) return;

            if (setting.IsDefault?.Trim().ToUpper() == "Y")
                await ClearOtherEmailDefaultsAsync(db, setting.Id);

            existing.ServerName = setting.ServerName;
            existing.SMTPAddress = setting.SMTPAddress;
            existing.Username = setting.Username;
            existing.Password = setting.Password;
            existing.Port = setting.Port;
            existing.TLS_SSL_Required = setting.TLS_SSL_Required;
            existing.IsDefault = setting.IsDefault;
            existing.IsActive = setting.IsActive;
            await db.SaveChangesAsync();
        }

        public async Task DeleteEmailAsync(int id)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<EmailSetting>().FirstOrDefaultAsync(x => x.Id == id);
            if (existing != null)
            {
                db.Set<EmailSetting>().Remove(existing);
                await db.SaveChangesAsync();
            }
        }

        private async Task ClearOtherEmailDefaultsAsync(ClidaposDbContext db, int? exceptId = null)
        {
            var others = await db.Set<EmailSetting>()
                .Where(e => e.Id != exceptId)
                .ToListAsync();
            foreach (var o in others) o.IsDefault = "N";
        }

        /// <summary>The active email config to actually send through, if any.</summary>
        public async Task<EmailSetting?> GetActiveEmailConfigAsync()
        {
            using var db = new ClidaposDbContext();
            var configs = await db.Set<EmailSetting>().ToListAsync();
            return configs.FirstOrDefault(e => e.IsDefault?.Trim().ToUpper() == "Y" && e.IsActive?.Trim().ToUpper() == "Y")
                ?? configs.FirstOrDefault(e => e.IsActive?.Trim().ToUpper() == "Y");
        }

        // ---------- SMS: a real list of named gateway URL templates ----------
        public async Task<List<SMSSetting>> GetAllSMSAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Set<SMSSetting>().OrderByDescending(s => s.IsDefault).ToListAsync();
        }

        public async Task<int> GetNextSMSIdAsync()
        {
            using var db = new ClidaposDbContext();
            var maxId = await db.Set<SMSSetting>().Select(s => (int?)s.Id).MaxAsync();
            return (maxId ?? 0) + 1;
        }

        public async Task AddSMSAsync(SMSSetting setting)
        {
            using var db = new ClidaposDbContext();
            if (setting.IsDefault?.Trim().ToUpper() == "Y")
                await ClearOtherSMSDefaultsAsync(db);

            db.Set<SMSSetting>().Add(setting);
            await db.SaveChangesAsync();
        }

        public async Task UpdateSMSAsync(SMSSetting setting)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<SMSSetting>().FirstOrDefaultAsync(x => x.Id == setting.Id);
            if (existing == null) return;

            if (setting.IsDefault?.Trim().ToUpper() == "Y")
                await ClearOtherSMSDefaultsAsync(db, setting.Id);

            existing.APIURL = setting.APIURL;
            existing.IsDefault = setting.IsDefault;
            existing.IsEnabled = setting.IsEnabled;
            await db.SaveChangesAsync();
        }

        public async Task DeleteSMSAsync(int id)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<SMSSetting>().FirstOrDefaultAsync(x => x.Id == id);
            if (existing != null)
            {
                db.Set<SMSSetting>().Remove(existing);
                await db.SaveChangesAsync();
            }
        }

        private async Task ClearOtherSMSDefaultsAsync(ClidaposDbContext db, int? exceptId = null)
        {
            var others = await db.Set<SMSSetting>()
                .Where(s => s.Id != exceptId)
                .ToListAsync();
            foreach (var o in others) o.IsDefault = "N";
        }

        /// <summary>The gateway URL template to actually send SMS through, if any.</summary>
        public async Task<SMSSetting?> GetActiveSMSConfigAsync()
        {
            using var db = new ClidaposDbContext();
            var configs = await db.Set<SMSSetting>().ToListAsync();
            return configs.FirstOrDefault(s => s.IsDefault?.Trim().ToUpper() == "Y" && s.IsEnabled?.Trim().ToUpper() == "Y")
                ?? configs.FirstOrDefault(s => s.IsEnabled?.Trim().ToUpper() == "Y");
        }
    }
}