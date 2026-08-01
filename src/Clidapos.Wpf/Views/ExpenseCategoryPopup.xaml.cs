using System;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class ExpenseCategoryPopup : Window
    {
        private readonly ExpenseTypeService _expenseTypeService = new();
        private string? _editing;

        public ExpenseCategoryPopup()
        {
            InitializeComponent();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            ItemList.SelectedItem = null;
            NameInput.Text = "";
            ErrorText.Text = "";
            NameInput.Focus();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Type a category name first.";
                return;
            }

            await _expenseTypeService.EnsureExistsAsync(name);
            _editing = null;
            NameInput.Text = "";
            ErrorText.Text = "";
            ItemList.ItemsSource = await _expenseTypeService.GetAllAsync();
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_editing))
            {
                ErrorText.Text = "Select a category from the list first, then edit and Update.";
                return;
            }

            var newName = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                ErrorText.Text = "The new name can't be empty.";
                return;
            }

            try
            {
                await _expenseTypeService.RenameAsync(_editing, newName);
                _editing = null;
                NameInput.Text = "";
                ErrorText.Text = "";
                ItemList.ItemsSource = await _expenseTypeService.GetAllAsync();
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_editing))
            {
                ErrorText.Text = "Select a category from the list first, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove expense type '{_editing}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _expenseTypeService.RemoveAsync(_editing);
            _editing = null;
            NameInput.Text = "";
            ErrorText.Text = "";
            ItemList.ItemsSource = await _expenseTypeService.GetAllAsync();
        }

        private async void GetData_Click(object sender, RoutedEventArgs e)
        {
            ItemList.ItemsSource = await _expenseTypeService.GetAllAsync();
        }

        private void ItemList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemList.SelectedItem is string name)
            {
                _editing = name;
                NameInput.Text = name;
                ErrorText.Text = "";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}