using System.Windows;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class MpesaManualPaymentPopup : Window
    {
        private readonly IntegrationSettingsService _settingsService = new();
        private readonly decimal _amount;

        /// <summary>Set once the cashier confirms payment was received. Matches
        /// MpesaPaymentPopup's PaymentResult shape so CompleteSale_Click can
        /// treat both payment popups interchangeably.</summary>
        public MpesaResult? PaymentResult { get; private set; }

        public MpesaManualPaymentPopup(decimal amount)
        {
            InitializeComponent();
            _amount = amount;
            AmountText.Text = $"KSh {amount:N2}";

            Loaded += async (s, e) =>
            {
                var mpesa = await _settingsService.GetOrCreateMpesaAsync();
                ShortcodeText.Text = string.IsNullOrWhiteSpace(mpesa.Shortcode) ? "(not set)" : mpesa.Shortcode.Trim();

                if (string.IsNullOrWhiteSpace(mpesa.AccountNumber))
                    AccountRow.Visibility = Visibility.Collapsed;
                else
                    AccountText.Text = mpesa.AccountNumber.Trim();
            };
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            PaymentResult = new MpesaResult
            {
                Success = true,
                Message = "Confirmed manually by cashier - no STK push.",
                MpesaReceiptNumber = string.IsNullOrWhiteSpace(ReferenceInput.Text) ? null : ReferenceInput.Text.Trim(),
                AmountPaid = _amount
            };
            DialogResult = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
