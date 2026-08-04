using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class StockAdjustmentListView : Window
    {
        private readonly StockAdjustmentService _stockAdjustmentService = new();
        private List<StockAdjustmentRow> _all = new();

        public StockAdjustmentListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _stockAdjustmentService.GetAllAsync();
            AdjustmentGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            AdjustmentGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(r => r.ProductName.ToLower().Contains(q)
                               || r.Warehouse.ToLower().Contains(q)).ToList();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var popup = new StockAdjustmentPopup();
            popup.Show();
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