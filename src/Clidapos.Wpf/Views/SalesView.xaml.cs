using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    // Turns a DataGridRow's 0-based AlternationIndex into a 1-based row number
    // for display in the Cart grid's row header column.
    public class RowNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int index) return (index + 1).ToString();
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    public partial class SalesView : Window
    {
        private readonly Registration _currentUser;
        private readonly SaleService _saleService = new();
        private readonly LogService _logService = new();
        private readonly CustomerLedgerService _customerLedgerService = new();
        private readonly HeldSaleService _heldSaleService = new();
        private readonly ReceiptService _receiptService = new();
        private readonly ObservableCollection<CartLine> _cart = new();

        public SalesView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            CashierText.Text = $"Cashier: {currentUser.Name.Trim()}";

            CartGrid.ItemsSource = _cart;
            RecomputeTotals();

            Loaded += async (s, e) =>
            {
                SearchBox.Focus();
                await LoadCreditCustomers();
            };
        }

        private async System.Threading.Tasks.Task LoadCreditCustomers()
        {
            using var db = new ClidaposDbContext();
            CreditCustomerCombo.ItemsSource = await db.Set<CreditCustomer>().OrderBy(c => c.Name).ToListAsync();
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

        // With items in the cart: parks them aside and clears the cart so another
        // customer can be served. With an empty cart: opens the list of already-held
        // sales so one can be picked up and resumed - one button covers both directions.
        private async void HoldSale_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (_cart.Count == 0)
            {
                var popup = new HeldSalesPopup { Owner = this };
                var resumed = popup.ShowDialog() == true;

                if (resumed && popup.Resumed != null)
                {
                    var held = popup.Resumed;

                    foreach (var item in held.Items)
                    {
                        _cart.Add(new CartLine
                        {
                            ProductId = item.ProductId,
                            ProductName = item.ProductName ?? "",
                            ProductCode = item.ProductCode ?? "",
                            Category = item.Category ?? "",
                            Rate = item.Rate,
                            Quantity = item.Quantity
                        });
                    }

                    DiscountInput.Text = held.Sale.DiscountPercent.ToString();
                    CartGrid.Items.Refresh();
                    RecomputeTotals();
                }
                return;
            }

            var confirm = MessageBox.Show(
                $"Hold this sale ({_cart.Count} line(s))? The cart will be cleared so you can serve another customer.",
                "Confirm Hold", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            var label = $"Held {DateTime.Now:hh:mm tt}";
            await _heldSaleService.HoldAsync(_cart.ToList(), _currentUser.Name.Trim(), DiscountPercent, "", label);
            await _logService.LogAsync(CurrentSession.UserId, $"Held a sale ({_cart.Count} line(s))");

            ResetSale();
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

        // Voiding an in-progress cart never touches the database (nothing was ever
        // saved) - but it still requires a reason and gets logged, matching the
        // same discipline used when voiding an already-completed sale in Sales History.
        private async void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            if (_cart.Count == 0) return;

            var popup = new VoidReasonPopup(_cart.Count, GrandTotal, AppSettings.CurrencySymbol) { Owner = this };
            if (popup.ShowDialog() != true) return;

            await _logService.LogAsync(CurrentSession.UserId,
                $"Voided in-progress cart ({_cart.Count} line(s), {AppSettings.CurrencySymbol} {GrandTotal:N2}) - Reason: {popup.Reason}");

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
            if (CashDetailsPanel == null || CreditDetailsPanel == null) return;
            CashDetailsPanel.Visibility = CashMode.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            CreditDetailsPanel.Visibility = CreditMode.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        // BankMode maps to the "Card" payment value so the Day End report's
        // existing Card bucket keeps working - only the button label reads "BANK".
        private string SelectedPaymentMode()
        {
            if (MpesaMode.IsChecked == true) return "M-Pesa";
            if (BankMode.IsChecked == true) return "Card";
            if (CreditMode.IsChecked == true) return "Credit";
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
            CreditCustomer? creditCustomer = null;

            if (mode == "M-Pesa")
            {
                var mpesaPopup = new MpesaPaymentPopup(GrandTotal, "Counter Sale", "Sale payment") { Owner = this };
                var completed = mpesaPopup.ShowDialog() == true;

                if (!completed || mpesaPopup.PaymentResult == null)
                {
                    ErrorText.Text = "M-Pesa payment was not completed. Sale not saved.";
                    return;
                }

                received = GrandTotal;
            }
            else if (mode == "Credit")
            {
                if (CreditCustomerCombo.SelectedItem is not CreditCustomer selected)
                {
                    ErrorText.Text = "Select the customer this credit sale is for.";
                    return;
                }

                creditCustomer = selected;
                received = 0; // nothing paid now - the full amount goes to their ledger balance
            }
            else if (mode != "Cash")
            {
                received = GrandTotal;
            }

            PayButton.IsEnabled = false;

            var customerName = creditCustomer?.Name?.Trim() ?? string.Empty;
            var customerPhone = creditCustomer?.ContactNo?.Trim() ?? string.Empty;

            var result = await _saleService.SaveSaleAsync(
                _cart.ToList(), _currentUser, mode, received, customerName, customerPhone, DiscountPercent);

            PayButton.IsEnabled = true;

            if (!result.Ok)
            {
                ErrorText.Text = result.Error;
                return;
            }

            await _logService.LogAsync(CurrentSession.UserId,
                $"Completed Sale {result.BillNo} - {AppSettings.CurrencySymbol} {result.GrandTotal:N2} ({mode})");

            // Best-effort: record the debt to the customer's ledger after the sale has
            // already succeeded. A failure here should never undo a completed sale.
            if (creditCustomer != null)
            {
                try
                {
                    await _customerLedgerService.AddCreditGivenAsync(
                        creditCustomer.CC_ID, $"Credit Sale {result.BillNo}", result.GrandTotal);
                }
                catch (Exception ledgerEx)
                {
                    var detail = ledgerEx.InnerException?.Message ?? ledgerEx.Message;
                    MessageBox.Show(
                        $"Sale {result.BillNo} was saved, but recording it to {creditCustomer.Name.Trim()}'s ledger failed:\n\n{detail}\n\n" +
                        "Please add it manually from Customer Ledger.",
                        "Clidapos", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            MessageBox.Show(
                $"Sale {result.BillNo} completed.\n\n" +
                $"Total: {AppSettings.CurrencySymbol} {result.GrandTotal:N2}\n" +
                (mode == "Credit"
                    ? $"Charged to: {creditCustomer?.Name.Trim()} (added to their credit balance)"
                    : $"Change: {AppSettings.CurrencySymbol} {result.Change:N2}"),
                "Clidapos");

            var printNow = MessageBox.Show("Print a receipt for this sale?", "Clidapos",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (printNow == MessageBoxResult.Yes)
                await _receiptService.PrintReceiptForNewSaleAsync(result.BillNo ?? "");

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
            CreditCustomerCombo.SelectedItem = null;
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
