using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class AccountingReportView : Window
    {
        private readonly Registration _currentUser;
        private readonly AccountingService _accountingService = new();
        private List<AccountBalanceRow> _all = new();

        public AccountingReportView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _accountingService.GetAccountBalancesAsync();
            AccountGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            AccountGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(a => a.AccountName.ToLower().Contains(q)).ToList();
        }

        private void AccountGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AccountGrid.SelectedItem is AccountBalanceRow row)
            {
                var popup = new AccountLedgerDetailPopup(row.AccountName) { Owner = this };
                popup.ShowDialog();
            }
        }

        private async void NewEntry_Click(object sender, RoutedEventArgs e)
        {
            var popup = new JournalEntryPopup { Owner = this };
            popup.ShowDialog();
            await LoadData();
        }

        private void AllEntries_Click(object sender, RoutedEventArgs e)
        {
            var popup = new JournalEntryListPopup { Owner = this };
            popup.ShowDialog();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var backOffice = new BackOfficeView(_currentUser);
            backOffice.Show();
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}