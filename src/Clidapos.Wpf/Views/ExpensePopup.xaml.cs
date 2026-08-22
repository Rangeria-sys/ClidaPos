using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class ExpensePopup : Window
    {
        private readonly ExpenseService _expenseService = new();
        private readonly ExpenseTypeService _expenseTypeService = new();
        private readonly LogService _logService = new();
        private string? _editingOriginalName;

        public ExpensePopup(Expense? editExpense = null)
        {
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                await LoadTypes();

                if (editExpense != null)
                {
                    _editingOriginalName = editExpense.ExpenseName.Trim();
                    NameInput.Text = editExpense.ExpenseName.Trim();
                    TypeCombo.Text = editExpense.ExpenseType.Trim();
                }
            };
        }

        private async System.Threading.Tasks.Task LoadTypes()
        {
            var types = await _expenseTypeService.GetAllAsync();
            TypeCombo.ItemsSource = types;
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editingOriginalName = null;
            NameInput.Text = "";
            TypeCombo.Text = "";
            ErrorText.Text = "";
            NameInput.Focus();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Expense Name is required.";
                return;
            }

            var type = TypeCombo.Text.Trim();
            if (string.IsNullOrEmpty(type))
            {
                ErrorText.Text = "Expense Type is required.";
                return;
            }

            try
            {
                await _expenseTypeService.EnsureExistsAsync(type);

                await _expenseService.AddAsync(new Expense
                {
                    ExpenseName = name,
                    ExpenseType = type
                });

                await _logService.LogAsync(CurrentSession.UserId, $"Added Expense '{name}' (Type: {type})");

                _editingOriginalName = null;
                NameInput.Text = "";
                TypeCombo.Text = "";
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
            ErrorText.Text = "";

            if (_editingOriginalName == null)
            {
                ErrorText.Text = "Use Get Data, pick an expense, then edit and Update.";
                return;
            }

            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Expense Name is required.";
                return;
            }

            var type = TypeCombo.Text.Trim();
            if (string.IsNullOrEmpty(type))
            {
                ErrorText.Text = "Expense Type is required.";
                return;
            }

            try
            {
                await _expenseTypeService.EnsureExistsAsync(type);

                await _expenseService.UpdateAsync(_editingOriginalName, new Expense
                {
                    ExpenseName = name,
                    ExpenseType = type
                });

                await _logService.LogAsync(CurrentSession.UserId, $"Updated Expense '{name}'");

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
                ErrorText.Text = "Use Get Data, pick an expense, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove expense '{_editingOriginalName}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var deletedName = _editingOriginalName;
            await _expenseService.RemoveAsync(_editingOriginalName);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Expense '{deletedName}'");
            _editingOriginalName = null;
            NameInput.Text = "";
            TypeCombo.Text = "";
            ErrorText.Text = "Removed.";
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new ExpenseListView();
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}