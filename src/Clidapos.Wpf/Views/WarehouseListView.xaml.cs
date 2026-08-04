using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class WarehouseListView : Window
    {
        private readonly WarehouseService _warehouseService = new();
        private List<Warehouse> _all = new();

        public WarehouseListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _warehouseService.GetAllAsync();
            WarehouseGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            WarehouseGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(w => w.WarehouseName.Trim().ToLower().Contains(q)
                               || (w.City ?? "").Trim().ToLower().Contains(q)
                               || (w.WarehouseType ?? "").Trim().ToLower().Contains(q)).ToList();
        }

        private void WarehouseGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (WarehouseGrid.SelectedItem is Warehouse warehouse)
            {
                var popup = new WarehousePopup(warehouse);
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