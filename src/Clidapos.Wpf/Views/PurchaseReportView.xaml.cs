using System;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class PurchaseReportView : Window
    {
        private readonly Registration _currentUser;
        private readonly ReportService _reportService = new();

        public PurchaseReportView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            var today = DateTime.Today;
            FromDate.SelectedDate = new DateTime(today.Year, today.Month, 1);
            ToDate.SelectedDate = today;

            Loaded += async (s, e) => await RunReport();
        }

        private async System.Threading.Tasks.Task RunReport()
        {
            if (FromDate.SelectedDate == null || ToDate.SelectedDate == null)
                return;

            var from = FromDate.SelectedDate.Value.Date;
            var to = ToDate.SelectedDate.Value.Date.AddDays(1).AddTicks(-1); // end of day

            var s = await _reportService.GetPurchaseReportAsync(from, to);
            var cur = AppSettings.CurrencySymbol;

            if (s.PurchaseCount == 0)
            {
                ResultsPanel.Visibility = Visibility.Collapsed;
                EmptyText.Visibility = Visibility.Visible;
                return;
            }

            EmptyText.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Visible;

            GrandTotalText.Text = $"{cur} {s.GrandTotal:N2}";
            PurchaseCountText.Text = s.PurchaseCount.ToString("N0");
            ItemCountText.Text = s.TotalItems.ToString("N2");

            SupplierGrid.ItemsSource = s.BySupplier;
            PurchaseGrid.ItemsSource = s.Purchases;
        }

        private async void RunReport_Click(object sender, RoutedEventArgs e) => await RunReport();

        private async void ThisWeek_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            FromDate.SelectedDate = today.AddDays(-daysSinceMonday);
            ToDate.SelectedDate = today;
            await RunReport();
        }

        private async void ThisMonth_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            FromDate.SelectedDate = new DateTime(today.Year, today.Month, 1);
            ToDate.SelectedDate = today;
            await RunReport();
        }

        private async void ThisYear_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            FromDate.SelectedDate = new DateTime(today.Year, 1, 1);
            ToDate.SelectedDate = today;
            await RunReport();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsPanel.Visibility != Visibility.Visible)
            {
                MessageBox.Show("Run a report with results before printing.", "Clidapos");
                return;
            }

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(ReportContent, "Clidapos - Purchase Report");
            }
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