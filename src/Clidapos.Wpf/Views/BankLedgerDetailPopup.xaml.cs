using System.Linq;
using System.Windows;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class BankLedgerDetailPopup : Window
    {
        private readonly BankingService _bankingService = new();
        private readonly LogService _logService = new();
        private readonly string _accountNo;
        private readonly string _accountName;

        public BankLedgerDetailPopup(string accountNo, string accountName)
        {
            InitializeComponent();
            _accountNo = accountNo;
            _accountName = accountName;
            AccountNameText.Text = accountName;

            Loaded += async (s, e) => await LoadHistory();
        }

        private async System.Threading.Tasks.Task LoadHistory()
        {
            // Balance rows already include the account's opening balance plus all
            // activity, so re-use that same real calculation here for consistency.
            var balances = await _bankingService.GetAccountBalancesAsync();
            var thisAccount = balances.FirstOrDefault(a => a.AccountNo == _accountNo);
            BalanceText.Text = (thisAccount?.RunningBalance ?? 0).ToString("N2");

            var entries = await _bankingService.GetLedgerEntriesForAccountAsync(_accountNo);
            HistoryGrid.ItemsSource = entries;
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

        private async void RecordDeposit_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetValidatedInput(out var amount)) return;

            await _bankingService.AddLedgerEntryAsync(_accountNo, LabelInput.Text.Trim(), debit: 0, credit: amount);
            await _logService.LogAsync(CurrentSession.UserId,
                $"Recorded deposit to Bank Account '{_accountName}' - {amount:N2} ({LabelInput.Text.Trim()})");

            LabelInput.Clear();
            AmountInput.Clear();
            await LoadHistory();
        }

        private async void RecordWithdrawal_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetValidatedInput(out var amount)) return;

            await _bankingService.AddLedgerEntryAsync(_accountNo, LabelInput.Text.Trim(), debit: amount, credit: 0);
            await _logService.LogAsync(CurrentSession.UserId,
                $"Recorded withdrawal from Bank Account '{_accountName}' - {amount:N2} ({LabelInput.Text.Trim()})");

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