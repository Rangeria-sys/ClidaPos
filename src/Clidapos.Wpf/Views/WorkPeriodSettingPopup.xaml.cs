using System;
using System.Globalization;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class WorkPeriodSettingPopup : Window
    {
        private readonly WorkPeriodSettingService _settingsService = new();
        private readonly LogService _logService = new();
        private WorkPeriodSetting? _setting;

        public WorkPeriodSettingPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                _setting = await _settingsService.GetOrCreateAsync();
                StartTimeInput.Text = _setting.DefaultStartTime?.Trim() ?? "08:00";
                EndTimeInput.Text = _setting.DefaultEndTime?.Trim() ?? "20:00";
                AutoCloseInput.Text = _setting.AutoCloseEnabled?.Trim() ?? "N";
                ReminderInput.Text = _setting.ReminderMinutesBeforeClose?.ToString() ?? "15";
            };
        }

        private static bool IsValidTime(string value) =>
            TimeSpan.TryParseExact(value.Trim(), "hh\\:mm", CultureInfo.InvariantCulture, out _);

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            if (_setting == null) { ErrorText.Text = "Settings haven't loaded yet."; return; }

            if (!IsValidTime(StartTimeInput.Text) || !IsValidTime(EndTimeInput.Text))
            {
                ErrorText.Text = "Start and End Time must be in HH:mm format, e.g. 08:00.";
                return;
            }
            if (!int.TryParse(ReminderInput.Text, out var reminderMinutes))
            {
                ErrorText.Text = "Reminder must be a valid number of minutes.";
                return;
            }

            _setting.DefaultStartTime = StartTimeInput.Text.Trim();
            _setting.DefaultEndTime = EndTimeInput.Text.Trim();
            _setting.AutoCloseEnabled = AutoCloseInput.Text.Trim();
            _setting.ReminderMinutesBeforeClose = reminderMinutes;

            try
            {
                await _settingsService.SaveAsync(_setting);
                await _logService.LogAsync(CurrentSession.UserId, "Updated Work Period Setting");
                MessageBox.Show("Work Period settings saved.", "Clidapos");
                Close();
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.InnerException?.Message ?? ex.Message;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}