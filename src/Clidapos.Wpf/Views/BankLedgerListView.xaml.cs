using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class BankLedgerListView : Window
    {
        private readonly Registration _currentUser;
        private readonly BankingService _bankingService = new();
        private List<BankAccountRow> _all = new();

        public BankLedgerListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _bankingService.GetAccountBalancesAsync();
            AccountGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            AccountGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(a => a.AccountName.ToLower().Contains(q)
                               || a.AccountNo.ToLower().Contains(q)
                               || a.BankName.ToLower().Contains(q)).ToList();
        }

        private void AccountGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AccountGrid.SelectedItem is BankAccountRow row)
            {
                // Show() is non-blocking - reopen this screen (or re-search) after
                // recording a transaction to see the updated balance here.
                var detail = new BankLedgerDetailPopup(row.AccountNo, row.AccountName);
                detail.Show();
            }
        }

        private async void RegisterAccount_Click(object sender, RoutedEventArgs e)
        {
            var popup = new BankAccountPopup { Owner = this };
            popup.ShowDialog();
            await LoadData();
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