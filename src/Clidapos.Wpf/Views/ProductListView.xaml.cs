using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public class ProductRow
    {
        public Product Product { get; set; } = null!;
        public decimal? BuyingPrice { get; set; }
    }

    public partial class ProductListView : Window
    {
        private readonly Registration _currentUser;
        private readonly ProductService _productService = new();
        private readonly PurchaseService _purchaseService = new();
        private List<ProductRow> _allRows = new();

        public ProductListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadProducts();
        }

        private async System.Threading.Tasks.Task LoadProducts()
        {
            var products = await _productService.GetAllAsync();
            var prices = await _purchaseService.GetLatestBuyingPricesAsync();

            _allRows = products.Select(p => new ProductRow
            {
                Product = p,
                BuyingPrice = prices.TryGetValue(p.PID, out var bp) ? bp : (decimal?)null
            }).ToList();

            ProductGrid.ItemsSource = _allRows;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = SearchBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(query))
            {
                ProductGrid.ItemsSource = _allRows;
                return;
            }

            ProductGrid.ItemsSource = _allRows.Where(r =>
                r.Product.ProductName.ToLower().Contains(query) ||
                r.Product.ProductCode.ToLower().Contains(query) ||
                (r.Product.Category ?? "").ToLower().Contains(query)
            ).ToList();
        }

        private void ProductGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProductGrid.SelectedItem is ProductRow row)
            {
                var itemsView = new ItemsView(_currentUser, row.Product);
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