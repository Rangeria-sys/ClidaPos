using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class WorkPeriodView : Window
    {
        private readonly Registration _currentUser;
        private readonly ShiftService _shiftService = new();
        private readonly LogService _logService = new();

        public WorkPeriodView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await RefreshStatus();
        }

        private async System.Threading.Tasks.Task RefreshStatus()
        {
            var open = await _shiftService.GetOpenPeriodAsync();

            if (open == null)
            {
                StatusText.Text = "No period is open";
                DetailText.Text = "Start one to unlock the POS.";
                StartButton.Visibility = Visibility.Visible;
                OpeningCashPanel.Visibility = Visibility.Visible;
                SummaryButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                StatusText.Text = "Period is OPEN";
                DetailText.Text = $"Started {open.WPStart:dd MMM yyyy, hh:mm tt}";
                StartButton.Visibility = Visibility.Collapsed;
                OpeningCashPanel.Visibility = Visibility.Collapsed;
                SummaryButton.Visibility = Visibility.Visible;
            }
        }

        private async void StartPeriod_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(OpeningCashInput.Text, out var openingCash) || openingCash < 0)
            {
                MessageBox.Show("Count the cash in the drawer and enter it - even if it's 0.", "Clidapos");
                return;
            }

            var started = await _shiftService.StartPeriodAsync(
                _currentUser.UserID.Trim(), _currentUser.Name.Trim(), openingCash);

            if (!started)
            {
                MessageBox.Show("A period is already open on this terminal.", "Clidapos");
                await RefreshStatus();
                return;
            }

            await _logService.LogAsync(CurrentSession.UserId, $"Started Work Period - Opening Cash {openingCash:N2}");
            MessageBox.Show("Period started. POS is now unlocked.", "Clidapos");
            new FrontOfficeHubView(_currentUser).Show();
            Close();
        }

        private async void GoToSummary_Click(object sender, RoutedEventArgs e)
        {
            var open = await _shiftService.GetOpenPeriodAsync();
            if (open == null)
            {
                await RefreshStatus();
                return;
            }

            new DayEndView(_currentUser, open.ID).Show();
            Close();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            new FrontOfficeHubView(_currentUser).Show();
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