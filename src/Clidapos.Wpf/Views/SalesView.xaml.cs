using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SalesView : Window
    {
        private readonly Registration _currentUser;
        private readonly SaleService _saleService = new();
        private readonly ObservableCollection<CartLine> _cart = new();

        private bool _isDarkMode = true;

        public SalesView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            ApplyTheme();

            CashierText.Text = $"Cashier: {currentUser.Name.Trim()}";

            CartGrid.ItemsSource = _cart;
            RecomputeTotals();

            Loaded += (s, e) => SearchBox.Focus();
        }

        // ---------------- THEME ----------------
        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            _isDarkMode = !_isDarkMode;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Color Rgb(byte r, byte g, byte b) => Color.FromRgb(r, g, b);

            if (_isDarkMode)
            {
                Resources["PageBg"] = new SolidColorBrush(Rgb(0x0F, 0x0F, 0x12));
                Resources["SurfaceBg"] = new SolidColorBrush(Rgb(0x1E, 0x1E, 0x24));
                Resources["PanelBg"] = new SolidColorBrush(Rgb(0x1A, 0x1A, 0x1F));
                Resources["FieldBg"] = new SolidColorBrush(Rgb(0x14, 0x14, 0x19));
                Resources["RowAltBg"] = new SolidColorBrush(Rgb(0x1E, 0x1E, 0x24));
                Resources["BorderColor"] = new SolidColorBrush(Rgb(0x3A, 0x3A, 0x42));
                Resources["TextPrimary"] = Brushes.White;
                Resources["TextSecondary"] = new SolidColorBrush(Rgb(0x88, 0x88, 0x88));
                Resources["TextMuted"] = new SolidColorBrush(Rgb(0x66, 0x66, 0x66));
            }
            else
            {
                // Light mode text and borders are deliberately black/near-black and bold-weighted -
                // a POS till needs to read at a glance, so subtle gray isn't good enough here.
                Resources["PageBg"] = new SolidColorBrush(Rgb(0xEF, 0xEF, 0xF2));
                Resources["SurfaceBg"] = Brushes.White;
                Resources["PanelBg"] = Brushes.White;
                Resources["FieldBg"] = new SolidColorBrush(Rgb(0xF5, 0xF5, 0xF7));
                Resources["RowAltBg"] = new SolidColorBrush(Rgb(0xF5, 0xF5, 0xF7));
                Resources["BorderColor"] = Brushes.Black;
                Resources["TextPrimary"] = Brushes.Black;
                Resources["TextSecondary"] = new SolidColorBrush(Rgb(0x20, 0x20, 0x22));
                Resources["TextMuted"] = new SolidColorBrush(Rgb(0x3A, 0x3A, 0x3E));
            }
        }

        // ---------------- SEARCH / SCAN ----------------
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var term = SearchBox.Text.Trim();
            if (term.Length < 1)
            {
                ResultsList.ItemsSource = null;
                return;
            }

            ResultsList.ItemsSource = await _saleService.SearchAsync(term);
        }

        // Enter key = scan/lookup path. Exact code match, or a single fuzzy match, adds immediately.
        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var term = SearchBox.Text.Trim();
            if (term.Length == 0) return;

            var exact = await _saleService.FindByCodeAsync(term);
            if (exact != null)
            {
                AddToCart(exact);
                ClearSearchUI();
                return;
            }

            var results = await _saleService.SearchAsync(term);
            if (results.Count == 1)
            {
                AddToCart(results[0]);
                ClearSearchUI();
            }
            else
            {
                ResultsList.ItemsSource = results;
                ErrorText.Text = results.Count == 0 ? $"Nothing found for \"{term}\"." : "";
            }
        }

        // Clicking a result adds it to the cart at qty 1 - adjust the quantity afterward in the cart itself.
        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsList.SelectedItem is Product p)
            {
                AddToCart(p);
                ClearSearchUI();
            }
        }

        private void ClearSearchUI()
        {
            SearchBox.Clear();
            ResultsList.ItemsSource = null;
            SearchBox.Focus();
        }

        // ---------------- CART ----------------
        private void AddToCart(Product p)
        {
            ErrorText.Text = "";

            var existing = _cart.FirstOrDefault(l => l.ProductId == p.PID);
            if (existing != null)
                existing.Quantity += 1;
            else
                _cart.Add(new CartLine
                {
                    ProductId = p.PID,
                    ProductName = p.ProductName.Trim(),
                    ProductCode = p.ProductCode.Trim(),
                    Category = p.Category?.Trim() ?? "",
                    Rate = p.Price,
                    Quantity = 1
                });

            CartGrid.Items.Refresh();
            RecomputeTotals();
        }

        // ---------------- LEFT ACTION STACK ----------------
        private void RemoveSelectedLine_Click(object sender, RoutedEventArgs e)
        {
            if (CartGrid.SelectedItem is CartLine line)
            {
                _cart.Remove(line);
                RecomputeTotals();
            }
            else
            {
                ErrorText.Text = "Select a cart line first.";
            }
        }

        // Quantity is edited directly in the grid now - this jumps straight into
        // edit mode on the selected row's Qty cell, saving a click.
        private void ChangeQty_Click(object sender, RoutedEventArgs e)
        {
            if (CartGrid.SelectedItem is not CartLine line)
            {
                ErrorText.Text = "Select a cart line first.";
                return;
            }

            var qtyColumn = CartGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Qty");
            if (qtyColumn == null) return;

            CartGrid.CurrentCell = new DataGridCellInfo(line, qtyColumn);
            CartGrid.BeginEdit();
        }

        private void HoldSale_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Hold Sale is coming soon - it will let you park this cart and serve another customer.",
                "Clidapos");
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            new SalesHistoryView(_currentUser).Show();
            Close();
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

            var confirm = MessageBox.Show("Void this sale? All lines will be cleared.", "Confirm Void",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            _cart.Clear();
            RecomputeTotals();
        }

        // ---------------- TOTALS (subtotal -> discount -> grand total -> VAT) ----------------
        private decimal Subtotal => Math.Round(_cart.Sum(l => l.Amount), 2);

        private decimal DiscountPercent
        {
            get
            {
                if (decimal.TryParse(DiscountInput?.Text, out var p) && p >= 0 && p <= 100)
                    return p;
                return 0;
            }
        }

        private decimal DiscountAmount => Math.Round(Subtotal * DiscountPercent / 100m, 2);

        private decimal GrandTotal => Math.Round(Subtotal - DiscountAmount, 2);

        private void DiscountInput_TextChanged(object sender, TextChangedEventArgs e) => RecomputeTotals();

        private void RecomputeTotals()
        {
            var subtotal = Subtotal;
            var discountAmt = DiscountAmount;
            var total = GrandTotal;
            var vatPercent = AppSettings.VatPercent;

            var taxable = vatPercent > 0 ? Math.Round(total / (1 + (vatPercent / 100m)), 2) : total;
            var vat = Math.Round(total - taxable, 2);

            SubtotalText.Text = subtotal.ToString("N2");
            DiscountAmountText.Text = discountAmt > 0
                ? $"- {AppSettings.CurrencySymbol} {discountAmt:N2} off"
                : "No discount applied";
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
                _cart.ToList(), _currentUser, mode, received, string.Empty, string.Empty, DiscountPercent);

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
            SearchBox.Clear();
            AmountReceivedInput.Clear();
            ResultsList.ItemsSource = null;
            ErrorText.Text = "";
            DiscountInput.Text = "0";
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