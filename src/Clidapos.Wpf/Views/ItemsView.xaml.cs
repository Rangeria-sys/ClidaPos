using System;
using System.Linq;
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
                else
                {
                    SetMode(isExisting: false);
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

            SetMode(isExisting: true);
        }

        /// <summary>
        /// Save only ever creates a brand-new item. Once something has been
        /// loaded via Get Data (double-click), only Update can change it - this
        /// keeps "adding a new one" and "editing an existing one" from ever being
        /// confused with each other.
        /// </summary>
        private void SetMode(bool isExisting)
        {
            SaveBtn.IsEnabled = !isExisting;
            UpdateBtn.IsEnabled = isExisting;
            DeleteBtn.IsEnabled = isExisting;

            // Quantity can only be set here when creating a brand-new item (the
            // starting stock count). Once editing an existing item, this field is
            // locked - changing stock afterward has to go through Purchase Entry
            // (adds correctly, tracks the supplier) or Stock Adjustment (corrections,
            // write-offs), never by overwriting the number here.
            QuantityInput.IsEnabled = !isExisting;
            QuantityLabel.Text = isExisting
                ? "Quantity (use Purchase Entry or Stock Adjustment to change stock)"
                : "Quantity";
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
            SetMode(isExisting: false);
        }

        /// <summary>Shared field validation for both Save and Update - returns the parsed values, or null (with ErrorText already set) if something's invalid.</summary>
        private (decimal price, decimal buyingPrice, int reorderPoint, decimal quantity)? ValidateFields()
        {
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                ErrorText.Text = "Product Name is required.";
                return null;
            }
            if (string.IsNullOrWhiteSpace(CategoryInput.Text))
            {
                ErrorText.Text = "Category is required.";
                return null;
            }
            if (string.IsNullOrWhiteSpace(UnitInput.Text))
            {
                ErrorText.Text = "Unit is required.";
                return null;
            }
            if (!decimal.TryParse(PriceInput.Text, out var price))
            {
                ErrorText.Text = "Selling Price must be a valid number.";
                return null;
            }
            if (!decimal.TryParse(BuyingPriceInput.Text, out var buyingPrice))
            {
                ErrorText.Text = "Buying Price must be a valid number.";
                return null;
            }
            if (price <= buyingPrice)
            {
                ErrorText.Text = "Selling Price must be higher than Buying Price.";
                return null;
            }
            if (!int.TryParse(ReorderInput.Text, out var reorderPoint))
            {
                ErrorText.Text = "Reorder Point must be a valid number.";
                return null;
            }

            decimal.TryParse(QuantityInput.Text, out var quantity);
            return (price, buyingPrice, reorderPoint, quantity);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var name = NameInput.Text.Trim();
            var fields = ValidateFields();
            if (fields == null) return;
            var (price, buyingPrice, reorderPoint, quantity) = fields.Value;

            var allProducts = await _productService.GetAllAsync();
            if (allProducts.Any(p => p.ProductName.Trim().Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorText.Text = $"An item named '{name}' already exists. Use Get Data to find and edit it instead.";
                return;
            }

            try
            {
                await _unitService.EnsureExistsAsync(UnitInput.Text.Trim());
                await _categoryService.EnsureExistsAsync(CategoryInput.Text.Trim());

                var productId = await _productService.GetNextIdAsync();
                var code = string.IsNullOrWhiteSpace(CodeInput.Text)
                    ? $"ITM-{productId}"
                    : CodeInput.Text.Trim();

                var newProduct = new Product
                {
                    PID = productId,
                    ProductCode = code,
                    ProductName = name,
                    Category = CategoryInput.Text.Trim(),
                    Unit = UnitInput.Text.Trim(),
                    Price = price,
                    ReorderPoint = reorderPoint,
                    P_Supplier = SupplierInput.Text.Trim()
                };
                await _productService.AddAsync(newProduct);
                await _productService.SetQuantityAsync(productId, quantity);

                if (buyingPrice > 0)
                {
                    await _purchaseService.RecordBuyingPriceAsync(productId, quantity, buyingPrice);
                }

                await LoadUnits();
                await LoadCategories();
                await _logService.LogAsync(CurrentSession.UserId, $"Added Item '{name}'");

                MessageBox.Show("Saved.", "Clidapos");
                NewItem_Click(sender, e);
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                ErrorText.Text = $"Error: {detail}";
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (_editingProduct == null)
            {
                ErrorText.Text = "Use Get Data, pick an item, then edit and Update.";
                return;
            }

            var name = NameInput.Text.Trim();
            var fields = ValidateFields();
            if (fields == null) return;
            var (price, buyingPrice, reorderPoint, quantity) = fields.Value;

            var allProducts = await _productService.GetAllAsync();
            var nameTaken = allProducts.Any(p =>
                p.PID != _editingProduct.PID &&
                p.ProductName.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
            if (nameTaken)
            {
                ErrorText.Text = $"Another item is already named '{name}'.";
                return;
            }

            var confirm = MessageBox.Show(
                $"Update '{_editingProduct.ProductName.Trim()}'?",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _unitService.EnsureExistsAsync(UnitInput.Text.Trim());
                await _categoryService.EnsureExistsAsync(CategoryInput.Text.Trim());

                var productId = _editingProduct.PID;
                var code = string.IsNullOrWhiteSpace(CodeInput.Text)
                    ? _editingProduct.ProductCode
                    : CodeInput.Text.Trim();

                _editingProduct.ProductCode = code;
                _editingProduct.ProductName = name;
                _editingProduct.Category = CategoryInput.Text.Trim();
                _editingProduct.Unit = UnitInput.Text.Trim();
                _editingProduct.Price = price;
                _editingProduct.ReorderPoint = reorderPoint;
                _editingProduct.P_Supplier = SupplierInput.Text.Trim();
                await _productService.UpdateAsync(_editingProduct);
                await _productService.SetQuantityAsync(productId, quantity);

                if (buyingPrice > 0)
                {
                    await _purchaseService.RecordBuyingPriceAsync(productId, quantity, buyingPrice);
                }

                await LoadUnits();
                await LoadCategories();
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Item '{name}'");

                MessageBox.Show("Updated.", "Clidapos");
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
                MessageBox.Show("Removed.", "Clidapos");
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
