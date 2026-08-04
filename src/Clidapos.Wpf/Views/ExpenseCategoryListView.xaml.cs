using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class ExpenseCategoryListView : Window
    {
        private readonly ExpenseTypeService _expenseTypeService = new();
        private List<string> _all = new();

        public ExpenseCategoryListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _expenseTypeService.GetAllAsync();
            ExpenseGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            ExpenseGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(c => c.ToLower().Contains(q)).ToList();
        }

        private void ExpenseGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ExpenseGrid.SelectedItem is string name)
            {
                var popup = new ExpenseCategoryPopup(name);
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