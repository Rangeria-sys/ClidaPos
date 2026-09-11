using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class StockReportView : Window
    {
        private readonly Registration _currentUser;
        private readonly StockLevelsService _stockLevelsService = new();
        private const int ValueAtRiskDisplayLimit = 5;

        public StockReportView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadReport();
        }

        private async System.Threading.Tasks.Task LoadReport()
        {
            var s = await _stockLevelsService.GetStockAnalyticsAsync();
            var cur = AppSettings.CurrencySymbol;

            ValueAtRetailText.Text = $"{cur} {s.ValueAtRetail:N0}";
            ValueAtCostText.Text = $"{cur} {s.ValueAtCost:N0}";
            MarginText.Text = $"{s.MarginPercent:N0}%";
            ProductCountText.Text = s.ProductCount.ToString("N0");
            UnitsOnHandText.Text = s.UnitsOnHand.ToString("N0");

            BuildReorderSoon(s);
            BuildValueAtRisk(s, cur);
            BuildCategoryBreakdown(s, cur);
            BuildDeadStock(s, cur);
        }

        private void BuildReorderSoon(StockAnalyticsSummary s)
        {
            ReorderList.Children.Clear();

            if (s.ReorderSoon.Count == 0)
            {
                ReorderEmptyText.Visibility = Visibility.Visible;
                return;
            }
            ReorderEmptyText.Visibility = Visibility.Collapsed;

            foreach (var p in s.ReorderSoon)
            {
                var row = new Grid { Margin = new Thickness(20, 8, 20, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var name = new TextBlock { Text = p.ProductName, Foreground = Brushes.Black, FontSize = 14 };
                var qty = new TextBlock { Text = p.Qty.ToString("N2"), Foreground = Brushes.Black, FontSize = 14 };
                var rate = new TextBlock { Text = $"{p.DailySalesRate:N1}/day", Foreground = Brushes.Black, FontSize = 14 };

                string daysLabel;
                Brush daysBrush;
                if (p.DaysLeft == 0)
                {
                    daysLabel = "Out now";
                    daysBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B));
                }
                else if (p.DaysLeft < 1)
                {
                    daysLabel = "< 1 day";
                    daysBrush = new SolidColorBrush(Color.FromRgb(0xB8, 0x86, 0x0B));
                }
                else
                {
                    daysLabel = $"{p.DaysLeft:N0} days";
                    daysBrush = new SolidColorBrush(Color.FromRgb(0x1B, 0x8A, 0x3D));
                }

                var days = new TextBlock
                {
                    Text = daysLabel, Foreground = daysBrush, FontSize = 14, FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                Grid.SetColumn(name, 0);
                Grid.SetColumn(qty, 1);
                Grid.SetColumn(rate, 2);
                Grid.SetColumn(days, 3);
                row.Children.Add(name);
                row.Children.Add(qty);
                row.Children.Add(rate);
                row.Children.Add(days);

                ReorderList.Children.Add(row);
            }
        }

        private void BuildValueAtRisk(StockAnalyticsSummary s, string cur)
        {
            RiskList.Children.Clear();

            if (s.ValueAtRisk.Count == 0)
            {
                RiskEmptyText.Visibility = Visibility.Visible;
                RiskMoreText.Visibility = Visibility.Collapsed;
                return;
            }
            RiskEmptyText.Visibility = Visibility.Collapsed;

            foreach (var p in s.ValueAtRisk.Take(ValueAtRiskDisplayLimit))
            {
                var row = new Grid { Margin = new Thickness(20, 8, 20, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var name = new TextBlock { Text = p.ProductName, Foreground = Brushes.Black, FontSize = 14 };
                var qty = new TextBlock { Text = p.Qty.ToString("N2"), Foreground = Brushes.Black, FontSize = 14 };
                var value = new TextBlock
                {
                    Text = $"{cur} {p.Value:N0}", Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B)),
                    FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right
                };

                Grid.SetColumn(name, 0);
                Grid.SetColumn(qty, 1);
                Grid.SetColumn(value, 2);
                row.Children.Add(name);
                row.Children.Add(qty);
                row.Children.Add(value);

                RiskList.Children.Add(row);
            }

            var remaining = s.ValueAtRisk.Count - ValueAtRiskDisplayLimit;
            if (remaining > 0)
            {
                RiskMoreText.Text = $"— {remaining} more item{(remaining == 1 ? "" : "s")} —";
                RiskMoreText.Visibility = Visibility.Visible;
            }
            else
            {
                RiskMoreText.Visibility = Visibility.Collapsed;
            }
        }

        private void BuildCategoryBreakdown(StockAnalyticsSummary s, string cur)
        {
            CategoryList.Children.Clear();

            var maxValue = s.ByCategory.Count > 0 ? s.ByCategory.Max(c => c.TotalValue) : 0;

            foreach (var c in s.ByCategory)
            {
                var block = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };

                var header = new Grid();
                var name = new TextBlock { Text = c.Category, Foreground = Brushes.Black, FontSize = 15, FontWeight = FontWeights.SemiBold };
                var detail = new TextBlock
                {
                    Text = $"{cur} {c.TotalValue:N0} · {c.ProductCount} product{(c.ProductCount == 1 ? "" : "s")}",
                    Foreground = Brushes.Gray, FontSize = 13, HorizontalAlignment = HorizontalAlignment.Right
                };
                header.Children.Add(name);
                header.Children.Add(detail);
                block.Children.Add(header);

                var barTrack = new Border { Background = new SolidColorBrush(Color.FromRgb(0xEC, 0xEC, 0xEE)), CornerRadius = new CornerRadius(6), Height = 12, Margin = new Thickness(0, 8, 0, 0) };
                var fillWidth = maxValue == 0 ? 0 : (double)(c.TotalValue / maxValue);
                var barFill = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x0E, 0x5E, 0x5A)),
                    CornerRadius = new CornerRadius(6),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 900 * Math.Max(0.02, fillWidth)
                };
                var barGrid = new Grid();
                barGrid.Children.Add(barTrack);
                barGrid.Children.Add(barFill);
                block.Children.Add(barGrid);

                CategoryList.Children.Add(block);
            }

            CategoryHintText.Text = s.ByCategory.Count <= 1
                ? "Only one category active right now — this chart fills in as more categories are added."
                : "";
        }

        private void BuildDeadStock(StockAnalyticsSummary s, string cur)
        {
            DeadStockList.Children.Clear();

            if (s.DeadStock.Count == 0)
            {
                DeadStockEmptyText.Visibility = Visibility.Visible;
                return;
            }
            DeadStockEmptyText.Visibility = Visibility.Collapsed;

            foreach (var p in s.DeadStock)
            {
                var row = new Grid { Margin = new Thickness(0, 6, 0, 6) };
                var name = new TextBlock { Text = p.ProductName, Foreground = Brushes.Black, FontSize = 14 };
                var value = new TextBlock
                {
                    Text = $"{p.Qty:N0} units · {cur} {p.Value:N0} tied up", Foreground = Brushes.Gray, FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                row.Children.Add(name);
                row.Children.Add(value);
                DeadStockList.Children.Add(row);
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
            new BackOfficeView(_currentUser).Show();
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            new BackOfficeView(_currentUser).Show();
            Close();
        }
    }
}
