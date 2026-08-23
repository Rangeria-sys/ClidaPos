using System;
using System.Windows;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class JournalEntryPopup : Window
    {
        private readonly AccountingService _accountingService = new();
        private readonly LogService _logService = new();

        public JournalEntryPopup()
        {
            InitializeComponent();
            DateInput.SelectedDate = DateTime.Today;

            Loaded += async (s, e) =>
            {
                var accounts = await _accountingService.GetDistinctAccountNamesAsync();
                DebitAccountInput.ItemsSource = accounts;
                CreditAccountInput.ItemsSource = accounts;
            };
        }

        private async void Post_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            var debitAccount = DebitAccountInput.Text.Trim();
            var creditAccount = CreditAccountInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(debitAccount))
            {
                ErrorText.Text = "Debit Account is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(creditAccount))
            {
                ErrorText.Text = "Credit Account is required.";
                return;
            }
            if (debitAccount.Equals(creditAccount, StringComparison.OrdinalIgnoreCase))
            {
                ErrorText.Text = "Debit and Credit accounts must be different.";
                return;
            }
            if (!decimal.TryParse(AmountInput.Text, out var amount) || amount <= 0)
            {
                ErrorText.Text = "Enter a valid Amount greater than zero.";
                return;
            }

            try
            {
                var date = DateInput.SelectedDate ?? DateTime.Today;
                await _accountingService.PostJournalEntryAsync(debitAccount, creditAccount, date, amount, RemarksInput.Text);
                await _logService.LogAsync(CurrentSession.UserId,
                    $"Posted Journal Entry: Dr {debitAccount} / Cr {creditAccount} - {amount:N2}");

                MessageBox.Show("Journal entry posted.", "Clidapos");
                Close();
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                ErrorText.Text = detail;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}