using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public class CategoryBreakdownRow
    {
        public string Category { get; set; } = "";
        public int ProductCount { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalValue { get; set; }
    }

    public partial class StockReportView : Window
    {
        private readonly Registration _currentUser;
        private readonly StockLevelsService _stockLevelsService = new();
        private const decimal LowStockThreshold = 10;

        public StockReportView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadReport();
        }

        private async System.Threading.Tasks.Task LoadReport()
        {
            var rows = await _stockLevelsService.GetStockLevelsAsync();
            var cur = AppSettings.CurrencySymbol;

            var totalValue = rows.Sum(r => r.Qty * r.Price);
            var distinctProducts = rows.Select(r => r.ProductID).Distinct().Count();
            var totalUnits = rows.Sum(r => r.Qty);
            var lowStock = rows.Where(r => r.Qty <= LowStockThreshold).ToList();

            TotalValueText.Text = $"{cur} {totalValue:N2}";
            ProductCountText.Text = distinctProducts.ToString("N0");
            TotalUnitsText.Text = totalUnits.ToString("N2");
            LowStockCountText.Text = lowStock.Count.ToString("N0");

            var byCategory = rows
                .GroupBy(r => string.IsNullOrWhiteSpace(r.Category) ? "(uncategorized)" : r.Category)
                .Select(g => new CategoryBreakdownRow
                {
                    Category = g.Key,
                    ProductCount = g.Select(r => r.ProductID).Distinct().Count(),
                    TotalQty = g.Sum(r => r.Qty),
                    TotalValue = g.Sum(r => r.Qty * r.Price)
                })
                .OrderByDescending(c => c.TotalValue)
                .ToList();

            CategoryGrid.ItemsSource = byCategory;

            if (lowStock.Count > 0)
            {
                LowStockCard.Visibility = Visibility.Visible;
                LowStockGrid.ItemsSource = lowStock.OrderBy(r => r.Qty).ToList();
            }
            else
            {
                LowStockCard.Visibility = Visibility.Collapsed;
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(ReportContent, "Clidapos - Stock Report");
            }
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