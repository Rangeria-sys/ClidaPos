using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SalesView : Window
    {
        private readonly Registration _currentUser;
        private readonly SaleService _saleService = new();
        private readonly ObservableCollection<CartLine> _cart = new();

        public SalesView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            CashierText.Text = $"Cashier: {currentUser.Name.Trim()}";
            VatNoteText.Text = $"All prices include VAT at {AppSettings.VatPercent:0.##}%";

            CartGrid.ItemsSource = _cart;
            RecomputeTotals();

            Loaded += (s, e) => SearchBox.Focus();
        }

        // ---------------- SEARCH ----------------
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var term = SearchBox.Text.Trim();
            if (term.Length < 2)
            {
                ResultsGrid.ItemsSource = null;
                return;
            }

            var results = await _saleService.SearchAsync(term);
            ResultsGrid.ItemsSource = results;
        }

        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var term = SearchBox.Text.Trim();
            if (term.Length == 0) return;

            var exact = await _saleService.FindByCodeAsync(term);
            if (exact != null)
            {
                AddToCart(exact);
                SearchBox.Clear();
                ResultsGrid.ItemsSource = null;
                return;
            }

            var results = await _saleService.SearchAsync(term);
            if (results.Count == 1)
            {
                AddToCart(results[0]);
                SearchBox.Clear();
                ResultsGrid.ItemsSource = null;
            }
            else
            {
                ResultsGrid.ItemsSource = results;
                if (results.Count == 0)
                    ErrorText.Text = $"Nothing found for \"{term}\".";
            }
        }

        private void ResultsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultsGrid.SelectedItem is Product p)
            {
                AddToCart(p);
                SearchBox.Clear();
                SearchBox.Focus();
            }
        }

        // ---------------- CART ----------------
        private void AddToCart(Product p)
        {
            ErrorText.Text = "";

            var existing = _cart.FirstOrDefault(l => l.ProductId == p.PID);
            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                _cart.Add(new CartLine
                {
                    ProductId = p.PID,
                    ProductName = p.ProductName.Trim(),
                    ProductCode = p.ProductCode.Trim(),
                    Category = p.Category?.Trim() ?? "",
                    Rate = p.Price,
                    Quantity = 1
                });
            }

            CartGrid.Items.Refresh();
            RecomputeTotals();
        }

        private void CartGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var line in _cart.ToList())
                {
                    if (line.Quantity <= 0)
                        _cart.Remove(line);
                }
                CartGrid.Items.Refresh();
                RecomputeTotals();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void RemoveLine_Click(object sender, RoutedEventArgs e)
        {
            if (CartGrid.SelectedItem is CartLine line)
            {
                _cart.Remove(line);
                RecomputeTotals();
            }
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            if (_cart.Count == 0) return;

            var confirm = MessageBox.Show("Clear all items from the cart?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            _cart.Clear();
            RecomputeTotals();
        }

        // ---------------- TOTALS ----------------
        private decimal GrandTotal => Math.Round(_cart.Sum(l => l.Amount), 2);

        private void RecomputeTotals()
        {
            var total = GrandTotal;
            var vatPercent = AppSettings.VatPercent;

            var taxable = vatPercent > 0
                ? Math.Round(total / (1 + (vatPercent / 100m)), 2)
                : total;
            var vat = Math.Round(total - taxable, 2);

            GrandTotalText.Text = total.ToString("N2");
            VatBreakdownText.Text = $"{AppSettings.CurrencySymbol} — includes VAT of {vat:N2}  (net {taxable:N2})";
            ItemCountText.Text = $"{_cart.Count} line(s), {_cart.Sum(l => l.Quantity):N2} item(s)";

            ComputeChange();
        }

        private void ComputeChange()
        {
            if (!decimal.TryParse(AmountReceivedInput.Text, out var received))
                received = 0;

            var change = received - GrandTotal;
            ChangeText.Text = change >= 0 ? change.ToString("N2") : "0.00";
            ChangeText.Foreground = change >= 0
                ? System.Windows.Media.Brushes.Orange
                : System.Windows.Media.Brushes.IndianRed;
        }

        private void AmountReceived_TextChanged(object sender, TextChangedEventArgs e) => ComputeChange();

        private void PaymentMode_Changed(object sender, RoutedEventArgs e)
        {
            if (CashPanel == null || RefPanel == null) return;

            var isCash = CashMode.IsChecked == true;
            CashPanel.Visibility = isCash ? Visibility.Visible : Visibility.Collapsed;
            RefPanel.Visibility = isCash ? Visibility.Collapsed : Visibility.Visible;
        }

        private string SelectedPaymentMode()
        {
            if (MpesaMode.IsChecked == true) return "M-Pesa";
            if (CardMode.IsChecked == true) return "Card";
            return "Cash";
        }

        // ---------------- COMPLETE ----------------
        private async void CompleteSale_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (_cart.Count == 0)
            {
                ErrorText.Text = "Add at least one item before completing the sale.";
                return;
            }

            var mode = SelectedPaymentMode();
            decimal.TryParse(AmountReceivedInput.Text, out var received);

            if (mode != "Cash")
                received = GrandTotal;

            CompleteButton.IsEnabled = false;

            var result = await _saleService.SaveSaleAsync(
                _cart.ToList(),
                _currentUser,
                mode,
                received,
                CustomerNameInput.Text,
                PhoneInput.Text);

            CompleteButton.IsEnabled = true;

            if (!result.Ok)
            {
                ErrorText.Text = result.Error;
                return;
            }

            MessageBox.Show(
                $"Sale {result.BillNo} completed.\n\n" +
                $"Total: {AppSettings.CurrencySymbol} {result.GrandTotal:N2}\n" +
                $"Change: {AppSettings.CurrencySymbol} {result.Change:N2}",
                "Clidapos");

            ResetSale();
        }

        private void NewSale_Click(object sender, RoutedEventArgs e) => ResetSale();

        private void ResetSale()
        {
            _cart.Clear();
            SearchBox.Clear();
            AmountReceivedInput.Clear();
            CustomerNameInput.Clear();
            PhoneInput.Clear();
            ResultsGrid.ItemsSource = null;
            ErrorText.Text = "";
            CashMode.IsChecked = true;
            RecomputeTotals();
            SearchBox.Focus();
        }

        // ---------------- CHROME ----------------
        private void BackToGateway_Click(object sender, RoutedEventArgs e)
        {
            if (_cart.Count > 0)
            {
                var confirm = MessageBox.Show("There are items in the cart. Leave anyway?", "Confirm",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;
            }

            new GatewayView(_currentUser).Show();
            Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            var message = _cart.Count > 0
                ? $"There are {_cart.Count} line(s) in the cart worth {AppSettings.CurrencySymbol} {GrandTotal:N2}.\n\n" +
                  "Closing now will lose this sale. Close Clidapos anyway?"
                : "Close Clidapos?";

            var confirm = MessageBox.Show(message, "Confirm Exit",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
    }
}