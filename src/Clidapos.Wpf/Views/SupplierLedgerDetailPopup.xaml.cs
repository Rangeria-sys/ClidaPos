using System.Windows;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SupplierLedgerDetailPopup : Window
    {
        private readonly SupplierLedgerService _ledgerService = new();
        private readonly LogService _logService = new();
        private readonly string _supplierCode;
        private readonly string _supplierName;

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

            await _ledgerService.AddManualEntryAsync(_supplierCode, _supplierName, LabelInput.Text.Trim(), amount, 0);
            await _logService.LogAsync(CurrentSession.UserId,
                $"Recorded payment to Supplier '{_supplierName}' - {amount:N2} ({LabelInput.Text.Trim()})");

            LabelInput.Clear();
            AmountInput.Clear();

            await LoadHistory();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}