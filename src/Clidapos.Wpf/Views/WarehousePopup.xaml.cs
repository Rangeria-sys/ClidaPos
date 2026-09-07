using System;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class WarehousePopup : Window
    {
        private readonly WarehouseService _warehouseService = new();
        private readonly WarehouseTypeService _warehouseTypeService = new();
        private readonly LogService _logService = new();
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
                    SetMode(isExisting: true);
                }
                else
                {
                    SetMode(isExisting: false);
                }
            };
        }

        private async System.Threading.Tasks.Task LoadTypes()
        {
            var types = await _warehouseTypeService.GetAllAsync();
            TypeInput.ItemsSource = types;
        }

        /// <summary>
        /// Save only ever creates a brand-new warehouse. Once something has been
        /// loaded via Get Data (double-click), only Update can change it.
        /// </summary>
        private void SetMode(bool isExisting)
        {
            SaveBtn.IsEnabled = !isExisting;
            UpdateBtn.IsEnabled = isExisting;
            DeleteBtn.IsEnabled = isExisting;
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editingOriginalName = null;
            NameInput.Text = "";
            AddressInput.Text = "";
            CityInput.Text = "";
            TypeInput.Text = "";
            ErrorText.Text = "";
            SetMode(isExisting: false);
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

            var existing = await _warehouseService.GetAllAsync();
            if (existing.Any(w => w.WarehouseName.Trim().Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorText.Text = $"A warehouse named '{name}' already exists.";
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
                await _logService.LogAsync(CurrentSession.UserId, $"Added Warehouse '{name}'");

                _editingOriginalName = null;
                NameInput.Text = "";
                AddressInput.Text = "";
                CityInput.Text = "";
                TypeInput.Text = "";
                await LoadTypes();

                MessageBox.Show("Saved.", "Clidapos");
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

            var existing = await _warehouseService.GetAllAsync();
            var nameTaken = existing.Any(w =>
                !w.WarehouseName.Trim().Equals(_editingOriginalName, StringComparison.OrdinalIgnoreCase) &&
                w.WarehouseName.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
            if (nameTaken)
            {
                ErrorText.Text = $"A warehouse named '{name}' already exists.";
                return;
            }

            var confirm = MessageBox.Show($"Update warehouse '{_editingOriginalName}'?",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

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
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Warehouse '{name}'");

                _editingOriginalName = name;
                await LoadTypes();

                MessageBox.Show("Updated.", "Clidapos");
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

            var deletedName = _editingOriginalName;

            try
            {
                await _warehouseService.RemoveAsync(_editingOriginalName);
                await _logService.LogAsync(CurrentSession.UserId, $"Deleted Warehouse '{deletedName}'");

                _editingOriginalName = null;
                NameInput.Text = "";
                AddressInput.Text = "";
                CityInput.Text = "";
                TypeInput.Text = "";
                SetMode(isExisting: false);

                MessageBox.Show("Removed.", "Clidapos");
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
            }
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

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                SystemSounds.Exclamation.Play();
            }
        }
    }
}
