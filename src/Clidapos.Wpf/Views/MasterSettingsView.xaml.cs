using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class MasterSettingsView : Window
    {
        private readonly Registration _currentUser;

        public MasterSettingsView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            WelcomeText.Text = $"{currentUser.Name.Trim()} ({currentUser.UserType.Trim()})";

            // Restaurant-only tiles - hidden in Supermarket mode, same pattern used
            // in BackOfficeView and FrontOfficeHubView.
            var isRestaurant = AppSettings.Mode == StoreMode.Restaurant;
            MenuLayoutTile.Visibility = isRestaurant ? Visibility.Visible : Visibility.Collapsed;
            TableSettingTile.Visibility = isRestaurant ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            var tag = ((Button)sender).Tag?.ToString() ?? "";

            var sectionName = tag switch
            {
                "Terminal" => "Terminal Setting",
                "Email" => "Email Setting",
                "SMS" => "SMS Setting",
                "EStoreAccess" => "eStore Access",
                "MenuLayout" => "Menu Layout",
                "TableSetting" => "Table Setting",
                "Wallet" => "Wallet",
                "MpesaApi" => "M-Pesa API Setting",
                "ExecuteQuery" => "Execute Query",
                "LicenseActivation" => "License Activation",
                "WorkPeriod" => "Work Period",
                _ => "Unknown section"
            };

            MessageBox.Show($"{sectionName} — coming soon.", "Clidapos");
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