using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class MpesaSettingPopup : Window
    {
        private readonly IntegrationSettingsService _settingsService = new();
        private readonly LogService _logService = new();
        private MpesaSetting? _setting;

        public MpesaSettingPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                _setting = await _settingsService.GetOrCreateMpesaAsync();
                ConsumerKeyInput.Text = _setting.ConsumerKey?.Trim() ?? "";
                ConsumerSecretInput.Text = _setting.ConsumerSecret?.Trim() ?? "";
                ShortcodeInput.Text = _setting.Shortcode?.Trim() ?? "";
                PassKeyInput.Text = _setting.PassKey?.Trim() ?? "";
                AccountNumberInput.Text = _setting.AccountNumber?.Trim() ?? "";
                EnvironmentInput.Text = _setting.Environment?.Trim() ?? "Sandbox - Paybill";
            };
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            if (_setting == null) { ErrorText.Text = "Settings haven't loaded yet."; return; }

            _setting.ConsumerKey = ConsumerKeyInput.Text.Trim();
            _setting.ConsumerSecret = ConsumerSecretInput.Text.Trim();
            _setting.Shortcode = ShortcodeInput.Text.Trim();
            _setting.PassKey = PassKeyInput.Text.Trim();
            _setting.AccountNumber = AccountNumberInput.Text.Trim();
            _setting.Environment = EnvironmentInput.Text.Trim();

            try
            {
                await _settingsService.SaveMpesaAsync(_setting);
                await _logService.LogAsync(CurrentSession.UserId, "Updated M-Pesa API Settings");
                MessageBox.Show("M-Pesa settings saved.", "Clidapos");
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