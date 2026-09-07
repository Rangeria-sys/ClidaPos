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
            ApplyToGrid(_all);
        }

        private void ApplyToGrid(List<string> names)
        {
            WarehouseTypeGrid.ItemsSource = names
                .Select((name, index) => new NumberedRow { Number = index + 1, Name = name })
                .ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            ApplyToGrid(string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(c => c.ToLower().Contains(q)).ToList());
        }

        private void WarehouseTypeGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (WarehouseTypeGrid.SelectedItem is NumberedRow row)
            {
                var popup = new WarehouseTypePopup(row.Name);
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