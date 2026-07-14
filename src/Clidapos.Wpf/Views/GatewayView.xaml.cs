using System;
using System.Windows;
using System.Windows.Threading;
using Clidapos.Wpf.Entities;
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
            WelcomeText.Text = $"Logged in as {currentUser.Name.Trim()} ({currentUser.UserType.Trim()})";

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();

            Loaded += async (s, e) => await RefreshGates();
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            TimeText.Text = now.ToString("hh:mm:ss tt");
            DateText.Text = now.ToString("dddd, dd MMMM yyyy");
        }

        private async System.Threading.Tasks.Task RefreshGates()
        {
            await _vm.RefreshShiftStatusAsync();

            BackOfficeButton.IsEnabled = _vm.IsAdmin;
            FrontOfficeButton.IsEnabled = _vm.IsShiftOpen;
            StartPeriodButton.Visibility = _vm.IsShiftOpen ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BackOffice_Click(object sender, RoutedEventArgs e)
        {
            var backOffice = new BackOfficeView(_vm.CurrentUser);
            backOffice.Show();
            Close();
        }

        private void FrontOffice_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("FRONT OFFICE screen coming in a later module.", "Clidapos");
        }

        private async void StartPeriod_Click(object sender, RoutedEventArgs e)
        {
            await _vm.StartPeriodAsync();
            await RefreshGates();
            MessageBox.Show("Period started. FRONT OFFICE is now unlocked.", "Clidapos");
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