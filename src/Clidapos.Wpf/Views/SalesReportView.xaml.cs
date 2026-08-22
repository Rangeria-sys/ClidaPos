using System;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SalesReportView : Window
    {
        private readonly Registration _currentUser;
        private readonly ReportService _reportService = new();

        public SalesReportView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            var today = DateTime.Today;
            FromDate.SelectedDate = today;
            ToDate.SelectedDate = today;

            Loaded += async (s, e) => await RunReport();
        }

        private async System.Threading.Tasks.Task RunReport()
        {
            if (FromDate.SelectedDate == null || ToDate.SelectedDate == null)
                return;

            var from = FromDate.SelectedDate.Value.Date;
            var to = ToDate.SelectedDate.Value.Date.AddDays(1).AddTicks(-1); // end of day

            var s = await _reportService.GetSalesReportAsync(from, to);
            var cur = AppSettings.CurrencySymbol;

            if (s.BillCount == 0)
            {
                ResultsPanel.Visibility = Visibility.Collapsed;
                EmptyText.Visibility = Visibility.Visible;
                return;
            }

            EmptyText.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Visible;

            GrandTotalText.Text = $"{cur} {s.GrandTotal:N2}";
            BillCountText.Text = s.BillCount.ToString("N0");
            ItemCountText.Text = s.ItemCount.ToString("N2");
            AverageText.Text = $"{cur} {s.AverageSale:N2}";

            CashText.Text = $"{cur} {s.CashTotal:N2}";
            MpesaText.Text = $"{cur} {s.MpesaTotal:N2}";
            CardText.Text = $"{cur} {s.CardTotal:N2}";

            if (s.OtherTotal > 0)
            {
                OtherRow.Visibility = Visibility.Visible;
                OtherText.Text = $"{cur} {s.OtherTotal:N2}";
            }
            else
            {
                OtherRow.Visibility = Visibility.Collapsed;
            }

            TaxableText.Text = $"{cur} {s.TaxableTotal:N2}";
            VatLabel.Text = $"VAT at {AppSettings.VatPercent:0.##}% (included)";
            VatText.Text = $"{cur} {s.VatTotal:N2}";

            if (s.TopItems.Count > 0)
            {
                TopItemsCard.Visibility = Visibility.Visible;
                TopItemsList.ItemsSource = s.TopItems;
            }
            else
            {
                TopItemsCard.Visibility = Visibility.Collapsed;
            }
        }

        private async void RunReport_Click(object sender, RoutedEventArgs e) => await RunReport();

        private async void Today_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            FromDate.SelectedDate = today;
            ToDate.SelectedDate = today;
            await RunReport();
        }

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
                printDialog.PrintVisual(ReportContent, "Clidapos - Sales Report");
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