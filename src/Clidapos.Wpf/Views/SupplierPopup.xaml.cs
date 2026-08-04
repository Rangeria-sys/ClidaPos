using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SupplierPopup : Window
    {
        private readonly SupplierService _supplierService = new();
        private Supplier? _editing;

        public SupplierPopup(Supplier? editSupplier = null)
        {
            InitializeComponent();

            if (editSupplier != null)
            {
                _editing = editSupplier;
                CodeInput.Text = editSupplier.SupplierID.Trim();
                NameInput.Text = editSupplier.Name.Trim();
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            CodeInput.Text = "";
            NameInput.Text = "";
            ErrorText.Text = "";
            NameInput.Focus();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Supplier Name is required.";
                return;
            }

            var newId = await _supplierService.GetNextIdAsync();
            var code = string.IsNullOrWhiteSpace(CodeInput.Text)
                ? $"SUPP-{newId}"
                : CodeInput.Text.Trim();

            var supplier = new Supplier
            {
                ID = newId,
                SupplierID = code,
                Name = name
            };

            await _supplierService.AddAsync(supplier);
            _editing = null;
            CodeInput.Text = "";
            NameInput.Text = "";
            ErrorText.Text = "Saved.";
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a supplier, then edit and Update.";
                return;
            }

            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Supplier Name is required.";
                return;
            }

            try
            {
                _editing.SupplierID = string.IsNullOrWhiteSpace(CodeInput.Text)
                    ? _editing.SupplierID
                    : CodeInput.Text.Trim();
                _editing.Name = name;

                await _supplierService.UpdateAsync(_editing);
                ErrorText.Text = "Updated.";
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a supplier, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove supplier '{_editing.Name.Trim()}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _supplierService.DeleteAsync(_editing.ID);
            _editing = null;
            CodeInput.Text = "";
            NameInput.Text = "";
            ErrorText.Text = "Removed.";
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new SupplierListView();
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}