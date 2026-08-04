using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class WarehousePopup : Window
    {
        private readonly WarehouseService _warehouseService = new();
        private readonly WarehouseTypeService _warehouseTypeService = new();
        private string? _editingOriginalName;

        public WarehousePopup(Warehouse? editWarehouse = null)
        {
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                await LoadTypes();

                if (editWarehouse != null)
                {
                    _editingOriginalName = editWarehouse.WarehouseName.Trim();
                    NameInput.Text = editWarehouse.WarehouseName.Trim();
                    AddressInput.Text = editWarehouse.Address?.Trim() ?? "";
                    CityInput.Text = editWarehouse.City?.Trim() ?? "";
                    TypeInput.Text = editWarehouse.WarehouseType?.Trim() ?? "";
                }
            };
        }

        private async System.Threading.Tasks.Task LoadTypes()
        {
            var types = await _warehouseTypeService.GetAllAsync();
            TypeInput.ItemsSource = types;
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editingOriginalName = null;
            NameInput.Text = "";
            AddressInput.Text = "";
            CityInput.Text = "";
            TypeInput.Text = "";
            ErrorText.Text = "";
            NameInput.Focus();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Warehouse Name is required.";
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(TypeInput.Text))
                {
                    await _warehouseTypeService.EnsureExistsAsync(TypeInput.Text.Trim());
                }

                var warehouse = new Warehouse
                {
                    WarehouseName = name,
                    Address = AddressInput.Text.Trim(),
                    City = CityInput.Text.Trim(),
                    WarehouseType = TypeInput.Text.Trim()
                };

                await _warehouseService.AddAsync(warehouse);
                _editingOriginalName = null;
                NameInput.Text = "";
                AddressInput.Text = "";
                CityInput.Text = "";
                TypeInput.Text = "";
                await LoadTypes();
                ErrorText.Text = "Saved.";
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (_editingOriginalName == null)
            {
                ErrorText.Text = "Use Get Data, pick a warehouse, then edit and Update.";
                return;
            }

            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Warehouse Name is required.";
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(TypeInput.Text))
                {
                    await _warehouseTypeService.EnsureExistsAsync(TypeInput.Text.Trim());
                }

                var warehouse = new Warehouse
                {
                    WarehouseName = name,
                    Address = AddressInput.Text.Trim(),
                    City = CityInput.Text.Trim(),
                    WarehouseType = TypeInput.Text.Trim()
                };

                await _warehouseService.UpdateAsync(_editingOriginalName, warehouse);
                _editingOriginalName = name;
                await LoadTypes();
                ErrorText.Text = "Updated.";
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_editingOriginalName == null)
            {
                ErrorText.Text = "Use Get Data, pick a warehouse, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove warehouse '{_editingOriginalName}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _warehouseService.RemoveAsync(_editingOriginalName);
            _editingOriginalName = null;
            NameInput.Text = "";
            AddressInput.Text = "";
            CityInput.Text = "";
            TypeInput.Text = "";
            ErrorText.Text = "Removed.";
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new WarehouseListView();
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}