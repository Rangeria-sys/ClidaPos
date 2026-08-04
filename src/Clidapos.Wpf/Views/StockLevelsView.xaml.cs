using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class StockLevelsView : Window
    {
        private readonly Registration _currentUser;
        private readonly StockLevelsService _stockLevelsService = new();
        private List<StockLevelRow> _all = new();

        public StockLevelsView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _stockLevelsService.GetStockLevelsAsync();
            StockGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            StockGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(r => r.ProductName.ToLower().Contains(q)
                               || r.ProductCode.ToLower().Contains(q)
                               || r.Category.ToLower().Contains(q)
                               || r.Warehouse.ToLower().Contains(q)).ToList();
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