using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class BankAccountListView : Window
    {
        private readonly BankingService _bankingService = new();
        private List<BankAccountRegistration> _all = new();

        public BankAccountListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _bankingService.GetAllAccountsAsync();
            AccountGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            AccountGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(a => (a.AccountName ?? "").ToLower().Contains(q)
                               || a.AccountNo.Trim().ToLower().Contains(q)).ToList();
        }

        private void AccountGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AccountGrid.SelectedItem is BankAccountRegistration account)
            {
                var popup = new BankAccountPopup(account);
                popup.Show();
                Close();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
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