using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class CategoryUnitView : Window
    {
        private readonly Registration _currentUser;
        private readonly CategoryService _categoryService = new();
        private readonly UnitService _unitService = new();

        public CategoryUnitView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await RefreshLists();
        }

        private async System.Threading.Tasks.Task RefreshLists()
        {
            CategoryList.ItemsSource = await _categoryService.GetAllAsync();
            UnitList.ItemsSource = await _unitService.GetAllAsync();
        }

        private async void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NewCategoryInput.Text))
            {
                await _categoryService.EnsureExistsAsync(NewCategoryInput.Text.Trim());
                NewCategoryInput.Text = "";
                await RefreshLists();
            }
        }

        private async void AddUnit_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NewUnitInput.Text))
            {
                await _unitService.EnsureExistsAsync(NewUnitInput.Text.Trim());
                NewUnitInput.Text = "";
                await RefreshLists();
            }
        }

        private async void RemoveCategory_Click(object sender, RoutedEventArgs e)
        {
            var name = ((FrameworkElement)sender).Tag?.ToString();
            if (string.IsNullOrEmpty(name)) return;

            var confirm = MessageBox.Show($"Remove category '{name}'? Items already using it will keep the name, but it won't appear in the dropdown suggestions anymore.",
                "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                await _categoryService.RemoveAsync(name);
                await RefreshLists();
            }
        }

        private async void RemoveUnit_Click(object sender, RoutedEventArgs e)
        {
            var name = ((FrameworkElement)sender).Tag?.ToString();
            if (string.IsNullOrEmpty(name)) return;

            var confirm = MessageBox.Show($"Remove unit '{name}'? Items already using it will keep the value, but it won't appear in the dropdown suggestions anymore.",
                "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                await _unitService.RemoveAsync(name);
                await RefreshLists();
            }
        }

        private void BackToOffice_Click(object sender, RoutedEventArgs e)
        {
            var backOffice = new BackOfficeView(_currentUser);
            backOffice.Show();
            Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}