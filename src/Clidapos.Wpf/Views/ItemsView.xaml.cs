using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class ItemsView : Window
    {
        private readonly Registration _currentUser;
        private readonly ProductService _productService = new();
        private readonly PurchaseService _purchaseService = new();
        private readonly UnitService _unitService = new();
        private readonly CategoryService _categoryService = new();
        private readonly LogService _logService = new();
        private Product? _editingProduct;

        public ItemsView(Registration currentUser, Product? productToEdit = null)
        {
            InitializeComponent();
            _currentUser = currentUser;

            Loaded += async (s, e) =>
            {
                await LoadUnits();
                await LoadCategories();
                if (productToEdit != null)
                {
                    await LoadForEditing(productToEdit);
                }
            };
        }

        private async System.Threading.Tasks.Task LoadUnits()
        {
            var units = await _unitService.GetAllAsync();
            UnitInput.ItemsSource = units;
        }

        private async System.Threading.Tasks.Task LoadCategories()
        {
            var categories = await _categoryService.GetAllAsync();
            CategoryInput.ItemsSource = categories;
        }

        private async System.Threading.Tasks.Task LoadForEditing(Product product)
        {
            _editingProduct = product;
            FormTitle.Text = $"Editing: {product.ProductName.Trim()}";
            CodeInput.Text = product.ProductCode.Trim();
            NameInput.Text = product.ProductName.Trim();
            CategoryInput.Text = product.Category?.Trim() ?? "";
            UnitInput.Text = product.Unit?.Trim() ?? "";
            PriceInput.Text = product.Price.ToString("0.00");
            ReorderInput.Text = product.ReorderPoint.ToString();
            SupplierInput.Text = product.P_Supplier?.Trim() ?? "";

            var qty = await _productService.GetQuantityAsync(product.PID);
            QuantityInput.Text = qty.ToString("0.##");

            var latestBuyingPrice = await _purchaseService.GetLatestBuyingPriceAsync(product.PID);
            BuyingPriceInput.Text = latestBuyingPrice?.ToString("0.00") ?? "";
        }

        private void NewItem_Click(object sender, RoutedEventArgs e)
        {
            _editingProduct = null;
            FormTitle.Text = "New Item";
            CodeInput.Text = "";
            NameInput.Text = "";
            CategoryInput.Text = "";
            UnitInput.Text = "";
            PriceInput.Text = "";
            BuyingPriceInput.Text = "";
            QuantityInput.Text = "";
            ReorderInput.Text = "";
            SupplierInput.Text = "";
            ErrorText.Text = "";
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                ErrorText.Text = "Product Name is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(CategoryInput.Text))
            {
                ErrorText.Text = "Category is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(UnitInput.Text))
            {
                ErrorText.Text = "Unit is required.";
                return;
            }

            if (!decimal.TryParse(PriceInput.Text, out var price))
            {
                ErrorText.Text = "Selling Price must be a valid number.";
                return;
            }
            if (!decimal.TryParse(BuyingPriceInput.Text, out var buyingPrice))
            {
                ErrorText.Text = "Buying Price must be a valid number.";
                return;
            }

            if (price <= buyingPrice)
            {
                ErrorText.Text = "Selling Price must be higher than Buying Price.";
                return;
            }

            if (!int.TryParse(ReorderInput.Text, out var reorderPoint))
            {
                ErrorText.Text = "Reorder Point must be a valid number.";
                return;
            }

            decimal.TryParse(QuantityInput.Text, out var quantity);

            try
            {
                await _unitService.EnsureExistsAsync(UnitInput.Text.Trim());
                await _categoryService.EnsureExistsAsync(CategoryInput.Text.Trim());

                int productId;
                var wasNew = _editingProduct == null;

                if (_editingProduct == null)
                {
                    productId = await _productService.GetNextIdAsync();

                    var code = string.IsNullOrWhiteSpace(CodeInput.Text)
                        ? $"ITM-{productId}"
                        : CodeInput.Text.Trim();

                    var newProduct = new Product
                    {
                        PID = productId,
                        ProductCode = code,
                        ProductName = NameInput.Text.Trim(),
                        Category = CategoryInput.Text.Trim(),
                        Unit = UnitInput.Text.Trim(),
                        Price = price,
                        ReorderPoint = reorderPoint,
                        P_Supplier = SupplierInput.Text.Trim()
                    };
                    await _productService.AddAsync(newProduct);
                }
                else
                {
                    productId = _editingProduct.PID;

                    var code = string.IsNullOrWhiteSpace(CodeInput.Text)
                        ? _editingProduct.ProductCode
                        : CodeInput.Text.Trim();

                    _editingProduct.ProductCode = code;
                    _editingProduct.ProductName = NameInput.Text.Trim();
                    _editingProduct.Category = CategoryInput.Text.Trim();
                    _editingProduct.Unit = UnitInput.Text.Trim();
                    _editingProduct.Price = price;
                    _editingProduct.ReorderPoint = reorderPoint;
                    _editingProduct.P_Supplier = SupplierInput.Text.Trim();
                    await _productService.UpdateAsync(_editingProduct);
                }

                await _productService.SetQuantityAsync(productId, quantity);

                if (buyingPrice > 0)
                {
                    await _purchaseService.RecordBuyingPriceAsync(productId, quantity, buyingPrice);
                }

                await LoadUnits();
                await LoadCategories();

                await _logService.LogAsync(CurrentSession.UserId,
                    (wasNew ? "Added Item '" : "Updated Item '") + NameInput.Text.Trim() + "'");

                MessageBox.Show("Saved successfully.", "Clidapos");
                NewItem_Click(sender, e);
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                ErrorText.Text = $"Error: {detail}";
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_editingProduct == null)
            {
                ErrorText.Text = "No item selected. Use Get Data to find and open an item first.";
                return;
            }

            var confirm = MessageBox.Show($"Delete '{_editingProduct.ProductName.Trim()}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                var deletedName = _editingProduct.ProductName.Trim();
                await _productService.DeleteAsync(_editingProduct.PID);
                await _logService.LogAsync(CurrentSession.UserId, $"Deleted Item '{deletedName}'");
                MessageBox.Show("Deleted.", "Clidapos");
                NewItem_Click(sender, e);
            }
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new ProductListView(_currentUser);
            listView.Show();
            Close();
        }

        private void BackToOffice_Click(object sender, RoutedEventArgs e)
        {
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