using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.ViewModels;

namespace Clidapos.Wpf.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
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