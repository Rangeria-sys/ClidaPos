using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SupplierLedgerListView : Window
    {
        private readonly Registration _currentUser;
        private readonly SupplierLedgerService _ledgerService = new();
        private List<SupplierBalanceRow> _all = new();

        public SupplierLedgerListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _ledgerService.GetSupplierBalancesAsync();
            SupplierGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            SupplierGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(r => r.SupplierName.ToLower().Contains(q)
                               || r.SupplierCode.ToLower().Contains(q)).ToList();
        }

        private void SupplierGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SupplierGrid.SelectedItem is SupplierBalanceRow row)
            {
                // Show() is non-blocking, so this list's balances won't auto-refresh
                // while the detail popup is open - reopen this screen (or re-search)
                // after adding a manual entry to see the updated balance.
                var detail = new SupplierLedgerDetailPopup(row.SupplierCode, row.SupplierName);
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