using System;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class EmailSettingPopup : Window
    {
        private readonly IntegrationSettingsService _settingsService = new();
        private readonly LogService _logService = new();
        private EmailSetting? _editing;

        // Tracks whether the user has actually typed into Password - it stays
        // masked until touched, and Save must reuse the original stored value
        // for it otherwise, rather than reading the mask text off the TextBox.
        private bool _passwordTouched;

        public EmailSettingPopup(EmailSetting? editSetting = null)
        {
            InitializeComponent();

            if (editSetting != null)
            {
                _editing = editSetting;
                ServerNameInput.Text = editSetting.ServerName?.Trim() ?? "";
                SMTPAddressInput.Text = editSetting.SMTPAddress?.Trim() ?? "";
                UsernameInput.Text = editSetting.Username?.Trim() ?? "";
                PasswordInput.Text = SecretEncryptionService.Mask(editSetting.Password);
                PortInput.Text = editSetting.Port?.ToString() ?? "";
                TlsInput.Text = editSetting.TLS_SSL_Required?.Trim() ?? "Y";
                IsDefaultInput.Text = editSetting.IsDefault?.Trim() ?? "N";
                IsActiveInput.Text = editSetting.IsActive?.Trim() ?? "Y";
                _passwordTouched = string.IsNullOrEmpty(editSetting.Password);

                // Attached only now, after the masked text above is already set -
                // attaching earlier would fire on that initial assignment itself.
                PasswordInput.TextChanged += (s, e) => _passwordTouched = true;
            }
            else
            {
                TlsInput.Text = "Y";
                IsDefaultInput.Text = "N";
                IsActiveInput.Text = "Y";
                _passwordTouched = true;
                PasswordInput.TextChanged += (s, e) => _passwordTouched = true;
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
            _passwordTouched = true;
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
            var email = SMTPAddressInput.Text.Trim();
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorText.Text = "Enter a valid From Email address.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(UsernameInput.Text))
            {
                ErrorText.Text = "Username is required.";
                return false;
            }
            var willHavePassword = _passwordTouched
                ? !string.IsNullOrWhiteSpace(PasswordInput.Text)
                : !string.IsNullOrWhiteSpace(_editing?.Password);
            if (!willHavePassword)
            {
                ErrorText.Text = "Password is required.";
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
            setting.Password = _passwordTouched ? PasswordInput.Text.Trim() : (_editing?.Password ?? "");
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
                ErrorText.Text = "Use Get Data, pick a server, then edit and Update.";
                return;
            }
            if (!TryBuildSetting(out var setting)) return;
            setting.Id = _editing.Id;

            var confirm = MessageBox.Show($"Update email server '{setting.ServerName}'?",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _settingsService.UpdateEmailAsync(setting);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Email Server '{setting.ServerName}'");
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
                ErrorText.Text = "Use Get Data, pick a server, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove email server '{_editing.ServerName?.Trim()}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _settingsService.DeleteEmailAsync(_editing.Id);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Email Server '{_editing.ServerName?.Trim()}'");
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