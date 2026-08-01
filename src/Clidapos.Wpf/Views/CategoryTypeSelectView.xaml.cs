using System.Windows;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Views
{
    public partial class CategoryTypeSelectView : Window
    {
        private readonly Registration _currentUser;

        public CategoryTypeSelectView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            WelcomeText.Text = $"{currentUser.Name.Trim()} ({currentUser.UserType.Trim()})";
        }

        private void ItemCategory_Click(object sender, RoutedEventArgs e)
        {
            var popup = new ItemCategoryPopup { Owner = this };
            popup.ShowDialog();
        }

        private void UnitCategory_Click(object sender, RoutedEventArgs e)
        {
            var popup = new UnitCategoryPopup { Owner = this };
            popup.ShowDialog();
        }

        private void ExpenseCategory_Click(object sender, RoutedEventArgs e)
        {
            var popup = new ExpenseCategoryPopup { Owner = this };
            popup.ShowDialog();
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