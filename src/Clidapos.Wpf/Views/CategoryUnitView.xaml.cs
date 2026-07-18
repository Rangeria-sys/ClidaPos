using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class CategoryUnitView : Window
    {
        private readonly Registration _currentUser;
        private readonly CategoryService _categoryService = new();
        private readonly UnitService _unitService = new();

        private string? _editingCategory;
        private string? _editingUnit;

        public CategoryUnitView(Registration currentUser, string? editCategory = null, string? editUnit = null)
        {
            InitializeComponent();
            _currentUser = currentUser;

            if (!string.IsNullOrEmpty(editCategory))
            {
                _editingCategory = editCategory;
                NewCategoryInput.Text = editCategory;
            }

            if (!string.IsNullOrEmpty(editUnit))
            {
                _editingUnit = editUnit;
                NewUnitInput.Text = editUnit;
            }
        }

        // ---------------- CATEGORIES ----------------
        private void GetCategories_Click(object sender, RoutedEventArgs e)
        {
            new CategoryListView(_currentUser).Show();
            Close();
        }

        private void NewCategory_Click(object sender, RoutedEventArgs e)
        {
            _editingCategory = null;
            NewCategoryInput.Text = "";
            NewCategoryInput.Focus();
        }

        private async void SaveCategory_Click(object sender, RoutedEventArgs e)
        {
            var name = NewCategoryInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Type a category name first.", "Clidapos");
                return;
            }

            await _categoryService.EnsureExistsAsync(name);
            _editingCategory = null;
            NewCategoryInput.Text = "";
            MessageBox.Show($"Category \"{name}\" saved. Use Get Data to view the list.", "Clidapos");
        }

        private async void UpdateCategory_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_editingCategory))
            {
                MessageBox.Show("Use Get Data, pick a category, then edit and Update.", "Clidapos");
                return;
            }

            var newName = NewCategoryInput.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                MessageBox.Show("The new category name can't be empty.", "Clidapos");
                return;
            }

            try
            {
                await _categoryService.RenameAsync(_editingCategory, newName);
                _editingCategory = null;
                NewCategoryInput.Text = "";
                MessageBox.Show("Category updated.", "Clidapos");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Clidapos");
            }
        }

        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_editingCategory))
            {
                MessageBox.Show("Use Get Data, pick a category, then Delete.", "Clidapos");
                return;
            }

            var confirm = MessageBox.Show(
                $"Remove category '{_editingCategory}'? Items already using it keep the name.",
                "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _categoryService.RemoveAsync(_editingCategory);
            _editingCategory = null;
            NewCategoryInput.Text = "";
            MessageBox.Show("Category removed.", "Clidapos");
        }

        // ---------------- UNITS ----------------
        private void GetUnits_Click(object sender, RoutedEventArgs e)
        {
            new UnitListView(_currentUser).Show();
            Close();
        }

        private void NewUnit_Click(object sender, RoutedEventArgs e)
        {
            _editingUnit = null;
            NewUnitInput.Text = "";
            NewUnitInput.Focus();
        }

        private async void SaveUnit_Click(object sender, RoutedEventArgs e)
        {
            var name = NewUnitInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Type a unit name first.", "Clidapos");
                return;
            }

            await _unitService.EnsureExistsAsync(name);
            _editingUnit = null;
            NewUnitInput.Text = "";
            MessageBox.Show($"Unit \"{name}\" saved. Use Get Data to view the list.", "Clidapos");
        }

        private async void UpdateUnit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_editingUnit))
            {
                MessageBox.Show("Use Get Data, pick a unit, then edit and Update.", "Clidapos");
                return;
            }

            var newName = NewUnitInput.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                MessageBox.Show("The new unit name can't be empty.", "Clidapos");
                return;
            }

            try
            {
                await _unitService.RenameAsync(_editingUnit, newName);
                _editingUnit = null;
                NewUnitInput.Text = "";
                MessageBox.Show("Unit updated.", "Clidapos");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Clidapos");
            }
        }

        private async void DeleteUnit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_editingUnit))
            {
                MessageBox.Show("Use Get Data, pick a unit, then Delete.", "Clidapos");
                return;
            }

            var confirm = MessageBox.Show(
                $"Remove unit '{_editingUnit}'? Items already using it keep the value.",
                "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _unitService.RemoveAsync(_editingUnit);
            _editingUnit = null;
            NewUnitInput.Text = "";
            MessageBox.Show("Unit removed.", "Clidapos");
        }

        // ---------------- window chrome ----------------
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