using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Clidapos.Wpf.Services;
using Clidapos.Wpf.ViewModels;

namespace Clidapos.Wpf.Views
{
    public partial class LoginView : Window
    {
        private readonly DispatcherTimer _clockTimer;
        private readonly LogService _logService = new();

        public LoginView()
        {
            InitializeComponent();

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            DateText.Text = now.ToString("dddd, dd MMMM yyyy");
            TimeText.Text = now.ToString("hh:mm:ss tt");
        }

        private LoginViewModel Vm => (LoginViewModel)DataContext;

        private async void Digit_Click(object sender, RoutedEventArgs e)
        {
            var digit = ((Button)sender).Content.ToString();
            Vm.Pin += digit;
            Vm.ErrorMessage = string.Empty;

            if (Vm.Pin.Length == LoginViewModel.PinLength)
            {
                await DoLoginAndNavigate();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Vm.Pin = string.Empty;
            Vm.ErrorMessage = string.Empty;
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            await DoLoginAndNavigate();
        }

        private async System.Threading.Tasks.Task DoLoginAndNavigate()
        {
            await Vm.LoginAsync();

            if (Vm.LoggedInUser != null)
            {
                // Session tracking starts here, at the moment of login, so any
                // screen reached afterward - Front Office or Back Office - can
                // log actions under the correct user.
                CurrentSession.UserId = Vm.LoggedInUser.UserID.Trim();
                await _logService.LogAsync(CurrentSession.UserId, "Logged in");

                var gateway = new GatewayView(Vm.LoggedInUser);
                gateway.Show();
                Close();
            }
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