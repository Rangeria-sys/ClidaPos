using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class ExpenseReportView : Window
    {
        private readonly Registration _currentUser;
        private readonly VoucherService _voucherService = new();

        public ExpenseReportView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            FromDateInput.SelectedDate = DateTime.Today.AddDays(-30);
            ToDateInput.SelectedDate = DateTime.Today;

            Loaded += async (s, e) => await RunReport();
        }

        private async System.Threading.Tasks.Task RunReport()
        {
            var from = FromDateInput.SelectedDate ?? DateTime.Today.AddDays(-30);
            var to = ToDateInput.SelectedDate ?? DateTime.Today;

            var summary = await _voucherService.GetExpenseReportAsync(from, to);

            TotalSpentText.Text = $"KSh {summary.TotalSpent:N2}";
            VoucherCountText.Text = summary.VoucherCount.ToString();
            PaymentModeList.ItemsSource = summary.ByPaymentMode;
            ParticularsList.ItemsSource = summary.TopParticulars;
            VoucherGrid.ItemsSource = summary.Vouchers;
        }

        private async void RunReport_Click(object sender, RoutedEventArgs e) => await RunReport();

        private async void Today_Click(object sender, RoutedEventArgs e)
        {
            FromDateInput.SelectedDate = DateTime.Today;
            ToDateInput.SelectedDate = DateTime.Today;
            await RunReport();
        }

        private async void ThisWeek_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            FromDateInput.SelectedDate = today.AddDays(-daysSinceMonday);
            ToDateInput.SelectedDate = today;
            await RunReport();
        }

        private async void ThisMonth_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            FromDateInput.SelectedDate = new DateTime(today.Year, today.Month, 1);
            ToDateInput.SelectedDate = today;
            await RunReport();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var backOffice = new BackOfficeView(_currentUser);
            backOffice.Show();
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}