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

            Loaded += async (s, e) => await EnforceLicenseAsync();
        }

        // Real license enforcement: loops until a valid, unexpired key is on record,
        // or the person chooses to close the app instead. This runs before login is
        // even attempted, since Master Settings (where a key is normally entered) is
        // only reachable after logging in - checking here avoids a lockout deadlock
        // on a fresh, unactivated install.
        private async System.Threading.Tasks.Task EnforceLicenseAsync()
        {
            while (true)
            {
                var (isValid, reason) = await EvaluateLicenseAsync();
                if (isValid) return;

                var result = MessageBox.Show(
                    $"{reason}\n\nEnter a license key now, or Cancel to close Clidapos.",
                    "License Required", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                if (result != MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();
                    return;
                }

                var popup = new LicenseSettingPopup { Owner = this };
                popup.ShowDialog();
                // Loop back and re-check - if they activated successfully the loop
                // exits above; otherwise they see the same prompt again.
            }
        }

        private async System.Threading.Tasks.Task<(bool isValid, string reason)> EvaluateLicenseAsync()
        {
            var settingsService = new TerminalLicenseService();
            var setting = await settingsService.GetOrCreateLicenseAsync();

            var isActive = setting.IsActive?.Trim().ToUpper() == "Y";
            var storedKey = setting.LicenseKey?.Trim() ?? "";

            if (!isActive || string.IsNullOrWhiteSpace(storedKey))
                return (false, "This installation has not been activated. Contact your provider to purchase a license.");

            if (!LicenseKeyService.TryValidate(storedKey, out var durationCode, out _))
                return (false, "The license on record is invalid. Contact your provider for a new key.");

            var activatedDate = setting.ActivatedDate ?? DateTime.Now;
            var expiry = LicenseKeyService.ComputeExpiry(activatedDate, durationCode);

            if (expiry != null && expiry.Value.Date < DateTime.Today)
                return (false, $"Your license expired on {expiry.Value:dd MMM yyyy}. Contact your provider to renew and receive a new key.");

            return (true, "");
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