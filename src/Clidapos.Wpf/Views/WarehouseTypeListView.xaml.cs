using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class WarehouseTypeListView : Window
    {
        private readonly WarehouseTypeService _warehouseTypeService = new();
        private List<string> _all = new();

        public WarehouseTypeListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _warehouseTypeService.GetAllAsync();
            WarehouseTypeGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            WarehouseTypeGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(c => c.ToLower().Contains(q)).ToList();
        }

        private void WarehouseTypeGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (WarehouseTypeGrid.SelectedItem is string name)
            {
                var popup = new WarehouseTypePopup(name);
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