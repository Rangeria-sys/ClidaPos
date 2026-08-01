using System;
using System.Windows;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class UnitCategoryPopup : Window
    {
        private readonly UnitService _unitService = new();
        private string? _editing;

        public UnitCategoryPopup(string? editName = null)
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

            await _unitService.EnsureExistsAsync(name);
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
                await _unitService.RenameAsync(_editing, newName);
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

            var confirm = MessageBox.Show($"Remove unit '{_editing}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _unitService.RemoveAsync(_editing);
            _editing = null;
            NameInput.Text = "";
            ErrorText.Text = "Removed.";
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new UnitCategoryListView();
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}