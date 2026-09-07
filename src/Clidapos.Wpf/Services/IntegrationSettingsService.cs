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
            if (s == null)
            {
                s = new MpesaSetting { Environment = "Sandbox - Paybill" };
                db.Set<MpesaSetting>().Add(s);
                await db.SaveChangesAsync();
            }

            // Credentials are stored encrypted - decrypt here so every other
            // part of the app (MpesaService, the settings popup) always works
            // with plain values and never needs to know encryption exists.
            s.ConsumerKey = SecretEncryptionService.Decrypt(s.ConsumerKey);
            s.ConsumerSecret = SecretEncryptionService.Decrypt(s.ConsumerSecret);
            s.PassKey = SecretEncryptionService.Decrypt(s.PassKey);
            return s;
        }

        public async Task SaveMpesaAsync(MpesaSetting setting)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<MpesaSetting>().FirstOrDefaultAsync(x => x.Id == setting.Id);
            if (existing == null) return;
            existing.ConsumerKey = SecretEncryptionService.Encrypt(setting.ConsumerKey);
            existing.ConsumerSecret = SecretEncryptionService.Encrypt(setting.ConsumerSecret);
            existing.Shortcode = setting.Shortcode;
            existing.PassKey = SecretEncryptionService.Encrypt(setting.PassKey);
            existing.AccountNumber = setting.AccountNumber;
            existing.Environment = setting.Environment;
            await db.SaveChangesAsync();
        }

        // ---------- Email: a real list of named server configs ----------
        public async Task<List<EmailSetting>> GetAllEmailAsync()
        {
            using var db = new ClidaposDbContext();
            var all = await db.Set<EmailSetting>().OrderBy(e => e.ServerName).ToListAsync();
            foreach (var e in all) e.Password = SecretEncryptionService.Decrypt(e.Password);
            return all;
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

            setting.Password = SecretEncryptionService.Encrypt(setting.Password);
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
            existing.Password = SecretEncryptionService.Encrypt(setting.Password);
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
            var config = configs.FirstOrDefault(e => e.IsDefault?.Trim().ToUpper() == "Y" && e.IsActive?.Trim().ToUpper() == "Y")
                ?? configs.FirstOrDefault(e => e.IsActive?.Trim().ToUpper() == "Y");
            if (config != null) config.Password = SecretEncryptionService.Decrypt(config.Password);
            return config;
        }

        // ---------- SMS: a real list of named gateway URL templates ----------
        public async Task<List<SMSSetting>> GetAllSMSAsync()
        {
            using var db = new ClidaposDbContext();
            var all = await db.Set<SMSSetting>().OrderByDescending(s => s.IsDefault).ToListAsync();
            foreach (var s in all) s.APIURL = SecretEncryptionService.Decrypt(s.APIURL);
            return all;
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

            setting.APIURL = SecretEncryptionService.Encrypt(setting.APIURL);
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

            existing.APIURL = SecretEncryptionService.Encrypt(setting.APIURL);
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
            var config = configs.FirstOrDefault(s => s.IsDefault?.Trim().ToUpper() == "Y" && s.IsEnabled?.Trim().ToUpper() == "Y")
                ?? configs.FirstOrDefault(s => s.IsEnabled?.Trim().ToUpper() == "Y");
            if (config != null) config.APIURL = SecretEncryptionService.Decrypt(config.APIURL);
            return config;
        }
    }
}