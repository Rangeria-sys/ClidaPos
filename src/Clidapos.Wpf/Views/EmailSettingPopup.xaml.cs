using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class EmailSettingPopup : Window
    {
        private readonly IntegrationSettingsService _settingsService = new();
        private readonly LogService _logService = new();
        private EmailSetting? _editing;

        public EmailSettingPopup(EmailSetting? editSetting = null)
        {
            InitializeComponent();

            if (editSetting != null)
            {
                _editing = editSetting;
                ServerNameInput.Text = editSetting.ServerName?.Trim() ?? "";
                SMTPAddressInput.Text = editSetting.SMTPAddress?.Trim() ?? "";
                UsernameInput.Text = editSetting.Username?.Trim() ?? "";
                PasswordInput.Text = editSetting.Password?.Trim() ?? "";
                PortInput.Text = editSetting.Port?.ToString() ?? "";
                TlsInput.Text = editSetting.TLS_SSL_Required?.Trim() ?? "Y";
                IsDefaultInput.Text = editSetting.IsDefault?.Trim() ?? "N";
                IsActiveInput.Text = editSetting.IsActive?.Trim() ?? "Y";
            }
            else
            {
                TlsInput.Text = "Y";
                IsDefaultInput.Text = "N";
                IsActiveInput.Text = "Y";
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            ServerNameInput.Text = "";
            SMTPAddressInput.Text = "";
            UsernameInput.Text = "";
            PasswordInput.Text = "";
            PortInput.Text = "";
            TlsInput.Text = "Y";
            IsDefaultInput.Text = "N";
            IsActiveInput.Text = "Y";
            ErrorText.Text = "";
            ServerNameInput.Focus();
        }

        private bool TryBuildSetting(out EmailSetting setting)
        {
            setting = new EmailSetting();
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(ServerNameInput.Text))
            {
                ErrorText.Text = "Server Name is required.";
                return false;
            }
            if (!int.TryParse(PortInput.Text, out var port))
            {
                ErrorText.Text = "Port must be a valid number.";
                return false;
            }

            setting.ServerName = ServerNameInput.Text.Trim();
            setting.SMTPAddress = SMTPAddressInput.Text.Trim();
            setting.Username = UsernameInput.Text.Trim();
            setting.Password = PasswordInput.Text.Trim();
            setting.Port = port;
            setting.TLS_SSL_Required = TlsInput.Text.Trim();
            setting.IsDefault = IsDefaultInput.Text.Trim();
            setting.IsActive = IsActiveInput.Text.Trim();
            return true;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!TryBuildSetting(out var setting)) return;

            try
            {
                await _settingsService.AddEmailAsync(setting);
                await _logService.LogAsync(CurrentSession.UserId, $"Added Email Server '{setting.ServerName}'");
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
                ErrorText.Text = "Use Get Data, pick a server, then edit and Update.";
                return;
            }
            if (!TryBuildSetting(out var setting)) return;
            setting.Id = _editing.Id;

            try
            {
                await _settingsService.UpdateEmailAsync(setting);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Email Server '{setting.ServerName}'");
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
                ErrorText.Text = "Use Get Data, pick a server, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove email server '{_editing.ServerName?.Trim()}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _settingsService.DeleteEmailAsync(_editing.Id);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Email Server '{_editing.ServerName?.Trim()}'");
            New_Click(sender, e);
            ErrorText.Text = "Removed.";
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}