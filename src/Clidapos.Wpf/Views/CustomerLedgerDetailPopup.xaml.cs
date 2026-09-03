using System;
using System.Windows;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class CustomerLedgerDetailPopup : Window
    {
        private readonly CustomerLedgerService _ledgerService = new();
        private readonly CreditCustomerService _customerService = new();
        private readonly LogService _logService = new();
        private readonly int _customerId;
        private readonly string _customerName;

        /// <summary>Fired after a credit or payment is successfully recorded, so the
        /// screen that opened this popup (the Ledger list) can refresh its balances
        /// immediately instead of only showing them from when it first loaded.</summary>
        public event EventHandler? BalanceChanged;

        public CustomerLedgerDetailPopup(int customerId, string customerName)
        {
            InitializeComponent();
            _customerId = customerId;
            _customerName = customerName;
            CustomerNameText.Text = customerName;

            Loaded += async (s, e) => await LoadHistory();
        }

        private async System.Threading.Tasks.Task LoadHistory()
        {
            var entries = await _ledgerService.GetEntriesForCustomerAsync(_customerId);
            HistoryGrid.ItemsSource = entries;

            // Balance must include Opening Balance to match what the Ledger list
            // shows - summing only ledger entries here would silently disagree
            // with GetCustomerBalancesAsync() for anyone with a non-zero opening figure.
            var customer = await _customerService.GetByIdAsync(_customerId);
            var balance = customer?.OpeningBalance ?? 0;

            foreach (var entry in entries)
                balance += (entry.Debit ?? 0) - (entry.Credit ?? 0);

            BalanceText.Text = balance.ToString("N2");
        }

        private bool TryGetValidatedInput(out decimal amount)
        {
            amount = 0;
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(LabelInput.Text))
            {
                ErrorText.Text = "Enter a description first.";
                return false;
            }

            if (!decimal.TryParse(AmountInput.Text, out amount) || amount <= 0)
            {
                ErrorText.Text = "Enter a valid amount greater than zero.";
                return false;
            }

            return true;
        }

        private async void RecordCredit_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetValidatedInput(out var amount)) return;

            await _ledgerService.AddCreditGivenAsync(_customerId, LabelInput.Text.Trim(), amount);
            await _logService.LogAsync(CurrentSession.UserId,
                $"Recorded credit given to Customer '{_customerName}' - {amount:N2} ({LabelInput.Text.Trim()})");

            LabelInput.Clear();
            AmountInput.Clear();
            await LoadHistory();
            BalanceChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void RecordPayment_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetValidatedInput(out var amount)) return;

            await _ledgerService.AddPaymentReceivedAsync(_customerId, LabelInput.Text.Trim(), amount);
            await _logService.LogAsync(CurrentSession.UserId,
                $"Recorded payment from Customer '{_customerName}' - {amount:N2} ({LabelInput.Text.Trim()})");

            LabelInput.Clear();
            AmountInput.Clear();
            await LoadHistory();
            BalanceChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}