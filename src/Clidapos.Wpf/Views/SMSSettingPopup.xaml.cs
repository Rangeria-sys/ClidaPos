using System;
using System.Media;
using System.Windows;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SMSSettingPopup : Window
    {
        private readonly IntegrationSettingsService _settingsService = new();
        private readonly LogService _logService = new();
        private SMSSetting? _editing;

        // Tracks whether the user has actually typed into APIURL - it stays
        // masked until touched, and TryBuildSetting must reuse the original
        // stored value for it otherwise, rather than reading the mask text.
        private bool _urlTouched;

        public SMSSettingPopup(SMSSetting? editSetting = null)
        {
            InitializeComponent();

            if (editSetting != null)
            {
                _editing = editSetting;
                APIURLInput.Text = SecretEncryptionService.Mask(editSetting.APIURL);
                IsDefaultInput.Text = editSetting.IsDefault?.Trim() ?? "N";
                IsEnabledInput.Text = editSetting.IsEnabled?.Trim() ?? "Y";
                _urlTouched = string.IsNullOrEmpty(editSetting.APIURL);

                // Attached only now, after the masked text above is already set -
                // attaching earlier would fire on that initial assignment itself.
                APIURLInput.TextChanged += (s, e) => _urlTouched = true;
            }
            else
            {
                IsDefaultInput.Text = "N";
                IsEnabledInput.Text = "Y";
                _urlTouched = true;
                APIURLInput.TextChanged += (s, e) => _urlTouched = true;
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            APIURLInput.Text = "";
            IsDefaultInput.Text = "N";
            IsEnabledInput.Text = "Y";
            ErrorText.Text = "";
            _urlTouched = true;
            APIURLInput.Focus();
        }

        private bool TryBuildSetting(out SMSSetting setting)
        {
            setting = new SMSSetting();
            ErrorText.Text = "";

            var url = _urlTouched ? APIURLInput.Text.Trim() : (_editing?.APIURL ?? "");
            if (string.IsNullOrWhiteSpace(url))
            {
                ErrorText.Text = "API URL is required.";
                return false;
            }
            if (!url.Contains("{phone}") || !url.Contains("{message}"))
            {
                ErrorText.Text = "The URL should include {phone} and {message} placeholders.";
                return false;
            }

            setting.APIURL = url;
            setting.IsDefault = IsDefaultInput.Text.Trim();
            setting.IsEnabled = IsEnabledInput.Text.Trim();
            return true;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!TryBuildSetting(out var setting)) return;

            try
            {
                await _settingsService.AddSMSAsync(setting);
                await _logService.LogAsync(CurrentSession.UserId, "Added SMS Gateway");
                New_Click(sender, e);
                MessageBox.Show("Saved.", "Clidapos");
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.InnerException?.Message ?? ex.Message;
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a gateway, then edit and Update.";
                return;
            }
            if (!TryBuildSetting(out var setting)) return;
            setting.Id = _editing.Id;

            var changeNote = _urlTouched ? "\n\nThe API URL is being changed." : "";
            var confirm = MessageBox.Show(
                $"Update this SMS gateway?{changeNote}\n\nIncorrect values will cause SMS sending to fail.",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _settingsService.UpdateSMSAsync(setting);
                await _logService.LogAsync(CurrentSession.UserId, "Updated SMS Gateway");
                MessageBox.Show("Updated.", "Clidapos");
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.InnerException?.Message ?? ex.Message;
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a gateway, then Delete.";
                return;
            }

            var confirm = MessageBox.Show("Remove this SMS gateway?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _settingsService.DeleteSMSAsync(_editing.Id);
            await _logService.LogAsync(CurrentSession.UserId, "Deleted SMS Gateway");
            New_Click(sender, e);
            MessageBox.Show("Removed.", "Clidapos");
        }

        private void GetData_Click(object sender, RoutedEventArgs e) => Close();

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                SystemSounds.Exclamation.Play();
            }
        }
    }
}
