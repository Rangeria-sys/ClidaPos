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
        private readonly WarehouseService _warehouseService = new();
        private int? _selectedProductId;

        public StockAdjustmentPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadWarehouses();
        }

        private async System.Threading.Tasks.Task LoadWarehouses()
        {
            WarehouseCombo.ItemsSource = await _warehouseService.GetAllAsync();
            if (WarehouseCombo.Items.Count > 0)
                WarehouseCombo.SelectedIndex = 0;
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

        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsList.SelectedItem is Product p)
            {
                _selectedProductId = p.PID;
                SelectedProductText.Text = $"Selected: {p.ProductName.Trim()}";
                SearchBox.Clear();
                ResultsList.ItemsSource = null;
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (_selectedProductId == null)
            {
                ErrorText.Text = "Search for and select a product first.";
                return;
            }

            if (WarehouseCombo.SelectedItem is not Warehouse warehouse)
            {
                ErrorText.Text = "Pick a warehouse.";
                return;
            }

            if (TypeCombo.SelectedItem is not ComboBoxItem typeItem)
            {
                ErrorText.Text = "Pick Increase or Decrease.";
                return;
            }

            if (!decimal.TryParse(QtyInput.Text, out var qty))
            {
                ErrorText.Text = "Quantity must be a number.";
                return;
            }

            var error = await _stockAdjustmentService.SaveAdjustmentAsync(
                _selectedProductId.Value,
                warehouse.WarehouseName.Trim(),
                typeItem.Content.ToString() ?? "",
                qty,
                ReasonInput.Text);

            if (!string.IsNullOrEmpty(error))
            {
                ErrorText.Text = error;
                return;
            }

            _selectedProductId = null;
            SelectedProductText.Text = "";
            QtyInput.Clear();
            ReasonInput.Clear();
            TypeCombo.SelectedItem = null;
            ErrorText.Text = "Saved.";
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