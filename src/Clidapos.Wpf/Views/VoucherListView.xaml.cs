using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class VoucherListView : Window
    {
        private readonly Registration _currentUser;
        private readonly VoucherService _voucherService = new();
        private List<Voucher> _all = new();

        public VoucherListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _voucherService.GetAllAsync();
            VoucherGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            VoucherGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(v => v.VoucherNo.Trim().ToLower().Contains(q)
                               || (v.Name ?? "").ToLower().Contains(q)).ToList();
        }

        private void VoucherGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (VoucherGrid.SelectedItem is Voucher voucher)
            {
                var popup = new VoucherDetailPopup(voucher) { Owner = this };
                popup.ShowDialog();
            }
        }

        private async void NewVoucher_Click(object sender, RoutedEventArgs e)
        {
            var popup = new VoucherPopup { Owner = this };
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