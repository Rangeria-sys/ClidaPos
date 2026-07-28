using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class WorkPeriodView : Window
    {
        private readonly Registration _currentUser;
        private readonly ShiftService _shiftService = new();

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
                SummaryButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                StatusText.Text = "Period is OPEN";
                DetailText.Text = $"Started {open.WPStart:dd MMM yyyy, hh:mm tt}";
                StartButton.Visibility = Visibility.Collapsed;
                SummaryButton.Visibility = Visibility.Visible;
            }
        }

        private async void StartPeriod_Click(object sender, RoutedEventArgs e)
        {
            await _shiftService.StartPeriodAsync();
            await RefreshStatus();
            MessageBox.Show("Period started. POS is now unlocked.", "Clidapos");
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