using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class ProductListView : Window
    {
        private readonly Registration _currentUser;
        private readonly ProductService _productService = new();
        private List<Product> _allProducts = new();

        public ProductListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadProducts();
        }

        private async System.Threading.Tasks.Task LoadProducts()
        {
            _allProducts = await _productService.GetAllAsync();
            ProductGrid.ItemsSource = _allProducts;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = SearchBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(query))
            {
                ProductGrid.ItemsSource = _allProducts;
                return;
            }

            ProductGrid.ItemsSource = _allProducts.Where(p =>
                p.ProductName.ToLower().Contains(query) ||
                p.ProductCode.ToLower().Contains(query) ||
                (p.Category ?? "").ToLower().Contains(query)
            ).ToList();
        }

        private void ProductGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProductGrid.SelectedItem is Product selected)
            {
                var itemsView = new ItemsView(_currentUser, selected);
                itemsView.Show();
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var itemsView = new ItemsView(_currentUser);
            itemsView.Show();
            Close();
        }
    }
}