using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class JournalEntryPopup : Window
    {
        private readonly AccountingService _accountingService = new();
        private readonly LogService _logService = new();
        private Dictionary<string, string> _knownAccountTypes = new();

        public JournalEntryPopup()
        {
            InitializeComponent();
            DateInput.SelectedDate = DateTime.Today;

            Loaded += async (s, e) =>
            {
                var accounts = await _accountingService.GetDistinctAccountNamesAsync();
                DebitAccountInput.ItemsSource = accounts;
                CreditAccountInput.ItemsSource = accounts;
                _knownAccountTypes = await _accountingService.GetAllAccountTypesAsync();
            };
        }

        // Auto-fills the type dropdown when an existing account is picked from
        // the list or typed to match one - a known account's type doesn't need
        // re-entering every time.
        private void DebitAccountInput_SelectionChanged(object sender, SelectionChangedEventArgs e) => AutoFillType(DebitAccountInput, DebitTypeInput);
        private void CreditAccountInput_SelectionChanged(object sender, SelectionChangedEventArgs e) => AutoFillType(CreditAccountInput, CreditTypeInput);
        private void DebitAccountInput_LostFocus(object sender, RoutedEventArgs e) => AutoFillType(DebitAccountInput, DebitTypeInput);
        private void CreditAccountInput_LostFocus(object sender, RoutedEventArgs e) => AutoFillType(CreditAccountInput, CreditTypeInput);

        private void AutoFillType(ComboBox accountInput, ComboBox typeInput)
        {
            var name = accountInput.Text.Trim();
            if (name.Length == 0) return;
            if (!_knownAccountTypes.TryGetValue(name, out var type)) return;

            foreach (ComboBoxItem item in typeInput.Items)
            {
                if (item.Content?.ToString() == type)
                {
                    typeInput.SelectedItem = item;
                    break;
                }
            }
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

            var debitType = (DebitTypeInput.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(debitType))
            {
                ErrorText.Text = "Pick a type for the Debit account (Asset/Liability/Equity/Income/Expense).";
                return;
            }
            var creditType = (CreditTypeInput.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(creditType))
            {
                ErrorText.Text = "Pick a type for the Credit account (Asset/Liability/Equity/Income/Expense).";
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
                await _accountingService.PostJournalEntryAsync(
                    debitAccount, debitType!, creditAccount, creditType!, date, amount, RemarksInput.Text);
                await _logService.LogAsync(CurrentSession.UserId,
                    $"Posted Journal Entry: Dr {debitAccount} ({debitType}) / Cr {creditAccount} ({creditType}) - {amount:N2}");

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
