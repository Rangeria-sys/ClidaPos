using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class DayEndView : Window
    {
        private readonly Registration _currentUser;
        private readonly ReportService _reportService = new();
        private readonly ShiftService _shiftService = new();
        private readonly int _periodId;

        public DayEndView(Registration currentUser, int periodId)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _periodId = periodId;

            StoreText.Text = AppSettings.StoreName.ToUpper();
            VatLabel.Text = $"VAT at {AppSettings.VatPercent:0.##}% (included)";

            Loaded += async (s, e) => await LoadSummary();
        }

        private async System.Threading.Tasks.Task LoadSummary()
        {
            var s = await _reportService.GetPeriodSummaryAsync(_periodId);
            if (s == null)
            {
                MessageBox.Show("That work period could not be found.", "Clidapos");
                return;
            }

            var cur = AppSettings.CurrencySymbol;

            PeriodText.Text = s.EndedAt == null
                ? $"Period {s.PeriodId} — opened {s.StartedAt:dd MMM yyyy, hh:mm tt}"
                : $"Period {s.PeriodId} — {s.StartedAt:dd MMM hh:mm tt} to {s.EndedAt:dd MMM hh:mm tt}";

            StatusText.Text = s.IsOpen ? "Period is still OPEN" : "Period is CLOSED";

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

            TaxableText.Text = $"{cur} {s.TaxableTotal:N2}";
            VatText.Text = $"{cur} {s.VatTotal:N2}";

            if (s.TopItems.Count > 0)
            {
                TopItemsCard.Visibility = Visibility.Visible;
                TopItemsList.ItemsSource = s.TopItems;
            }

            ClosePeriodButton.Visibility = s.IsOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void ClosePeriod_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Close this period? Count the drawer against the cash figure above before confirming.",
                "Confirm Close", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            var closed = await _shiftService.EndPeriodAsync();

            if (closed)
            {
                MessageBox.Show("Period closed.", "Clidapos");
                await LoadSummary();
            }
            else
            {
                MessageBox.Show("There was no open period to close.", "Clidapos");
            }
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