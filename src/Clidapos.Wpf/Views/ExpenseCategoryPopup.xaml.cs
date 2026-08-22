using System;
using System.Windows;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class ExpenseCategoryPopup : Window
    {
        private readonly ExpenseTypeService _expenseTypeService = new();
        private readonly LogService _logService = new();
        private string? _editing;

        public ExpenseCategoryPopup(string? editName = null)
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(editName))
            {
                _editing = editName;
                NameInput.Text = editName;
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
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
            await _logService.LogAsync(CurrentSession.UserId, $"Added Expense Category '{name}'");
            _editing = null;
            NameInput.Text = "";
            ErrorText.Text = "Saved.";
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_editing))
            {
                ErrorText.Text = "Use Get Data, pick a category, then edit and Update.";
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
                var oldName = _editing;
                await _expenseTypeService.RenameAsync(_editing, newName);
                await _logService.LogAsync(CurrentSession.UserId, $"Renamed Expense Category '{oldName}' to '{newName}'");
                _editing = null;
                ErrorText.Text = "Updated.";
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
                ErrorText.Text = "Use Get Data, pick a category, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove expense type '{_editing}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var deletedName = _editing;
            await _expenseTypeService.RemoveAsync(_editing);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Expense Category '{deletedName}'");
            _editing = null;
            NameInput.Text = "";
            ErrorText.Text = "Removed.";
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new ExpenseCategoryListView();
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}