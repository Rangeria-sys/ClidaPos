using System;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Input;
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
                SetMode(isExisting: true);
            }
            else
            {
                SetMode(isExisting: false);
            }
        }

        /// <summary>
        /// Save only ever creates a brand-new expense type. Once something has been
        /// loaded via Get Data (double-click), only Update can change it - this
        /// keeps "adding a new one" and "editing an existing one" from ever being
        /// confused with each other.
        /// </summary>
        private void SetMode(bool isExisting)
        {
            SaveBtn.IsEnabled = !isExisting;
            UpdateBtn.IsEnabled = isExisting;
            DeleteBtn.IsEnabled = isExisting;
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            NameInput.Text = "";
            ErrorText.Text = "";
            SetMode(isExisting: false);
            NameInput.Focus();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Type a expense type name first.";
                return;
            }

            var existing = await _expenseTypeService.GetAllAsync();
            if (existing.Any(c => c.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorText.Text = $"A expense type named '{name}' already exists.";
                return;
            }

            await _expenseTypeService.EnsureExistsAsync(name);
            await _logService.LogAsync(CurrentSession.UserId, $"Added Expense Category '{name}'");
            _editing = null;
            NameInput.Text = "";
            MessageBox.Show("Saved.", "Clidapos");
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_editing))
            {
                ErrorText.Text = "Use Get Data, pick a expense type, then edit and Update.";
                return;
            }

            var newName = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                ErrorText.Text = "The new name can't be empty.";
                return;
            }

            if (newName.Equals(_editing, StringComparison.OrdinalIgnoreCase))
            {
                ErrorText.Text = "Change the name before updating.";
                return;
            }

            var existing = await _expenseTypeService.GetAllAsync();
            if (existing.Any(c => c.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorText.Text = $"A expense type named '{newName}' already exists.";
                return;
            }

            var confirm = MessageBox.Show(
                $"Rename expense type '{_editing}' to '{newName}'?",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var oldName = _editing;
                await _expenseTypeService.RenameAsync(_editing, newName);
                await _logService.LogAsync(CurrentSession.UserId, $"Renamed Expense Category '{oldName}' to '{newName}'");
                _editing = newName;
                NameInput.Text = newName;
                MessageBox.Show("Updated.", "Clidapos");
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
                ErrorText.Text = "Use Get Data, pick a expense type, then Delete.";
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
            SetMode(isExisting: false);
            MessageBox.Show("Removed.", "Clidapos");
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

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                SystemSounds.Exclamation.Play();
            }
        }
    }
}
