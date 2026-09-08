using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    /// <summary>
    /// The closed ComboBox box relies on WPF auto-generating a display template
    /// from DisplayMemberPath - in practice this doesn't reliably fire for a
    /// custom-templated ComboBox, and falls back to the raw object's ToString()
    /// (showing the full class name). This converter sidesteps that entirely by
    /// reading "Name" or "WarehouseName" directly via reflection.
    /// </summary>
    public class ComboDisplayNameConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            var type = value.GetType();
            var nameProp = type.GetProperty("Name") ?? type.GetProperty("WarehouseName");
            return nameProp?.GetValue(value)?.ToString() ?? "";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }

    public partial class PurchaseEntryView : Window
    {
        private readonly Registration _currentUser;
        private readonly PurchaseService _purchaseService = new();
        private readonly SaleService _saleService = new();
        private readonly SupplierService _supplierService = new();
        private readonly WarehouseService _warehouseService = new();
        private readonly LogService _logService = new();
        private readonly ObservableCollection<PurchaseCartLine> _lines = new();

        public PurchaseEntryView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            LineGrid.ItemsSource = _lines;

            DiscountInput.Text = "0";
            FreightInput.Text = "0";
            OtherChargesInput.Text = "0";

            RecomputeTotals();

            Loaded += async (s, e) => await LoadDropdowns();
        }

        private async System.Threading.Tasks.Task LoadDropdowns()
        {
            SupplierCombo.ItemsSource = await _supplierService.GetAllAsync();

            var warehouses = await _warehouseService.GetAllAsync();
            WarehouseCombo.ItemsSource = warehouses;

            // Default to Main Store specifically, not just "whatever sorts first" -
            // the list is alphabetical, so a warehouse named e.g. "jiji" would
            // otherwise get silently pre-selected ahead of "Main Store" purely
            // because 'j' comes before 'M', with nothing on screen calling
            // attention to it.
            var mainStoreIndex = warehouses.FindIndex(w =>
                w.WarehouseName.Trim().Equals(WarehouseService.DefaultWarehouseName, StringComparison.OrdinalIgnoreCase));

            if (mainStoreIndex >= 0)
                WarehouseCombo.SelectedIndex = mainStoreIndex;
            else if (WarehouseCombo.Items.Count > 0)
                WarehouseCombo.SelectedIndex = 0;
        }

        // ---------------- SEARCH ----------------
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

        private async void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsList.SelectedItem is Product p)
            {
                await AddLine(p);
                SearchBox.Clear();
                ResultsList.ItemsSource = null;
                SearchBox.Focus();
            }
        }

        // Enter key = scan/lookup path, same as checkout - exact code match, or a
        // single fuzzy match, adds immediately without needing to click a result.
        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var term = SearchBox.Text.Trim();
            if (term.Length == 0) return;

            var exact = await _saleService.FindByCodeAsync(term);
            if (exact != null)
            {
                await AddLine(exact);
                SearchBox.Clear();
                ResultsList.ItemsSource = null;
                SearchBox.Focus();
                return;
            }

            var results = await _saleService.SearchAsync(term);
            if (results.Count == 1)
            {
                await AddLine(results[0]);
                SearchBox.Clear();
                ResultsList.ItemsSource = null;
                SearchBox.Focus();
            }
            else
            {
                ResultsList.ItemsSource = results;
                ErrorText.Text = results.Count == 0 ? $"Nothing found for \"{term}\"." : "";
            }
        }

        private async System.Threading.Tasks.Task AddLine(Product p)
        {
            ErrorText.Text = "";

            var existing = _lines.FirstOrDefault(l => l.ProductId == p.PID);
            if (existing != null)
            {
                existing.Qty += 1;
            }
            else
            {
                // Cost Price is deliberately left blank, not pre-filled - goods can
                // come in cheaper (or dearer) than last time, and silently assuming
                // last time's price risks it being accepted unnoticed. LastKnownPrice
                // is shown as a separate read-only reference column instead, so you
                // have context to decide from without the field ever guessing for you.
                var lastCost = await _purchaseService.GetLatestBuyingPriceAsync(p.PID);

                _lines.Add(new PurchaseCartLine
                {
                    ProductId = p.PID,
                    ProductName = p.ProductName.Trim(),
                    ProductCode = p.ProductCode.Trim(),
                    Price = 0,
                    LastKnownPrice = lastCost,
                    Qty = 1
                });
            }

            LineGrid.Items.Refresh();
            RecomputeTotals();
        }

        private void RemoveLine_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is PurchaseCartLine line)
            {
                _lines.Remove(line);
                RecomputeTotals();
            }
        }

        // Shared by any TextBox that should only ever hold a plain decimal number -
        // blocks the keystroke entirely rather than validating after the fact, so
        // it's simply not possible to type a letter into a quantity or price field.
        private static readonly Regex NumericPattern = new(@"^[0-9]*\.?[0-9]*$");

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                e.Handled = true;
                return;
            }

            var proposedText = textBox.Text
                .Remove(textBox.SelectionStart, textBox.SelectionLength)
                .Insert(textBox.SelectionStart, e.Text);

            e.Handled = !NumericPattern.IsMatch(proposedText);
        }

        private void LineGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(CleanupAndRecompute), System.Windows.Threading.DispatcherPriority.Background);
        }

        // Qty lives in a template column now (for the +/- stepper), so it doesn't
        // go through CellEditEnding like the plain text columns do - this covers
        // the same "remove the line if qty drops to zero" behavior for it.
        private void QtyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CleanupAndRecompute();
        }

        private void QtyMinus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is PurchaseCartLine line)
            {
                line.Qty -= 1;
                CleanupAndRecompute();
            }
        }

        private void QtyPlus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is PurchaseCartLine line)
            {
                line.Qty += 1;
                RecomputeTotals();
            }
        }

        private void CleanupAndRecompute()
        {
            foreach (var line in _lines.ToList())
                if (line.Qty <= 0) _lines.Remove(line);

            RecomputeTotals();
        }

        // ---------------- TOTALS ----------------
        private decimal Subtotal => Math.Round(_lines.Sum(l => l.Amount), 2);

        private decimal DiscountPercent
            => decimal.TryParse(DiscountInput?.Text, out var d) && d >= 0 && d <= 100 ? d : 0;

        private decimal Freight
            => decimal.TryParse(FreightInput?.Text, out var f) && f >= 0 ? f : 0;

        private decimal OtherCharges
            => decimal.TryParse(OtherChargesInput?.Text, out var o) && o >= 0 ? o : 0;

        private decimal DiscountAmount => Math.Round(Subtotal * DiscountPercent / 100m, 2);

        private decimal GrandTotal => Math.Round(Subtotal - DiscountAmount + Freight + OtherCharges, 2);

        private void Totals_TextChanged(object sender, TextChangedEventArgs e) => RecomputeTotals();

        private void RecomputeTotals()
        {
            SubtotalText.Text = Subtotal.ToString("N2");
            DiscountAmountText.Text = DiscountAmount.ToString("N2");
            ChargesText.Text = (Freight + OtherCharges).ToString("N2");
            GrandTotalText.Text = GrandTotal.ToString("N2");
        }

        // ---------------- SAVE ----------------
        private async void SavePurchase_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (SupplierCombo.SelectedItem is not Supplier supplier)
            {
                ErrorText.Text = "Pick a supplier first.";
                return;
            }

            if (WarehouseCombo.SelectedItem is not Warehouse warehouse)
            {
                ErrorText.Text = "Pick a warehouse first.";
                return;
            }

            if (_lines.Count == 0)
            {
                ErrorText.Text = "Add at least one item.";
                return;
            }

            var unpriced = _lines.FirstOrDefault(l => l.Price <= 0);
            if (unpriced != null)
            {
                ErrorText.Text = $"Enter the Cost Price for '{unpriced.ProductName}' before saving.";
                return;
            }

            var purchaseLines = _lines.Select(l => new PurchaseLine
            {
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                ProductCode = l.ProductCode,
                Qty = l.Qty,
                Price = l.Price,
                ExpiryDate = string.IsNullOrWhiteSpace(l.ExpiryDate) ? null : l.ExpiryDate.Trim()
            }).ToList();

            SaveButton.IsEnabled = false;

            var result = await _purchaseService.SavePurchaseAsync(
                supplier.ID,
                warehouse.WarehouseName.Trim(),
                InvoiceInput.Text,
                purchaseLines,
                DiscountPercent,
                Freight,
                OtherCharges);

            SaveButton.IsEnabled = true;

            if (!result.Ok)
            {
                ErrorText.Text = result.Error;
                return;
            }

            await _logService.LogAsync(CurrentSession.UserId,
                $"Recorded Purchase {result.InvoiceNo} from '{(supplier.Name ?? "").Trim()}' - {AppSettings.CurrencySymbol} {result.GrandTotal:N2}");

            MessageBox.Show(
                $"Purchase {result.InvoiceNo} saved.\n\nTotal: {AppSettings.CurrencySymbol} {result.GrandTotal:N2}\n\nStock has been added to {warehouse.WarehouseName.Trim()}.",
                "Clidapos");

            ResetForm();
        }

        private void ResetForm()
        {
            _lines.Clear();
            InvoiceInput.Clear();
            DiscountInput.Text = "0";
            FreightInput.Text = "0";
            OtherChargesInput.Text = "0";
            SearchBox.Clear();
            ResultsList.ItemsSource = null;
            ErrorText.Text = "";
            RecomputeTotals();
        }

        // ---------------- CHROME ----------------
        private void NewEntry_Click(object sender, RoutedEventArgs e)
        {
            if (_lines.Count > 0)
            {
                var confirm = MessageBox.Show("There are unsaved items in this purchase. Discard and start a new entry?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;
            }

            ResetForm();
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            // Stock Levels is view-only - nothing here can be edited, on purpose.
            var stockLevels = new StockLevelsView(_currentUser, cameFromPurchaseEntry: true);
            stockLevels.Show();
            Close();
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            if (_lines.Count > 0)
            {
                var confirm = MessageBox.Show("There are unsaved items in this purchase. Leave anyway?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;
            }

            var history = new PurchaseHistoryListView(_currentUser);
            history.Show();
            Close();
        }

        private void BackToOffice_Click(object sender, RoutedEventArgs e)
        {
            if (_lines.Count > 0)
            {
                var confirm = MessageBox.Show("There are unsaved items in this purchase. Leave anyway?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;
            }

            var backOffice = new BackOfficeView(_currentUser);
            backOffice.Show();
            Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}