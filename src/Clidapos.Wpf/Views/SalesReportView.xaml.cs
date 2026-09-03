using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SalesReportView : Window
    {
        private readonly Registration _currentUser;
        private readonly ReportService _reportService = new();
        private readonly ExportService _exportService = new();

        private enum Tab { Totals, Payment, TopItems, Cashier, Voided }
        private Tab _activeTab = Tab.Totals;
        private SalesBreakdownGranularity _granularity = SalesBreakdownGranularity.Daily;

        // Cached results from the last run, reused by whichever tab is showing and by export.
        private ShiftSummary? _summary;
        private List<SalesBreakdownRow> _breakdown = new();
        private List<CashierSalesRow> _cashierRows = new();
        private VoidedSalesSummary _voided = new();
        private DateTime _from;
        private DateTime _to;

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

            _from = FromDate.SelectedDate.Value.Date;
            _to = ToDate.SelectedDate.Value.Date.AddDays(1).AddTicks(-1); // end of day

            _summary = await _reportService.GetSalesReportAsync(_from, _to);
            var cur = AppSettings.CurrencySymbol;

            if (_summary.BillCount == 0)
            {
                ResultsPanel.Visibility = Visibility.Collapsed;
                EmptyText.Visibility = Visibility.Visible;
                return;
            }

            EmptyText.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Visible;

            // ---- Totals tab ----
            GrandTotalText.Text = $"{cur} {_summary.GrandTotal:N2}";
            BillCountText.Text = _summary.BillCount.ToString("N0");
            ItemCountText.Text = _summary.ItemCount.ToString("N2");
            AverageText.Text = $"{cur} {_summary.AverageSale:N2}";

            _breakdown = await _reportService.GetSalesBreakdownAsync(_from, _to, _granularity);
            BreakdownGrid.ItemsSource = _breakdown;

            // ---- Payment mode tab ----
            CashText.Text = $"{cur} {_summary.CashTotal:N2}";
            MpesaText.Text = $"{cur} {_summary.MpesaTotal:N2}";
            CardText.Text = $"{cur} {_summary.CardTotal:N2}";

            if (_summary.OtherTotal > 0)
            {
                OtherRow.Visibility = Visibility.Visible;
                OtherText.Text = $"{cur} {_summary.OtherTotal:N2}";
            }
            else
            {
                OtherRow.Visibility = Visibility.Collapsed;
            }

            TaxableText.Text = $"{cur} {_summary.TaxableTotal:N2}";
            VatLabel.Text = $"VAT at {AppSettings.VatPercent:0.##}% (included)";
            VatText.Text = $"{cur} {_summary.VatTotal:N2}";

            // ---- Top items tab ----
            TopItemsList.ItemsSource = _summary.TopItems;
            NoTopItemsText.Visibility = _summary.TopItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // ---- By cashier tab ----
            _cashierRows = await _reportService.GetSalesByCashierAsync(_from, _to);
            CashierGrid.ItemsSource = _cashierRows;

            // ---- Voided sales tab ----
            _voided = await _reportService.GetVoidedSalesAsync(_from, _to);
            VoidedTotalText.Text = $"{cur} {_voided.TotalVoided:N2}";
            VoidedCountText.Text = _voided.Count == 1 ? "1 voided sale" : $"{_voided.Count} voided sales";
            VoidedGrid.ItemsSource = _voided.Rows;
            NoVoidedText.Visibility = _voided.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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

        // ---------------- TAB SWITCHING ----------------
        // ToggleButtons aren't grouped like radio buttons by default, so exclusivity
        // (and panel visibility) is managed by hand here.

        private void SetActiveTab(Tab tab)
        {
            _activeTab = tab;

            TotalsTab.Visibility = tab == Tab.Totals ? Visibility.Visible : Visibility.Collapsed;
            PaymentTab.Visibility = tab == Tab.Payment ? Visibility.Visible : Visibility.Collapsed;
            TopItemsTab.Visibility = tab == Tab.TopItems ? Visibility.Visible : Visibility.Collapsed;
            CashierTab.Visibility = tab == Tab.Cashier ? Visibility.Visible : Visibility.Collapsed;
            VoidedTab.Visibility = tab == Tab.Voided ? Visibility.Visible : Visibility.Collapsed;

            TabTotalsBtn.IsChecked = tab == Tab.Totals;
            TabPaymentBtn.IsChecked = tab == Tab.Payment;
            TabTopItemsBtn.IsChecked = tab == Tab.TopItems;
            TabCashierBtn.IsChecked = tab == Tab.Cashier;
            TabVoidedBtn.IsChecked = tab == Tab.Voided;
        }

        private void TabTotals_Click(object sender, RoutedEventArgs e) => SetActiveTab(Tab.Totals);
        private void TabPayment_Click(object sender, RoutedEventArgs e) => SetActiveTab(Tab.Payment);
        private void TabTopItems_Click(object sender, RoutedEventArgs e) => SetActiveTab(Tab.TopItems);
        private void TabCashier_Click(object sender, RoutedEventArgs e) => SetActiveTab(Tab.Cashier);
        private void TabVoided_Click(object sender, RoutedEventArgs e) => SetActiveTab(Tab.Voided);

        private async void Granularity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton clicked) return;

            _granularity = clicked.Name switch
            {
                nameof(WeeklyBtn) => SalesBreakdownGranularity.Weekly,
                nameof(MonthlyBtn) => SalesBreakdownGranularity.Monthly,
                _ => SalesBreakdownGranularity.Daily
            };

            DailyBtn.IsChecked = _granularity == SalesBreakdownGranularity.Daily;
            WeeklyBtn.IsChecked = _granularity == SalesBreakdownGranularity.Weekly;
            MonthlyBtn.IsChecked = _granularity == SalesBreakdownGranularity.Monthly;

            _breakdown = await _reportService.GetSalesBreakdownAsync(_from, _to, _granularity);
            BreakdownGrid.ItemsSource = _breakdown;
        }

        // ---------------- EXPORT ----------------

        private (string Title, IReadOnlyList<(string, string)> Summary, IReadOnlyList<string> Columns, List<string[]> Rows)? BuildActiveTabExport()
        {
            if (_summary == null) return null;
            var cur = AppSettings.CurrencySymbol;

            switch (_activeTab)
            {
                case Tab.Totals:
                    return (
                        "Sales Report - Totals",
                        new (string, string)[]
                        {
                            ("Total Takings", $"{cur} {_summary.GrandTotal:N2}"),
                            ("Bills", _summary.BillCount.ToString("N0")),
                            ("Items Sold", _summary.ItemCount.ToString("N2")),
                            ("Average Sale", $"{cur} {_summary.AverageSale:N2}")
                        },
                        new[] { "Period", "Bills", "Total" },
                        _breakdown.ConvertAll(r => new[] { r.PeriodLabel, r.BillCount.ToString("N0"), r.GrandTotal.ToString("N2") })
                    );

                case Tab.Payment:
                    var rows = new List<string[]>
                    {
                        new[] { "Cash", $"{cur} {_summary.CashTotal:N2}" },
                        new[] { "M-Pesa", $"{cur} {_summary.MpesaTotal:N2}" },
                        new[] { "Card", $"{cur} {_summary.CardTotal:N2}" }
                    };
                    if (_summary.OtherTotal > 0)
                        rows.Add(new[] { "Other", $"{cur} {_summary.OtherTotal:N2}" });

                    return (
                        "Sales Report - By Payment Mode",
                        new (string, string)[]
                        {
                            ("Net of VAT", $"{cur} {_summary.TaxableTotal:N2}"),
                            ($"VAT at {AppSettings.VatPercent:0.##}%", $"{cur} {_summary.VatTotal:N2}")
                        },
                        new[] { "Payment Mode", "Amount" },
                        rows
                    );

                case Tab.TopItems:
                    return (
                        "Sales Report - Top Selling Items",
                        Array.Empty<(string, string)>(),
                        new[] { "Item", "Qty Sold", "Value" },
                        _summary.TopItems.ConvertAll(t => new[] { t.Name, t.Qty.ToString("N2"), t.Value.ToString("N2") })
                    );

                case Tab.Cashier:
                    return (
                        "Sales Report - By Cashier",
                        Array.Empty<(string, string)>(),
                        new[] { "Cashier", "Bills", "Average Sale", "Total" },
                        _cashierRows.ConvertAll(c => new[] { c.CashierName, c.BillCount.ToString("N0"), c.AverageSale.ToString("N2"), c.GrandTotal.ToString("N2") })
                    );

                case Tab.Voided:
                    return (
                        "Sales Report - Voided Sales",
                        new (string, string)[]
                        {
                            ("Total Voided", $"{cur} {_voided.TotalVoided:N2}"),
                            ("Voided Sales", _voided.Count.ToString("N0"))
                        },
                        new[] { "Bill No", "Sale Date", "Voided", "Cashier", "Amount", "Reason" },
                        _voided.Rows.ConvertAll(v => new[]
                        {
                            v.BillNo,
                            v.BillDate?.ToString("dd MMM yyyy HH:mm") ?? "",
                            v.DeletedDate?.ToString("dd MMM yyyy HH:mm") ?? "",
                            v.Operator,
                            v.GrandTotal.ToString("N2"),
                            v.Reason
                        })
                    );

                default:
                    return null;
            }
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var export = BuildActiveTabExport();
            if (export == null)
            {
                MessageBox.Show("Run a report with results before exporting.", "Clidapos");
                return;
            }

            var subtitle = $"{_from:dd MMM yyyy} - {_to:dd MMM yyyy}";
            var fileName = $"{export.Value.Title.Replace(" - ", "_").Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}";
            _exportService.ExportToExcel(export.Value.Title, subtitle, export.Value.Summary, export.Value.Columns, export.Value.Rows, fileName);
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var export = BuildActiveTabExport();
            if (export == null)
            {
                MessageBox.Show("Run a report with results before exporting.", "Clidapos");
                return;
            }

            var subtitle = $"{_from:dd MMM yyyy} - {_to:dd MMM yyyy}";
            var fileName = $"{export.Value.Title.Replace(" - ", "_").Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}";
            _exportService.ExportToPdf(export.Value.Title, subtitle, export.Value.Summary, export.Value.Columns, export.Value.Rows, fileName);
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
