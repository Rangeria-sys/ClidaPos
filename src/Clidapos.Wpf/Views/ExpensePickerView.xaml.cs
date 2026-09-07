using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class ExpensePickerView : Window
    {
        private readonly Registration _currentUser;
        private readonly ExpenseService _expenseService = new();
        private List<Expense> _all = new();

        public ExpensePickerView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _expenseService.GetAllAsync();
            ExpenseGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var term = SearchBox.Text.Trim();
            ExpenseGrid.ItemsSource = string.IsNullOrEmpty(term)
                ? _all
                : _all.Where(x =>
                    x.ExpenseName.Trim().Contains(term, System.StringComparison.OrdinalIgnoreCase) ||
                    x.ExpenseType.Trim().Contains(term, System.StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private void ExpenseGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ExpenseGrid.SelectedItem is not Expense expense) return;

            var paymentPopup = new BillPaymentPopup(_currentUser, expense);
            paymentPopup.Show();
            Close();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var expenseLedger = new ExpenseLogListView(_currentUser);
            expenseLedger.Show();
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
