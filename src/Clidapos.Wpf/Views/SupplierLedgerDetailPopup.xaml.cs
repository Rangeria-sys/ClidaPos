using System;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SupplierLedgerDetailPopup : Window
    {
        private readonly SupplierLedgerService _ledgerService = new();
        private readonly LogService _logService = new();
        private readonly string _supplierCode;
        private readonly string _supplierName;

        /// <summary>Fired after a payment is successfully recorded, so the screen
        /// that opened this popup (the Ledger list) can refresh its balances
        /// immediately instead of only showing them from when it first loaded.</summary>
        public event EventHandler? BalanceChanged;

        public SupplierLedgerDetailPopup(string supplierCode, string supplierName)
        {
            InitializeComponent();
            _supplierCode = supplierCode;
            _supplierName = supplierName;
            SupplierNameText.Text = supplierName;

            Loaded += async (s, e) => await LoadHistory();
        }

        private async System.Threading.Tasks.Task LoadHistory()
        {
            var entries = await _ledgerService.GetEntriesForSupplierAsync(_supplierCode);
            HistoryGrid.ItemsSource = entries;

            var balance = 0m;
            foreach (var entry in entries)
                balance += entry.Credit - entry.Debit;

            BalanceText.Text = balance.ToString("N2");
        }

        private async void RecordPayment_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(LabelInput.Text))
            {
                ErrorText.Text = "Enter a description for this payment.";
                return;
            }

            if (!decimal.TryParse(AmountInput.Text, out var amount) || amount <= 0)
            {
                ErrorText.Text = "Enter a valid amount greater than zero.";
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you've paid '{_supplierName}' {amount:N2}?\n\n{LabelInput.Text.Trim()}",
                "Confirm Payment", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            await _ledgerService.AddManualEntryAsync(_supplierCode, _supplierName, LabelInput.Text.Trim(), amount, 0);
            await _logService.LogAsync(CurrentSession.UserId,
                $"Recorded payment to Supplier '{_supplierName}' - {amount:N2} ({LabelInput.Text.Trim()})");

            LabelInput.Clear();
            AmountInput.Clear();
            await LoadHistory();
            BalanceChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static readonly Regex NumericPattern = new(@"^[0-9]*\.?[0-9]*$");

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                e.Handled = true;
                return;
            }

            var proposedText = textBox.Text
                .Remove(textBox.SelectionStart, textBox.SelectionLength)
                .Insert(textBox.SelectionStart, e.Text);

            e.Handled = !NumericPattern.IsMatch(proposedText);
        }

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                SystemSounds.Exclamation.Play();
            }
        }
    }
}
