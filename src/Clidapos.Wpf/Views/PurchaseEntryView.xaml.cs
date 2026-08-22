using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
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
            WarehouseCombo.ItemsSource = await _warehouseService.GetAllAsync();

            if (WarehouseCombo.Items.Count > 0)
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

        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsList.SelectedItem is Product p)
            {
                AddLine(p);
                SearchBox.Clear();
                ResultsList.ItemsSource = null;
                SearchBox.Focus();
            }
        }

        private void AddLine(Product p)
        {
            ErrorText.Text = "";

            var existing = _lines.FirstOrDefault(l => l.ProductId == p.PID);
            if (existing != null)
            {
                existing.Qty += 1;
            }
            else
            {
                _lines.Add(new PurchaseCartLine
                {
                    ProductId = p.PID,
                    ProductName = p.ProductName.Trim(),
                    ProductCode = p.ProductCode.Trim(),
                    Price = p.Price,
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

        private void LineGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var line in _lines.ToList())
                    if (line.Qty <= 0) _lines.Remove(line);

                LineGrid.Items.Refresh();
                RecomputeTotals();
            }), System.Windows.Threading.DispatcherPriority.Background);
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

            var purchaseLines = _lines.Select(l => new PurchaseLine
            {
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                ProductCode = l.ProductCode,
                Qty = l.Qty,
                Price = l.Price
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
                $"Recorded Purchase {result.InvoiceNo} from '{supplier.Name.Trim()}' - {AppSettings.CurrencySymbol} {result.GrandTotal:N2}");

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