using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class BackupSettingPopup : Window
    {
        private readonly BackupService _backupService = new();
        private readonly BackupScheduleService _scheduleService = new();
        private readonly LogService _logService = new();
        private bool _loaded;

        public BackupSettingPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                await LoadSchedule();
                await LoadHistory();
                _loaded = true;
            };
        }

        private async System.Threading.Tasks.Task LoadSchedule()
        {
            var schedule = await _scheduleService.LoadAsync();

            FolderInput.Text = schedule.FolderPath;
            AutoEnabledCheck.IsChecked = schedule.Enabled;
            IntervalInput.Text = schedule.IntervalHours.ToString();

            LastAutoRunText.Text = schedule.LastRunUtc == null
                ? "Automatic backup has not run yet."
                : $"Last automatic backup: {schedule.LastRunUtc.Value.ToLocalTime():dd MMM yyyy, hh:mm tt}";
        }

        private async System.Threading.Tasks.Task LoadHistory()
        {
            try
            {
                HistoryGrid.ItemsSource = await _backupService.GetBackupHistoryAsync();
            }
            catch
            {
                // Informational only - if msdb isn't reachable, the grid just stays empty.
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Choose a Backup Folder",
                Multiselect = false
            };

            if (System.IO.Directory.Exists(FolderInput.Text.Trim()))
                dialog.InitialDirectory = FolderInput.Text.Trim();

            if (dialog.ShowDialog() == true)
            {
                FolderInput.Text = dialog.FolderName;
                ErrorText.Text = "";
            }
        }

        // Just clears any stale error while the user is adjusting settings - actual
        // saving only happens on the explicit Save Schedule click.
        private void ScheduleChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            ErrorText.Text = "";
        }

        private async void SaveSchedule_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (!int.TryParse(IntervalInput.Text, out var hours) || hours < 1)
            {
                ErrorText.Text = "Repeat interval must be a whole number of hours, 1 or more.";
                return;
            }

            var folder = FolderInput.Text.Trim();
            if (AutoEnabledCheck.IsChecked == true && string.IsNullOrEmpty(folder))
            {
                ErrorText.Text = "Enter a backup folder before enabling automatic backup.";
                return;
            }

            var schedule = await _scheduleService.LoadAsync();
            schedule.Enabled = AutoEnabledCheck.IsChecked == true;
            schedule.IntervalHours = hours;
            schedule.FolderPath = folder;

            await _scheduleService.SaveAsync(schedule);
            await _logService.LogAsync(CurrentSession.UserId,
                $"Updated automatic backup schedule ({(schedule.Enabled ? "enabled" : "disabled")}, every {hours}h)");

            // Make sure the background checker is running now that a schedule exists.
            AutoBackupScheduler.EnsureStarted();

            MessageBox.Show("Backup schedule saved.", "Clidapos");
        }

        private async void BackupNow_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            var folder = FolderInput.Text.Trim();
            if (string.IsNullOrEmpty(folder))
            {
                ErrorText.Text = "Enter a backup folder path.";
                return;
            }

            BackupButtonText.Text = "Backing up...";

            var result = await _backupService.RunBackupAsync(folder);

            BackupButtonText.Text = "Backup Now";

            if (!result.Ok)
            {
                ErrorText.Text = $"Backup failed: {result.Error}";
                return;
            }

            await _logService.LogAsync(CurrentSession.UserId, $"Ran database backup to {result.FilePath}");

            MessageBox.Show($"Backup completed:\n\n{result.FilePath}", "Clidapos");

            await LoadHistory();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}