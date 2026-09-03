using System;
using System.Windows;
using System.Windows.Threading;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class FrontOfficeHubView : Window
    {
        private readonly Registration _currentUser;
        private readonly ShiftService _shiftService = new();
        private readonly ClockService _clockService = new();
        private readonly LogService _logService = new();
        private readonly DispatcherTimer _clockTimer;

        public FrontOfficeHubView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            OperatorText.Text = $"Operator ID: {currentUser.UserID.Trim()}";
            KitchenTile.Visibility = AppSettings.Mode == StoreMode.Restaurant
                ? Visibility.Visible
                : Visibility.Collapsed;

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();

            Loaded += async (s, e) => await RefreshClockStatus();
        }

        private void UpdateClock()
        {
            DateTimeText.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy  hh:mm:ss tt");
        }

        // Shows whether the current operator is currently clocked in, and since when -
        // so the tile's own state is visible before they even click it.
        private async System.Threading.Tasks.Task RefreshClockStatus()
        {
            var open = await _clockService.GetOpenEntryAsync(_currentUser.UserID.Trim());
            ClockStatusText.Text = open != null
                ? $"Clocked in since {open.ClockInTime:hh:mm tt}"
                : "Not clocked in";
        }

        private void WorkPeriod_Click(object sender, RoutedEventArgs e)
        {
            new WorkPeriodView(_currentUser).Show();
            Close();
        }

        private async void Pos_Click(object sender, RoutedEventArgs e)
        {
            var open = await _shiftService.IsShiftOpenAsync();
            if (!open)
            {
                MessageBox.Show("No period is open. Open one from Work Period first.", "Clidapos");
                return;
            }

            new SalesView(_currentUser).Show();
            Close();
        }

        private void KitchenDisplay_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Kitchen Display is not built yet.", "Clidapos");
        }

        private async void Report_Click(object sender, RoutedEventArgs e)
        {
            var latest = await _shiftService.GetLatestPeriodAsync();
            if (latest == null)
            {
                MessageBox.Show("No sales period has been recorded yet.", "Clidapos");
                return;
            }

            new DayEndView(_currentUser, latest.ID).Show();
            Close();
        }

        // One tile does both jobs: clocks the operator in if they're not already,
        // or clocks them out (showing how long the shift ran) if they are.
        private async void ClockInOut_Click(object sender, RoutedEventArgs e)
        {
            var result = await _clockService.ToggleAsync(_currentUser.UserID.Trim(), _currentUser.Name.Trim());

            if (result.JustClockedIn)
            {
                await _logService.LogAsync(CurrentSession.UserId, "Clocked in");
                MessageBox.Show($"Clocked in at {result.ClockInTime:hh:mm tt}.", "Clidapos");
            }
            else
            {
                await _logService.LogAsync(CurrentSession.UserId,
                    $"Clocked out at {result.ClockOutTime:hh:mm tt} (shift: {result.Duration:hh\\:mm})");
                MessageBox.Show(
                    $"Clocked out at {result.ClockOutTime:hh:mm tt}.\n\nShift duration: {result.Duration:hh\\:mm}",
                    "Clidapos");
            }

            await RefreshClockStatus();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Log out and return to the login screen?", "Confirm Logout",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            new LoginView().Show();
            Close();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            new GatewayView(_currentUser).Show();
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Close Clidapos?", "Confirm Exit",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
    }
}