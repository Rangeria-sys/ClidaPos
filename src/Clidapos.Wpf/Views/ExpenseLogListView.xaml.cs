using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class ExpenseLogListView : Window
    {
        private readonly Registration _currentUser;
        private readonly VoucherService _voucherService = new();
        private List<VoucherSummaryRow> _allSummaries = new();
        private List<Voucher> _allVouchers = new();

        public ExpenseLogListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _allSummaries = await _voucherService.GetVoucherSummariesAsync();
            _allVouchers = await _voucherService.GetAllAsync();
            PaymentGrid.ItemsSource = _allSummaries;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            PaymentGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _allSummaries
                : _allSummaries.Where(v => v.Particulars.ToLower().Contains(q)
                                        || v.Name.ToLower().Contains(q)).ToList();
        }

        private void PaymentGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PaymentGrid.SelectedItem is VoucherSummaryRow row)
            {
                var voucher = _allVouchers.FirstOrDefault(v => v.ID == row.ID);
                if (voucher == null) return;

                var popup = new VoucherDetailPopup(voucher) { Owner = this };
                popup.ShowDialog();
            }
        }

        private async void RecordPayment_Click(object sender, RoutedEventArgs e)
        {
            var popup = new BillPaymentPopup { Owner = this };
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