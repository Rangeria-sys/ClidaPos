using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly ItemsExcelService _itemsExcelService = new();
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

        private async void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var quantities = await _productService.GetAllQuantitiesAsync();
            var buyingPrices = await _purchaseService.GetLatestBuyingPricesAsync();
            var products = _allRows.Select(r => r.Product).ToList();

            var path = _itemsExcelService.Export(products, quantities, buyingPrices);
            if (path != null)
            {
                MessageBox.Show(
                    "Exported. The same file, edited and saved, is what Import Excel reads back in:\n" +
                    "add new rows (leave ID blank) to create items, or edit a row's values to update that item.",
                    "Clidapos");
            }
        }

        private async void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            var result = await _itemsExcelService.ImportAsync(CurrentSession.UserId);
            if (result.Cancelled)
                return;

            var summary = new StringBuilder();
            summary.AppendLine($"Added: {result.Added}");
            summary.AppendLine($"Updated: {result.Updated}");

            if (result.HasErrors)
            {
                summary.AppendLine($"Skipped: {result.Errors.Count}");
                summary.AppendLine();
                summary.AppendLine("Issues:");
                foreach (var err in result.Errors.Take(20))
                    summary.AppendLine("• " + err);
                if (result.Errors.Count > 20)
                    summary.AppendLine($"...and {result.Errors.Count - 20} more.");
            }

            MessageBox.Show(summary.ToString(), "Import Complete",
                MessageBoxButton.OK,
                result.HasErrors ? MessageBoxImage.Warning : MessageBoxImage.Information);

            await LoadProducts();
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