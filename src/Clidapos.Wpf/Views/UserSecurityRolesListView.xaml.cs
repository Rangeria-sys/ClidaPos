using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class UserSecurityRolesListView : Window
    {
        private readonly RegistrationService _registrationService = new();
        private List<Registration> _all = new();

        public UserSecurityRolesListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _registrationService.GetAllAsync();
            UserGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            UserGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(u => u.UserID.Trim().ToLower().Contains(q)
                               || u.Name.Trim().ToLower().Contains(q)
                               || u.UserType.Trim().ToLower().Contains(q)).ToList();
        }

        private void UserGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (UserGrid.SelectedItem is Registration user)
            {
                var popup = new UserSecurityRolesPopup(user);
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