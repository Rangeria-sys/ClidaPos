using System;
using System.Windows;
using System.Windows.Media;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class LicenseSettingPopup : Window
    {
        private readonly TerminalLicenseService _settingsService = new();
        private readonly LogService _logService = new();
        private LicenseSetting? _setting;

        public LicenseSettingPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                _setting = await _settingsService.GetOrCreateLicenseAsync();
                LicenseKeyInput.Text = "";
                NotesInput.Text = _setting.Notes?.Trim() ?? "";
                RefreshStatus();
            };
        }

        private void RefreshStatus()
        {
            if (_setting == null) return;

            var isActive = _setting.IsActive?.Trim().ToUpper() == "Y";
            var storedKey = _setting.LicenseKey?.Trim() ?? "";

            if (!isActive || string.IsNullOrWhiteSpace(storedKey))
            {
                StatusText.Text = "NOT ACTIVATED";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xD9, 0x38, 0x38));
                ExpiryText.Text = "";
                return;
            }

            // Re-validate and re-derive expiry from the stored key + activation date every
            // time - nothing about validity is trusted from the IsActive flag alone.
            if (!LicenseKeyService.TryValidate(storedKey, out var durationCode, out _))
            {
                StatusText.Text = "INVALID KEY ON RECORD";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xD9, 0x38, 0x38));
                ExpiryText.Text = "";
                return;
            }

            var activatedDate = _setting.ActivatedDate ?? DateTime.Now;
            var expiry = LicenseKeyService.ComputeExpiry(activatedDate, durationCode);

            if (expiry == null)
            {
                StatusText.Text = "ACTIVE (LIFETIME)";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x17, 0xA3, 0x98));
                ExpiryText.Text = "No expiry.";
                return;
            }

            var daysLeft = (expiry.Value.Date - DateTime.Today).Days;

            if (daysLeft < 0)
            {
                StatusText.Text = "EXPIRED";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xD9, 0x38, 0x38));
                ExpiryText.Text = $"Expired on {expiry.Value:dd MMM yyyy}. Enter a new key to renew.";
            }
            else
            {
                StatusText.Text = "ACTIVE";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x17, 0xA3, 0x98));
                ExpiryText.Text = $"Expires {expiry.Value:dd MMM yyyy} ({daysLeft} day(s) left).";
            }
        }

        private async void Activate_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            if (_setting == null) { ErrorText.Text = "Settings haven't loaded yet."; return; }

            if (string.IsNullOrWhiteSpace(LicenseKeyInput.Text))
            {
                ErrorText.Text = "Enter a license key to activate.";
                return;
            }

            if (!LicenseKeyService.TryValidate(LicenseKeyInput.Text.Trim(), out _, out var validationError))
            {
                ErrorText.Text = validationError;
                return;
            }

            try
            {
                await _settingsService.ActivateLicenseAsync(_setting.Id, LicenseKeyInput.Text.Trim(), NotesInput.Text.Trim());
                await _logService.LogAsync(CurrentSession.UserId, "License activated with a new key");

                _setting.LicenseKey = LicenseKeyInput.Text.Trim();
                _setting.ActivatedDate = DateTime.Now;
                _setting.IsActive = "Y";
                LicenseKeyInput.Text = "";
                RefreshStatus();
                ErrorText.Text = "Activated.";
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.InnerException?.Message ?? ex.Message;
            }
        }

        private async void Deactivate_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            if (_setting == null) { ErrorText.Text = "Settings haven't loaded yet."; return; }

            var confirm = MessageBox.Show("Deactivate this license?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _settingsService.DeactivateLicenseAsync(_setting.Id);
            await _logService.LogAsync(CurrentSession.UserId, "License deactivated");
            _setting.IsActive = "N";
            RefreshStatus();
            ErrorText.Text = "Deactivated.";
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}