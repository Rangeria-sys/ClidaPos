using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class CategoryListView : Window
    {
        private readonly Registration _currentUser;
        private readonly CategoryService _categoryService = new();
        private List<string> _all = new();

        public CategoryListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _categoryService.GetAllAsync();
            CategoryGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            CategoryGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(c => c.ToLower().Contains(q)).ToList();
        }

        private void CategoryGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CategoryGrid.SelectedItem is string name)
            {
                new CategoryUnitView(_currentUser, editCategory: name).Show();
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            new CategoryUnitView(_currentUser).Show();
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}