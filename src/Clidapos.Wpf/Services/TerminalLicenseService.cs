using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class TerminalLicenseService
    {
        public async Task<TerminalSetting> GetOrCreateTerminalAsync()
        {
            using var db = new ClidaposDbContext();
            var s = await db.Set<TerminalSetting>().FirstOrDefaultAsync();
            if (s != null) return s;
            s = new TerminalSetting { TerminalName = "Till 1", ReceiptPaperWidth = "80mm" };
            db.Set<TerminalSetting>().Add(s);
            await db.SaveChangesAsync();
            return s;
        }

        public async Task SaveTerminalAsync(TerminalSetting setting)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<TerminalSetting>().FirstOrDefaultAsync(x => x.Id == setting.Id);
            if (existing == null) return;
            existing.TerminalName = setting.TerminalName;
            existing.PrinterName = setting.PrinterName;
            existing.ReceiptPaperWidth = setting.ReceiptPaperWidth;
            existing.ScannerNotes = setting.ScannerNotes;
            existing.WifiNetworkName = setting.WifiNetworkName;
            await db.SaveChangesAsync();
        }

        public async Task<LicenseSetting> GetOrCreateLicenseAsync()
        {
            using var db = new ClidaposDbContext();
            var s = await db.Set<LicenseSetting>().FirstOrDefaultAsync();
            if (s != null) return s;
            s = new LicenseSetting { IsActive = "N" };
            db.Set<LicenseSetting>().Add(s);
            await db.SaveChangesAsync();
            return s;
        }

        /// <summary>Local-only activation - stores the key and marks the installation
        /// as activated. No server call, no cryptographic validation.</summary>
        public async Task ActivateLicenseAsync(int id, string licenseKey, string? notes)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<LicenseSetting>().FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return;
            existing.LicenseKey = licenseKey;
            existing.ActivatedDate = DateTime.Now;
            existing.IsActive = "Y";
            existing.Notes = notes;
            await db.SaveChangesAsync();
        }

        public async Task DeactivateLicenseAsync(int id)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<LicenseSetting>().FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return;
            existing.IsActive = "N";
            await db.SaveChangesAsync();
        }
    }
}