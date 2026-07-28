using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        private Product? _stagedProduct;
        private string _qtyBuffer = "";

        public SalesView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            CashierText.Text = $"Cashier: {currentUser.Name.Trim()}";

            CartGrid.ItemsSource = _cart;
            UpdateQtyDisplay();
            RecomputeTotals();

            Loaded += (s, e) => SearchBox.Focus();
        }

        // ---------------- SEARCH ----------------
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var term = SearchBox.Text.Trim();
            if (term.Length < 2)
            {
                ResultsList.ItemsSource = null;
                return;
            }

            ResultsList.ItemsSource = await _saleService.SearchAsync(term);
        }

        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var term = SearchBox.Text.Trim();
            if (term.Length == 0) return;

            var exact = await _saleService.FindByCodeAsync(term);
            if (exact != null)
            {
                AddToCart(exact, ParseQtyOrDefault());
                FinishAddAndReset();
                return;
            }

            var results = await _saleService.SearchAsync(term);
            if (results.Count == 1)
            {
                StageProduct(results[0]);
                ResultsList.ItemsSource = null;
                SearchBox.Clear();
            }
            else
            {
                ResultsList.ItemsSource = results;
                ErrorText.Text = results.Count == 0 ? $"Nothing found for \"{term}\"." : "";
            }
        }

        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsList.SelectedItem is Product p)
                StageProduct(p);
        }

        private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultsList.SelectedItem is Product p)
            {
                StageProduct(p);
                AddStagedToCart_Click(sender, e);
            }
        }

        // ---------------- STAGING + KEYPAD ----------------
        private void StageProduct(Product p)
        {
            _stagedProduct = p;
            StagedNameText.Text = p.ProductName.Trim();
            StagedPriceText.Text = $"{p.Price:N2} each";
            _qtyBuffer = "";
            UpdateQtyDisplay();
            KeypadErrorText.Text = "";
        }

        private void Keypad_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag as string ?? "";

            switch (tag)
            {
                case "CLR":
                    _qtyBuffer = "";
                    break;
                case "DEL":
                    if (_qtyBuffer.Length > 0)
                        _qtyBuffer = _qtyBuffer.Substring(0, _qtyBuffer.Length - 1);
                    break;
                default:
                    if (_qtyBuffer.Length < 6)
                        _qtyBuffer += tag;
                    break;
            }

            UpdateQtyDisplay();
        }

        private void UpdateQtyDisplay()
        {
            QtyDisplayText.Text = string.IsNullOrEmpty(_qtyBuffer) ? "1" : _qtyBuffer;
        }

        private decimal ParseQtyOrDefault()
        {
            var text = string.IsNullOrEmpty(_qtyBuffer) ? "1" : _qtyBuffer;
            return decimal.TryParse(text, out var q) && q > 0 ? q : 1;
        }

        private void AddStagedToCart_Click(object sender, RoutedEventArgs e)
        {
            KeypadErrorText.Text = "";

            if (_stagedProduct == null)
            {
                KeypadErrorText.Text = "Select an item first.";
                return;
            }

            AddToCart(_stagedProduct, ParseQtyOrDefault());
            FinishAddAndReset();
        }

        private void FinishAddAndReset()
        {
            _stagedProduct = null;
            _qtyBuffer = "";
            UpdateQtyDisplay();
            StagedNameText.Text = "Search or scan an item";
            StagedPriceText.Text = "";
            SearchBox.Clear();
            ResultsList.ItemsSource = null;
            SearchBox.Focus();
        }

        // ---------------- CART ----------------
        private void AddToCart(Product p, decimal qty)
        {
            ErrorText.Text = "";

            var existing = _cart.FirstOrDefault(l => l.ProductId == p.PID);
            if (existing != null)
                existing.Quantity += qty;
            else
                _cart.Add(new CartLine
                {
                    ProductId = p.PID,
                    ProductName = p.ProductName.Trim(),
                    ProductCode = p.ProductCode.Trim(),
                    Category = p.Category?.Trim() ?? "",
                    Rate = p.Price,
                    Quantity = qty
                });

            CartGrid.Items.Refresh();
            RecomputeTotals();
        }

        private void RemoveLineButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CartLine line)
            {
                _cart.Remove(line);
                RecomputeTotals();
            }
        }

        private void CartGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var line in _cart.ToList())
                    if (line.Quantity <= 0) _cart.Remove(line);

                CartGrid.Items.Refresh();
                RecomputeTotals();
            }), System.Windows.Threading.DispatcherPriority.Background);
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

            var taxable = vatPercent > 0 ? Math.Round(total / (1 + (vatPercent / 100m)), 2) : total;
            var vat = Math.Round(total - taxable, 2);

            GrandTotalText.Text = total.ToString("N2");
            VatBreakdownText.Text = $"{AppSettings.CurrencySymbol} — incl. VAT {vat:N2} (net {taxable:N2})";
            ItemCountText.Text = $"{_cart.Count} line(s) · {_cart.Sum(l => l.Quantity):N2} item(s)";

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

        private void AmountReceivedInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                CompleteSale_Click(sender, e);
        }

        private void PaymentMode_Changed(object sender, RoutedEventArgs e)
        {
            if (CashDetailsPanel == null) return;
            CashDetailsPanel.Visibility = CashMode.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        // BankMode maps to the "Card" payment value so the Day End report's
        // existing Card bucket keeps working - only the button label reads "BANK".
        private string SelectedPaymentMode()
        {
            if (MpesaMode.IsChecked == true) return "M-Pesa";
            if (BankMode.IsChecked == true) return "Card";
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

            PayButton.IsEnabled = false;

            var result = await _saleService.SaveSaleAsync(
                _cart.ToList(), _currentUser, mode, received, string.Empty, string.Empty);

            PayButton.IsEnabled = true;

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
            _stagedProduct = null;
            _qtyBuffer = "";
            SearchBox.Clear();
            AmountReceivedInput.Clear();
            ResultsList.ItemsSource = null;
            ErrorText.Text = "";
            KeypadErrorText.Text = "";
            StagedNameText.Text = "Search or scan an item";
            StagedPriceText.Text = "";
            CashMode.IsChecked = true;
            UpdateQtyDisplay();
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

            new FrontOfficeHubView(_currentUser).Show();
            Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            var message = _cart.Count > 0
                ? $"There are {_cart.Count} line(s) in the cart worth {AppSettings.CurrencySymbol} {GrandTotal:N2}.\n\n" +
                  "Closing now will lose this sale. Close Clidapos anyway?"
                : "Close Clidapos?";

            var confirm = MessageBox.Show(message, "Confirm Exit", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
    }
}