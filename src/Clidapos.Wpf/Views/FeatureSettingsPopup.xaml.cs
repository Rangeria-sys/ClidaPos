using System;
using System.Windows;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class FeatureSettingsPopup : Window
    {
        private readonly StoreFeatureSettingsService _service = new();
        private readonly LogService _logService = new();
        private StoreFeatureSettings? _settings;

        public FeatureSettingsPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                _settings = await _service.GetOrCreateAsync();
                StkPushInput.Text = _settings.EnableMpesaSTKPush?.Trim().ToUpper() == "N" ? "Off" : "On";
                LoyaltyInput.Text = _settings.EnableLoyaltyProgram?.Trim().ToUpper() == "N" ? "Off" : "On";
                BankInput.Text = _settings.EnableBankPayment?.Trim().ToUpper() == "N" ? "Off" : "On";
                CreditInput.Text = _settings.EnableCreditPayment?.Trim().ToUpper() == "N" ? "Off" : "On";
            };
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;
            ErrorText.Text = "";

            _settings.EnableMpesaSTKPush = StkPushInput.Text.Trim() == "Off" ? "N" : "Y";
            _settings.EnableLoyaltyProgram = LoyaltyInput.Text.Trim() == "Off" ? "N" : "Y";
            _settings.EnableBankPayment = BankInput.Text.Trim() == "Off" ? "N" : "Y";
            _settings.EnableCreditPayment = CreditInput.Text.Trim() == "Off" ? "N" : "Y";

            try
            {
                await _service.UpdateAsync(_settings);
                await _logService.LogAsync(CurrentSession.UserId,
                    $"Updated Feature Settings - STK Push: {_settings.EnableMpesaSTKPush}, Loyalty: {_settings.EnableLoyaltyProgram}, " +
                    $"Bank: {_settings.EnableBankPayment}, Credit: {_settings.EnableCreditPayment}");
                MessageBox.Show("Saved. Changes apply immediately on every terminal.", "Clidapos");
                Close();
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                ErrorText.Text = detail;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
        }
    }
}
