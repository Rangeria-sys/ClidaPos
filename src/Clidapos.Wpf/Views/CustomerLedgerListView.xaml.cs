using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class CustomerLedgerListView : Window
    {
        private readonly Registration _currentUser;
        private readonly CustomerLedgerService _ledgerService = new();
        private List<CustomerBalanceRow> _all = new();

        public CustomerLedgerListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _ledgerService.GetCustomerBalancesAsync();
            CustomerGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            CustomerGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(r => r.CustomerName.ToLower().Contains(q)
                               || r.CustomerCode.ToLower().Contains(q)).ToList();
        }

        private void CustomerGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CustomerGrid.SelectedItem is CustomerBalanceRow row)
            {
                // Show() is non-blocking - reopen this screen (or re-search) after
                // recording an entry to see the updated balance here.
                var detail = new CustomerLedgerDetailPopup(row.CustomerId, row.CustomerName);
                detail.Show();
            }
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