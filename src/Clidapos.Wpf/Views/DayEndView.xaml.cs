using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class DayEndView : Window
    {
        private readonly Registration _currentUser;
        private readonly int _periodId;
        private readonly ReportService _reportService = new();
        private readonly ShiftService _shiftService = new();
        private readonly LogService _logService = new();

        private ShiftSummary? _summary;

        public DayEndView(Registration currentUser, int periodId)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _periodId = periodId;
            Loaded += async (s, e) => await LoadSummary();
        }

        private async System.Threading.Tasks.Task LoadSummary()
        {
            _summary = await _reportService.GetPeriodSummaryAsync(_periodId);
            if (_summary == null)
            {
                MessageBox.Show("This period could not be found.", "Clidapos");
                Close();
                return;
            }

            var cur = AppSettings.CurrencySymbol;

            TitleText.Text = $"{AppSettings.StoreName} — Period {_periodId}";
            TakingsText.Text = $"{cur} {_summary.GrandTotal:N2}";
            BillCountText.Text = _summary.BillCount.ToString();
            ItemCountText.Text = _summary.ItemCount.ToString("N0");
            AverageText.Text = $"{cur} {_summary.AverageSale:N2}";

            OpeningCashText.Text = _summary.OpeningCash == null ? "Not recorded" : $"{cur} {_summary.OpeningCash:N2}";
            CashSalesText.Text = $"{cur} {_summary.CashTotal:N2}";
            ExpectedCashText.Text = _summary.ExpectedCash == null ? "—" : $"{cur} {_summary.ExpectedCash:N2}";

            UpdateVariancePreview();

            _ = Dispatcher.BeginInvoke(new Action(() => CountedCashText.Focus()),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // ---------------- COUNTED CASH INPUT ----------------
        // CountedCashText.Text is the single source of truth. Every path that
        // can change it - the on-screen keypad, or the physical keyboard via
        // PreviewTextInput - calls UpdateVariancePreview() directly and
        // explicitly right afterward. Deliberately not relying on the
        // TextChanged event to drive anything, since that indirection was
        // the actual source of the "nothing responds" bug reported earlier -
        // this way there is exactly one, traceable path from a keystroke to
        // the screen updating, with no hidden event-wiring in between.

        private void Digit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var digit = btn.Content?.ToString() ?? "";

            if (digit == "." && CountedCashText.Text.Contains('.')) return;

            CountedCashText.Text += digit;
            CountedCashText.CaretIndex = CountedCashText.Text.Length;
            UpdateVariancePreview();
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (CountedCashText.Text.Length == 0) return;
            CountedCashText.Text = CountedCashText.Text[..^1];
            CountedCashText.CaretIndex = CountedCashText.Text.Length;
            UpdateVariancePreview();
        }

        // Physical keyboard input - restricted to digits and a single
        // decimal point, matching the on-screen keypad. Always handles the
        // keystroke itself (e.Handled = true) rather than letting WPF's own
        // insertion run - modifying .Text mid-handler and then letting the
        // default insertion continue afterward was breaking things before.
        private void CountedCashText_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = true;

            if (e.Text == ".")
            {
                if (CountedCashText.Text.Contains('.')) return;
            }
            else if (e.Text.Length != 1 || !char.IsDigit(e.Text[0]))
            {
                return;
            }

            CountedCashText.Text += e.Text;
            CountedCashText.CaretIndex = CountedCashText.Text.Length;
            UpdateVariancePreview();
        }

        // Enter must never submit or add a value here.
        // Enter triggers Confirm & Close, same as clicking the button -
        // Backspace is deliberately not handled anywhere in this method, so
        // it keeps working exactly as a normal TextBox always has.
        private void CountedCashText_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Return)
            {
                e.Handled = true;
                ConfirmClose_Click(sender, e);
            }
        }

        /// <summary>An empty field means 0.00 - not "invalid, can't proceed".
        /// The Confirm button is always clickable; this is only used to
        /// interpret whatever is currently in the field.</summary>
        private decimal GetCountedCash()
        {
            if (string.IsNullOrWhiteSpace(CountedCashText.Text)) return 0m;
            return decimal.TryParse(CountedCashText.Text, out var counted) && counted >= 0 ? counted : 0m;
        }

        /// <summary>Updates live as digits are typed, before Confirm is
        /// pressed.</summary>
        private void UpdateVariancePreview()
        {
            if (_summary?.ExpectedCash == null)
            {
                VarianceText.Text = "No opening float recorded";
                VarianceText.Foreground = new SolidColorBrush(Color.FromRgb(0xB8, 0x86, 0x0B));
                return;
            }

            var counted = GetCountedCash();
            var cur = AppSettings.CurrencySymbol;
            var variance = counted - _summary.ExpectedCash.Value;

            if (variance == 0)
            {
                VarianceText.Text = $"Exact match ({cur} 0.00)";
                VarianceText.Foreground = new SolidColorBrush(Color.FromRgb(0x1B, 0x8A, 0x3D));
            }
            else if (variance > 0)
            {
                VarianceText.Text = $"Over by {cur} {variance:N2}";
                VarianceText.Foreground = new SolidColorBrush(Color.FromRgb(0x1B, 0x8A, 0x3D));
            }
            else
            {
                VarianceText.Text = $"SHORT by {cur} {Math.Abs(variance):N2}";
                VarianceText.Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B));
            }
        }

        // ---------------- CONFIRM & CLOSE ----------------
        private async void ConfirmClose_Click(object sender, RoutedEventArgs e)
        {
            var isServer = TerminalRoleService.IsServerTerminal();

            // Hard block: the server must not close until every other
            // terminal has already closed its own period, since the server's
            // close is what generates the comprehensive end-of-day report -
            // an incomplete one would be actively misleading.
            if (isServer)
            {
                var stillOpen = await _shiftService.GetOtherOpenTerminalsAsync();
                if (stillOpen.Count > 0)
                {
                    var names = string.Join(", ", stillOpen.Select(t => $"{t.TerminalID}" + (string.IsNullOrWhiteSpace(t.CashierName) ? "" : $" ({t.CashierName})")));
                    MessageBox.Show(
                        $"Cannot close yet - {stillOpen.Count} other terminal{(stillOpen.Count == 1 ? " is" : "s are")} still open: {names}.\n\nEvery terminal must close its own period before the server can close and generate the end-of-day report.",
                        "Clidapos", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var counted = GetCountedCash();
            var cur = AppSettings.CurrencySymbol;
            var message = "Close this period?";

            if (_summary?.ExpectedCash != null)
            {
                var variance = counted - _summary.ExpectedCash.Value;
                message = variance == 0
                    ? $"Close this period? Drawer matches exactly ({cur} {_summary.ExpectedCash:N2})."
                    : variance > 0
                        ? $"Close this period? Drawer is over by {cur} {variance:N2}."
                        : $"Close this period? Drawer is SHORT by {cur} {Math.Abs(variance):N2}. Continue anyway?";
            }

            var confirm = MessageBox.Show(message, "Confirm Close", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            var closed = await _shiftService.EndPeriodAsync(counted);
            if (!closed)
            {
                MessageBox.Show("This period could not be closed - it may already be closed.", "Clidapos");
                return;
            }

            await _logService.LogAsync(CurrentSession.UserId, $"Ended Work Period - Counted Cash {counted:N2}");

            var emailNote = isServer ? await SendEndOfDayReportEmailAsync() : "";
            MessageBox.Show($"Period closed.{emailNote}", "Clidapos");
            new FrontOfficeHubView(_currentUser).Show();
            Close();
        }

        /// <summary>Only ever called from the server terminal's close - builds
        /// the comprehensive end-of-day report (combined sales across every
        /// terminal, plus each terminal's individual cash reconciliation) and
        /// emails it to the owner. Best-effort - a failure here never undoes
        /// the already-successful close.</summary>
        private async System.Threading.Tasks.Task<string> SendEndOfDayReportEmailAsync()
        {
            try
            {
                var hotel = await new HotelProfileService().GetOrCreateAsync();
                if (string.IsNullOrWhiteSpace(hotel.EmailID))
                    return "\n\nNo email could be sent - the owner's email isn't set in Business Profile.";

                var today = DateTime.Today;
                var report = await _reportService.GetEndOfDayReportAsync(today);
                var cur = AppSettings.CurrencySymbol;

                var pdfBytes = new ExportService().GenerateEndOfDayPdfBytes(AppSettings.StoreName, cur, today, report);

                var body = $"End of Day Report for {AppSettings.StoreName} — {today:dd MMM yyyy}\n\n" +
                           "Combined totals across every terminal, plus each terminal's individual cash reconciliation. " +
                           "See the attached PDF for the full report.";

                var result = await new EmailService().SendAsync(
                    hotel.EmailID.Trim(), $"End of Day Report — {today:dd MMM yyyy}", body,
                    pdfBytes, $"EndOfDay_{today:yyyyMMdd}.pdf");

                return result.Success ? "\n\nEnd-of-day report emailed to the owner." : $"\n\nCould not email the report: {result.Message}";
            }
            catch (Exception ex)
            {
                return $"\n\nCould not email the report: {ex.Message}";
            }
        }


        private void Back_Click(object sender, RoutedEventArgs e)
        {
            new WorkPeriodView(_currentUser).Show();
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            new WorkPeriodView(_currentUser).Show();
            Close();
        }
    }
}
