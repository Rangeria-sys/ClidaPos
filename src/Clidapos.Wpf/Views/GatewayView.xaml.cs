using System;
using System.Windows;
using System.Windows.Threading;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;
using Clidapos.Wpf.ViewModels;

namespace Clidapos.Wpf.Views
{
    public partial class GatewayView : Window
    {
        private readonly GatewayViewModel _vm;
        private readonly DispatcherTimer _clockTimer;

        public GatewayView(Registration currentUser)
        {
            InitializeComponent();

            _vm = new GatewayViewModel(currentUser);

            StoreNameText.Text = AppSettings.StoreName.ToUpper();
            ModeText.Text = AppSettings.ModeLabel;
            WelcomeText.Text = $"Logged in as {currentUser.Name.Trim()} ({currentUser.UserType.Trim()})";
            BackOfficeButton.IsEnabled = _vm.IsAdmin;

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            TimeText.Text = now.ToString("hh:mm:ss tt");
            DateText.Text = now.ToString("dddd, dd MMMM yyyy");
        }

        private void BackOffice_Click(object sender, RoutedEventArgs e)
        {
            var backOffice = new BackOfficeView(_vm.CurrentUser);
            backOffice.Show();
            Close();
        }

        private void FrontOffice_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.Mode == StoreMode.Restaurant)
            {
                MessageBox.Show(
                    "Restaurant mode (menu, tables, KOT) is not built yet.\n\n" +
                    "Set \"AppMode\": \"Supermarket\" in appsettings.json to use the Front Office.",
                    "Clidapos");
                return;
            }

            new FrontOfficeHubView(_vm.CurrentUser).Show();
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