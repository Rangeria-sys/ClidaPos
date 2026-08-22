using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class ExpenseListView : Window
    {
        private readonly ExpenseService _expenseService = new();
        private List<Expense> _all = new();

        public ExpenseListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _expenseService.GetAllAsync();
            ExpenseGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            ExpenseGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(x => x.ExpenseName.Trim().ToLower().Contains(q)
                               || x.ExpenseType.Trim().ToLower().Contains(q)).ToList();
        }

        private void ExpenseGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ExpenseGrid.SelectedItem is Expense expense)
            {
                var popup = new ExpensePopup(expense);
                popup.Show();
                Close();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
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