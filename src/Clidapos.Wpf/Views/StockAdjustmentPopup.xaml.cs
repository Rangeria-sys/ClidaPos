using System;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class StockAdjustmentPopup : Window
    {
        private readonly StockAdjustmentService _stockAdjustmentService = new();
        private readonly SaleService _saleService = new();
        private readonly LogService _logService = new();
        private int? _selectedProductId;
        private string? _selectedProductName;
        private decimal _currentQty;

        public StockAdjustmentPopup()
        {
            InitializeComponent();
        }

        /// <summary>Opens with a product already picked - used when arriving from Get Data.</summary>
        public StockAdjustmentPopup(int productId, string productName) : this()
        {
            _selectedProductId = productId;
            _selectedProductName = productName;
            SelectedProductText.Text = $"Selected: {productName}";

            Loaded += async (s, e) => await LoadCurrentQty();
        }

        private async System.Threading.Tasks.Task LoadCurrentQty()
        {
            if (_selectedProductId == null)
            {
                _currentQty = 0;
                CurrentQtyText.Text = "—";
                RecomputeFinal();
                return;
            }

            _currentQty = await _stockAdjustmentService.GetCurrentQtyAsync(
                _selectedProductId.Value, WarehouseService.DefaultWarehouseName);

            CurrentQtyText.Text = _currentQty.ToString("N2");
            RecomputeFinal();
        }

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
                _selectedProductId = p.PID;
                _selectedProductName = p.ProductName.Trim();
                SelectedProductText.Text = $"Selected: {_selectedProductName}";
                SearchBox.Clear();
                ResultsList.ItemsSource = null;

                await LoadCurrentQty();
            }
        }

        private void Recompute_Changed(object sender, RoutedEventArgs e) => RecomputeFinal();

        private void RecomputeFinal()
        {
            if (FinalQtyText == null) return;

            var typeItem = TypeCombo.SelectedItem as ComboBoxItem;
            var hasQty = decimal.TryParse(QtyInput?.Text, out var qty);

            if (typeItem == null || !hasQty)
            {
                FinalQtyText.Text = "—";
                return;
            }

            var adjustmentType = typeItem.Content?.ToString() ?? "";
            var final = adjustmentType == "Increase" ? _currentQty + qty : _currentQty - qty;
            FinalQtyText.Text = final.ToString("N2");
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (_selectedProductId == null)
            {
                ErrorText.Text = "Search and select a product first, or use Get Data.";
                return;
            }

            if (TypeCombo.SelectedItem is not ComboBoxItem typeItem)
            {
                ErrorText.Text = "Pick Increase or Decrease.";
                return;
            }

            if (!decimal.TryParse(QtyInput.Text, out var qty) || qty <= 0)
            {
                ErrorText.Text = "Enter a quantity greater than zero.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ReasonInput.Text))
            {
                ErrorText.Text = "A reason is required - you can't save without one.";
                return;
            }

            var adjustmentType = typeItem.Content?.ToString() ?? "";

            var result = await _stockAdjustmentService.SaveAdjustmentAsync(
                _selectedProductId.Value, WarehouseService.DefaultWarehouseName, adjustmentType, qty, ReasonInput.Text);

            if (!result.Ok)
            {
                ErrorText.Text = result.Error;
                return;
            }

            await _logService.LogAsync(CurrentSession.UserId,
                $"Stock Adjustment: {adjustmentType} {qty:N2} on '{_selectedProductName}' - Reason: {ReasonInput.Text.Trim()}");

            MessageBox.Show(
                $"Stock adjustment saved. {adjustmentType} of {qty:N2} applied to {_selectedProductName}.",
                "Clidapos");

            _selectedProductId = null;
            _selectedProductName = null;
            _currentQty = 0;
            SelectedProductText.Text = "";
            CurrentQtyText.Text = "—";
            FinalQtyText.Text = "—";
            QtyInput.Clear();
            ReasonInput.Clear();
            ErrorText.Text = "";
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new StockAdjustmentListView();
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}