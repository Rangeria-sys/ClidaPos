using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SupplierListView : Window
    {
        private readonly SupplierService _supplierService = new();
        private List<Supplier> _all = new();

        public SupplierListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _supplierService.GetAllAsync();
            SupplierGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            SupplierGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(s => s.Name.Trim().ToLower().Contains(q)
                               || s.SupplierID.Trim().ToLower().Contains(q)).ToList();
        }

        private void SupplierGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SupplierGrid.SelectedItem is Supplier supplier)
            {
                var popup = new SupplierPopup(supplier);
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