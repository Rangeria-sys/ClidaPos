using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Clidapos.Wpf.Services
{
    public class BackupSchedule
    {
        public bool Enabled { get; set; }
        public int IntervalHours { get; set; } = 24;
        public string FolderPath { get; set; } = @"C:\ClidaPos_Backups";
        public DateTime? LastRunUtc { get; set; }
    }

    /// <summary>
    /// Reads and writes the automatic backup schedule to a small local JSON file -
    /// separate from appsettings.json (which handles the DB connection), since this
    /// is a self-contained preference this feature fully owns.
    /// </summary>
    public class BackupScheduleService
    {
        private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "backup_schedule.json");

        public async Task<BackupSchedule> LoadAsync()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new BackupSchedule();

                var json = await File.ReadAllTextAsync(FilePath);
                return JsonSerializer.Deserialize<BackupSchedule>(json) ?? new BackupSchedule();
            }
            catch
            {
                // A corrupted or unreadable schedule file should never crash the app -
                // just fall back to "automatic backup off" until re-saved.
                return new BackupSchedule();
            }
        }

        public async Task SaveAsync(BackupSchedule schedule)
        {
            var json = JsonSerializer.Serialize(schedule, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(FilePath, json);
        }
    }
}