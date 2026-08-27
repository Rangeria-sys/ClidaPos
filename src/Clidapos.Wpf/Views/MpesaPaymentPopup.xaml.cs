using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Clidapos.Wpf.Services;

// Note: this file is complete with added SMS + email receipt steps, sent
// automatically after a successful payment using the phone/email entered above.
// Email is optional - if left blank, no email receipt is attempted.

namespace Clidapos.Wpf.Views
{
    public partial class MpesaPaymentPopup : Window
    {
        private readonly MpesaService _mpesaService = new();
        private readonly SmsService _smsService = new();
        private readonly EmailService _emailService = new();
        private readonly LogService _logService = new();
        private readonly decimal _amount;
        private readonly string _accountReference;
        private readonly string _transactionDesc;

        private CancellationTokenSource? _cts;
        private DispatcherTimer? _elapsedTimer;
        private int _elapsedSeconds;

        /// <summary>Set once the flow completes successfully - the caller should check this
        /// after ShowDialog() returns to know whether to mark the sale as paid.</summary>
        public MpesaResult? PaymentResult { get; private set; }

        public MpesaPaymentPopup(decimal amount, string accountReference, string transactionDesc, string? defaultPhone = null)
        {
            InitializeComponent();
            _amount = amount;
            _accountReference = accountReference;
            _transactionDesc = transactionDesc;

            AmountText.Text = $"KSh {amount:N2}";
            if (!string.IsNullOrWhiteSpace(defaultPhone))
                PhoneInput.Text = defaultPhone;
        }

        private async void SendPush_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(PhoneInput.Text))
            {
                ErrorText.Text = "Enter the customer's phone number.";
                return;
            }

            EntryPanel.Visibility = Visibility.Collapsed;
            WaitingPanel.Visibility = Visibility.Visible;
            CloseButton.IsEnabled = false;

            _elapsedSeconds = 0;
            ElapsedText.Text = "0s";
            _elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _elapsedTimer.Tick += (s, args) =>
            {
                _elapsedSeconds++;
                ElapsedText.Text = $"{_elapsedSeconds}s";
            };
            _elapsedTimer.Start();

            _cts = new CancellationTokenSource();

            MpesaResult result;
            try
            {
                result = await _mpesaService.InitiateAndAwaitPaymentAsync(
                    _amount, PhoneInput.Text.Trim(), _accountReference, _transactionDesc, _cts.Token);
            }
            catch (Exception ex)
            {
                result = new MpesaResult { Success = false, Message = ex.Message };
            }

            _elapsedTimer.Stop();

            WaitingPanel.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;
            CloseButton.IsEnabled = true;

            if (result.Success)
            {
                PaymentResult = result;
                ResultIcon.Text = "✅";
                ResultText.Text = $"Payment of KSh {result.AmountPaid:N2} received.";
                await _logService.LogAsync(CurrentSession.UserId,
                    $"M-Pesa payment received for '{_accountReference}' - KSh {result.AmountPaid:N2}");

                // Best-effort SMS and email receipts - a failed or unconfigured gateway
                // should never block the sale, which has already succeeded at this point.
                try
                {
                    var receiptMessage = $"Payment of KSh {result.AmountPaid:N2} received for {_transactionDesc}. Thank you.";
                    var smsResult = await _smsService.SendAsync(PhoneInput.Text.Trim(), receiptMessage);

                    if (smsResult.Success)
                    {
                        ResultText.Text += "\nSMS receipt sent.";
                        await _logService.LogAsync(CurrentSession.UserId, "SMS receipt sent for M-Pesa payment");
                    }
                }
                catch
                {
                    // Silently skip - the payment itself already succeeded and that's what matters.
                }

                if (!string.IsNullOrWhiteSpace(EmailInput.Text))
                {
                    try
                    {
                        var emailBody = $"Payment of KSh {result.AmountPaid:N2} received for {_transactionDesc}. Thank you.";
                        var emailResult = await _emailService.SendAsync(EmailInput.Text.Trim(), "Payment Receipt", emailBody);

                        if (emailResult.Success)
                        {
                            ResultText.Text += "\nEmail receipt sent.";
                            await _logService.LogAsync(CurrentSession.UserId, "Email receipt sent for M-Pesa payment");
                        }
                    }
                    catch
                    {
                        // Silently skip - the payment itself already succeeded and that's what matters.
                    }
                }
            }
            else
            {
                ResultIcon.Text = "❌";
                ResultText.Text = result.Message;
            }
        }

        private void CancelWait_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = PaymentResult != null;
            Close();
        }
    }
}