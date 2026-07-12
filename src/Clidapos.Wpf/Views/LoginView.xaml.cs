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
    }
}