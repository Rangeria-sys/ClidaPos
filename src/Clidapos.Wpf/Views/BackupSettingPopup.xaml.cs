using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            IntervalInput.Text = schedule.IntervalMinutes.ToString();

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

            if (!int.TryParse(IntervalInput.Text, out var minutes) || minutes < 30)
            {
                ErrorText.Text = "Repeat interval must be a whole number of minutes, 30 or more - a full backup running more often than that can slow down checkout.";
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
            schedule.IntervalMinutes = minutes;
            schedule.FolderPath = folder;

            await _scheduleService.SaveAsync(schedule);
            await _logService.LogAsync(CurrentSession.UserId,
                $"Updated automatic backup schedule ({(schedule.Enabled ? "enabled" : "disabled")}, every {minutes} min)");

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

        private void BrowseRestoreFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose a Backup File to Restore",
                Filter = "SQL Server Backup (*.bak)|*.bak|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                RestoreFileInput.Text = dialog.FileName;
                RestoreBtn.IsEnabled = true;
                ErrorText.Text = "";
            }
        }

        private async void Restore_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            var filePath = RestoreFileInput.Text.Trim();
            if (string.IsNullOrEmpty(filePath))
            {
                ErrorText.Text = "Choose a backup file first.";
                return;
            }

            var confirm1 = MessageBox.Show(
                "This will completely REPLACE all current data with the selected backup.\n\n" +
                "Everything saved after that backup was made - every sale, every item, every " +
                "change - will be permanently lost. This cannot be undone.\n\n" +
                "Are you absolutely sure you want to continue?",
                "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm1 != MessageBoxResult.Yes) return;

            var confirm2 = MessageBox.Show(
                "Last chance to back out.\n\n" +
                "Once the restore starts, every other window and user connected to this " +
                "system will be disconnected, and Clidapos will need to be closed and " +
                "reopened afterward.\n\n" +
                "Proceed with the restore now?",
                "Final Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm2 != MessageBoxResult.Yes) return;

            RestoreBtn.IsEnabled = false;
            RestoreBtn.Content = "Restoring...";

            var result = await _backupService.RestoreBackupAsync(filePath);

            if (!result.Ok)
            {
                RestoreBtn.Content = "Restore Database";
                RestoreBtn.IsEnabled = true;
                ErrorText.Text = $"Restore failed: {result.Error}";
                return;
            }

            await _logService.LogAsync(CurrentSession.UserId, $"Restored database from {result.FilePath}");

            MessageBox.Show(
                "Restore completed successfully.\n\n" +
                "Clidapos will now close - please reopen it to continue with the restored data.",
                "Clidapos");

            Application.Current.Shutdown();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                SystemSounds.Exclamation.Play();
            }
        }
    }
}
