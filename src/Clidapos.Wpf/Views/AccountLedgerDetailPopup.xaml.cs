using System.Windows;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class AccountLedgerDetailPopup : Window
    {
        private readonly AccountingService _accountingService = new();

        public AccountLedgerDetailPopup(string accountName)
        {
            InitializeComponent();
            AccountNameText.Text = accountName;

            Loaded += async (s, e) =>
            {
                var entries = await _accountingService.GetLedgerForAccountAsync(accountName);
                HistoryGrid.ItemsSource = entries;

                var balance = 0m;
                foreach (var entry in entries)
                    balance += (entry.Debit ?? 0) - (entry.Credit ?? 0);

                BalanceText.Text = balance.ToString("N2");
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}