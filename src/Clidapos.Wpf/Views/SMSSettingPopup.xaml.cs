using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SMSSettingPopup : Window
    {
        private readonly IntegrationSettingsService _settingsService = new();
        private readonly LogService _logService = new();
        private SMSSetting? _editing;

        public SMSSettingPopup(SMSSetting? editSetting = null)
        {
            InitializeComponent();

            if (editSetting != null)
            {
                _editing = editSetting;
                APIURLInput.Text = editSetting.APIURL?.Trim() ?? "";
                IsDefaultInput.Text = editSetting.IsDefault?.Trim() ?? "N";
                IsEnabledInput.Text = editSetting.IsEnabled?.Trim() ?? "Y";
            }
            else
            {
                IsDefaultInput.Text = "N";
                IsEnabledInput.Text = "Y";
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            APIURLInput.Text = "";
            IsDefaultInput.Text = "N";
            IsEnabledInput.Text = "Y";
            ErrorText.Text = "";
            APIURLInput.Focus();
        }

        private bool TryBuildSetting(out SMSSetting setting)
        {
            setting = new SMSSetting();
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(APIURLInput.Text))
            {
                ErrorText.Text = "API URL is required.";
                return false;
            }
            if (!APIURLInput.Text.Contains("{phone}") || !APIURLInput.Text.Contains("{message}"))
            {
                ErrorText.Text = "The URL should include {phone} and {message} placeholders.";
                return false;
            }

            setting.APIURL = APIURLInput.Text.Trim();
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
                ErrorText.Text = "Saved.";
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

            try
            {
                await _settingsService.UpdateSMSAsync(setting);
                await _logService.LogAsync(CurrentSession.UserId, "Updated SMS Gateway");
                ErrorText.Text = "Updated.";
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
            ErrorText.Text = "Removed.";
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}