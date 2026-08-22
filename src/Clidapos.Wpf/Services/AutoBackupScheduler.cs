using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Clidapos.Wpf.Services
{
    /// <summary>
    /// Owns a single background timer that checks every few minutes whether a scheduled
    /// backup is due, and runs it silently if so. Started once (idempotent - safe to call
    /// repeatedly), keeps running for as long as the app process stays alive, independent
    /// of which window is currently open.
    /// </summary>
    public static class AutoBackupScheduler
    {
        private static DispatcherTimer? _timer;
        private static readonly BackupService _backupService = new();
        private static readonly BackupScheduleService _scheduleService = new();
        private static bool _checking;

        public static void EnsureStarted()
        {
            if (_timer != null) return;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
            _timer.Tick += async (s, e) => await CheckAndRunIfDueAsync();
            _timer.Start();

            // Also check once immediately on startup, in case a scheduled backup
            // was missed while the app was closed.
            _ = CheckAndRunIfDueAsync();
        }

        private static async Task CheckAndRunIfDueAsync()
        {
            if (_checking) return; // avoid overlapping runs if a backup takes a while
            _checking = true;

            try
            {
                var schedule = await _scheduleService.LoadAsync();

                if (!schedule.Enabled || string.IsNullOrWhiteSpace(schedule.FolderPath))
                    return;

                var due = schedule.LastRunUtc == null
                    || DateTime.UtcNow >= schedule.LastRunUtc.Value.AddHours(schedule.IntervalHours);

                if (!due) return;

                var result = await _backupService.RunBackupAsync(schedule.FolderPath);

                if (result.Ok)
                {
                    schedule.LastRunUtc = DateTime.UtcNow;
                    await _scheduleService.SaveAsync(schedule);
                    await new LogService().LogAsync("System", $"Automatic backup completed: {result.FilePath}");
                }
                else
                {
                    await new LogService().LogAsync("System", $"Automatic backup failed: {result.Error}");
                }
            }
            finally
            {
                _checking = false;
            }
        }
    }
}