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
        private Product? _editingProduct;

        public ItemsView(Registration currentUser, Product? productToEdit = null)
        {
            InitializeComponent();
            _currentUser = currentUser;

            if (productToEdit != null)
            {
                Loaded += async (s, e) => await LoadForEditing(productToEdit);
            }
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
            QuantityInput.Text = "";
            ReorderInput.Text = "";
            SupplierInput.Text = "";
            ErrorText.Text = "";
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(NameInput.Text) || string.IsNullOrWhiteSpace(CodeInput.Text))
            {
                ErrorText.Text = "Product Code and Name are required.";
                return;
            }

            if (!decimal.TryParse(PriceInput.Text, out var price))
            {
                ErrorText.Text = "Price must be a valid number.";
                return;
            }

            decimal.TryParse(QuantityInput.Text, out var quantity);
            int.TryParse(ReorderInput.Text, out var reorderPoint);

            try
            {
                int productId;

                if (_editingProduct == null)
                {
                    productId = await _productService.GetNextIdAsync();
                    var newProduct = new Product
                    {
                        PID = productId,
                        ProductCode = CodeInput.Text.Trim(),
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
                    _editingProduct.ProductCode = CodeInput.Text.Trim();
                    _editingProduct.ProductName = NameInput.Text.Trim();
                    _editingProduct.Category = CategoryInput.Text.Trim();
                    _editingProduct.Unit = UnitInput.Text.Trim();
                    _editingProduct.Price = price;
                    _editingProduct.ReorderPoint = reorderPoint;
                    _editingProduct.P_Supplier = SupplierInput.Text.Trim();
                    await _productService.UpdateAsync(_editingProduct);
                }

                await _productService.SetQuantityAsync(productId, quantity);

                MessageBox.Show("Saved successfully.", "Clidapos");
                NewItem_Click(sender, e);
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Error: {ex.Message}";
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
                await _productService.DeleteAsync(_editingProduct.PID);
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