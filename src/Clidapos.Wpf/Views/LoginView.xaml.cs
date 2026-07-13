using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Clidapos.Wpf.ViewModels;

namespace Clidapos.Wpf.Views
{
    public partial class LoginView : Window
    {
        private readonly DispatcherTimer _clockTimer;

        public LoginView()
        {
            InitializeComponent();

            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
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

        private void Digit_Click(object sender, RoutedEventArgs e)
        {
            var digit = ((Button)sender).Content.ToString();
            Vm.Pin += digit;
            Vm.ErrorMessage = string.Empty;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Vm.Pin = string.Empty;
            Vm.ErrorMessage = string.Empty;
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            await Vm.LoginAsync();
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