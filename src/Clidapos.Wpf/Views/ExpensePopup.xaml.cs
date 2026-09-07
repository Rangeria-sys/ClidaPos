using System;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class ExpensePopup : Window
    {
        private readonly ExpenseService _expenseService = new();
        private readonly ExpenseTypeService _expenseTypeService = new();
        private readonly LogService _logService = new();
        private string? _editing;

        public ExpensePopup(Expense? editExpense = null)
        {
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                await LoadTypes();

                if (editExpense != null)
                {
                    _editing = editExpense.ExpenseName.Trim();
                    NameInput.Text = editExpense.ExpenseName.Trim();
                    TypeCombo.Text = editExpense.ExpenseType.Trim();
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
            var types = await _expenseTypeService.GetAllAsync();
            TypeCombo.ItemsSource = types;
        }

        /// <summary>
        /// Save only ever creates a brand-new expense. Once something has been
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
            _editing = null;
            NameInput.Text = "";
            TypeCombo.Text = "";
            ErrorText.Text = "";
            SetMode(isExisting: false);
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

            var existing = await _expenseService.GetAllAsync();
            if (existing.Any(x => x.ExpenseName.Trim().Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorText.Text = $"An expense named '{name}' already exists.";
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

                await LoadTypes();
                MessageBox.Show("Saved.", "Clidapos");
                New_Click(sender, e);
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (_editing == null)
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

            var existing = await _expenseService.GetAllAsync();
            var nameTaken = existing.Any(x =>
                !x.ExpenseName.Trim().Equals(_editing, StringComparison.OrdinalIgnoreCase) &&
                x.ExpenseName.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
            if (nameTaken)
            {
                ErrorText.Text = $"An expense named '{name}' already exists.";
                return;
            }

            var confirm = MessageBox.Show($"Update expense '{_editing}'?",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _expenseTypeService.EnsureExistsAsync(type);

                await _expenseService.UpdateAsync(_editing, new Expense
                {
                    ExpenseName = name,
                    ExpenseType = type
                });

                await _logService.LogAsync(CurrentSession.UserId, $"Updated Expense '{name}'");

                _editing = name;
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
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick an expense, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove expense '{_editing}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var deletedName = _editing;
            await _expenseService.RemoveAsync(_editing);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Expense '{deletedName}'");

            MessageBox.Show("Removed.", "Clidapos");
            New_Click(sender, e);
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

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                SystemSounds.Exclamation.Play();
            }
        }
    }
}
