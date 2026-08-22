using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class StockAdjustmentListView : Window
    {
        private readonly StockLevelsService _stockLevelsService = new();
        private List<StockLevelRow> _all = new();

        public StockAdjustmentListView()
        {
            InitializeComponent();
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
                               || r.Category.ToLower().Contains(q)).ToList();
        }

        private void StockGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (StockGrid.SelectedItem is StockLevelRow row)
            {
                var popup = new StockAdjustmentPopup(row.ProductID, row.ProductName);
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