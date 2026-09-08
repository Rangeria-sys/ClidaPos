using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class BankBranchListView : Window
    {
        private readonly Registration _currentUser;
        private readonly BankingService _bankingService = new();
        private List<BankBranch> _all = new();

        public BankBranchListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _bankingService.GetAllBranchesAsync();
            BranchGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            BranchGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(b => (b.BankName ?? "").ToLower().Contains(q)
                               || (b.BranchName ?? "").ToLower().Contains(q)).ToList();
        }

        private void AddBank_Click(object sender, RoutedEventArgs e)
        {
            var popup = new BankBranchPopup(_currentUser);
            popup.Show();
            Close();
        }

        private void BranchGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BranchGrid.SelectedItem is BankBranch branch)
            {
                var popup = new BankBranchPopup(_currentUser, branch);
                popup.Show();
                Close();
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
