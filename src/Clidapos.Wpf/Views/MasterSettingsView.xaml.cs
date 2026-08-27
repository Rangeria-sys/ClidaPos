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

            if (tag == "MpesaApi")
            {
                var mpesaPopup = new MpesaSettingPopup { Owner = this };
                mpesaPopup.Show();
                return;
            }

            if (tag == "Email")
            {
                var emailListView = new EmailSettingListView { Owner = this };
                emailListView.Show();
                return;
            }

            if (tag == "SMS")
            {
                var smsListView = new SMSSettingListView { Owner = this };
                smsListView.Show();
                return;
            }

            if (tag == "Wallet")
            {
                var walletTypeListView = new WalletTypeListView { Owner = this };
                walletTypeListView.Show();
                return;
            }

            if (tag == "Terminal")
            {
                var terminalPopup = new TerminalSettingPopup { Owner = this };
                terminalPopup.Show();
                return;
            }

            if (tag == "LicenseActivation")
            {
                var licensePopup = new LicenseSettingPopup { Owner = this };
                licensePopup.Show();
                return;
            }

            if (tag == "WorkPeriod")
            {
                var workPeriodPopup = new WorkPeriodSettingPopup { Owner = this };
                workPeriodPopup.Show();
                return;
            }

            var sectionName = tag switch
            {
                "EStoreAccess" => "eStore Access",
                "MenuLayout" => "Menu Layout",
                "TableSetting" => "Table Setting",
                "ExecuteQuery" => "Execute Query",
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