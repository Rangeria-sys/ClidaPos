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
        private readonly ShiftService _shiftService = new();
        private readonly DispatcherTimer _clockTimer;

        public GatewayView(Registration currentUser)
        {
            InitializeComponent();

            _vm = new GatewayViewModel(currentUser);

            StoreNameText.Text = AppSettings.StoreName.ToUpper();
            ModeText.Text = AppSettings.ModeLabel;
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
            EndPeriodButton.Visibility = _vm.IsShiftOpen ? Visibility.Visible : Visibility.Collapsed;

            ShiftStatusText.Text = _vm.IsShiftOpen
                ? "Period is OPEN — Front Office is unlocked."
                : "No open period. Start one to unlock Front Office.";
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
                    "Set \"AppMode\": \"Supermarket\" in appsettings.json to use the counter sales screen.",
                    "Clidapos");
                return;
            }

            new SalesView(_vm.CurrentUser).Show();
            Close();
        }

        private async void StartPeriod_Click(object sender, RoutedEventArgs e)
        {
            await _vm.StartPeriodAsync();
            await RefreshGates();
            MessageBox.Show("Period started. FRONT OFFICE is now unlocked.", "Clidapos");
        }

        private async void EndPeriod_Click(object sender, RoutedEventArgs e)
        {
            var open = await _shiftService.GetOpenPeriodAsync();

            if (open == null)
            {
                MessageBox.Show("There is no open period.", "Clidapos");
                await RefreshGates();
                return;
            }

            new DayEndView(_vm.CurrentUser, open.ID).Show();
            Close();
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