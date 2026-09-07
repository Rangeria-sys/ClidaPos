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
        private readonly LogService _logService = new();
        private readonly HotelProfileService _hotelService = new();
        private readonly EmailService _emailService = new();
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
                await _logService.LogAsync(CurrentSession.UserId, "Ended Work Period");
                var emailNote = await SendClosingReportEmailAsync();
                MessageBox.Show($"Period closed.{emailNote}", "Clidapos");
                await LoadSummary();
            }
            else
            {
                MessageBox.Show("There was no open period to close.", "Clidapos");
            }
        }

        /// <summary>
        /// Emails the just-closed period's summary to the address set under
        /// Business Profile. Returns a short note to append to the close
        /// confirmation - a failure here never blocks the close itself, since
        /// the period is already closed by the time this runs.
        /// </summary>
        private async System.Threading.Tasks.Task<string> SendClosingReportEmailAsync()
        {
            var hotel = await _hotelService.GetOrCreateAsync();
            var toAddress = hotel.EmailID?.Trim();

            if (string.IsNullOrWhiteSpace(toAddress))
                return "\n\n(No email set in Business Profile - closing report was not sent.)";

            var s = await _reportService.GetPeriodSummaryAsync(_periodId);
            if (s == null) return "";

            var cur = AppSettings.CurrencySymbol;
            var body = new System.Text.StringBuilder();
            body.AppendLine($"{AppSettings.StoreName.ToUpper()} - WORK PERIOD CLOSING REPORT");
            body.AppendLine($"Period {s.PeriodId}: {s.StartedAt:dd MMM yyyy, hh:mm tt} to {s.EndedAt:dd MMM yyyy, hh:mm tt}");
            body.AppendLine();
            body.AppendLine($"Grand Total: {cur} {s.GrandTotal:N2}");
            body.AppendLine($"Bills: {s.BillCount:N0}");
            body.AppendLine($"Items Sold: {s.ItemCount:N2}");
            body.AppendLine($"Average Sale: {cur} {s.AverageSale:N2}");
            body.AppendLine();
            body.AppendLine("Payment Breakdown");
            body.AppendLine($"  Cash:   {cur} {s.CashTotal:N2}");
            body.AppendLine($"  M-Pesa: {cur} {s.MpesaTotal:N2}");
            body.AppendLine($"  Card:   {cur} {s.CardTotal:N2}");
            if (s.OtherTotal > 0)
                body.AppendLine($"  Other:  {cur} {s.OtherTotal:N2}");
            body.AppendLine();
            body.AppendLine($"Net of VAT: {cur} {s.TaxableTotal:N2}");
            body.AppendLine($"VAT at {AppSettings.VatPercent:0.##}%: {cur} {s.VatTotal:N2}");

            if (s.TopItems.Count > 0)
            {
                body.AppendLine();
                body.AppendLine("Top Selling Items");
                foreach (var item in s.TopItems)
                    body.AppendLine($"  {item.Qty:N0} x {item.Name} - {cur} {item.Value:N2}");
            }

            var subject = $"{AppSettings.StoreName} - Period {s.PeriodId} Closing Report ({s.EndedAt:dd MMM yyyy})";
            var result = await _emailService.SendAsync(toAddress, subject, body.ToString());

            return result.Success
                ? $"\n\nClosing report emailed to {toAddress}."
                : $"\n\n(Closing report email failed: {result.Message})";
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