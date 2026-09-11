using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SystemLogsView : Window
    {
        private readonly Registration _currentUser;
        private readonly LogService _logService = new();
        private SystemLogReportSummary? _summary;
        private LogCategory? _activeCategoryFilter; // null = all
        private bool _showLogins;

        public SystemLogsView(Registration currentUser)
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
            if (FromDate.SelectedDate == null || ToDate.SelectedDate == null) return;

            _summary = await _logService.GetSystemLogReportAsync(FromDate.SelectedDate.Value, ToDate.SelectedDate.Value);
            _showLogins = false;
            RenderList();
        }

        private void RenderList()
        {
            if (_summary == null) return;

            var userQuery = UserFilterBox.Text.Trim().ToLower();

            IEnumerable<LogRow> baseEvents = _showLogins
                ? _summary.Events.Concat(_summary.LoginEvents)
                : _summary.Events;

            if (_activeCategoryFilter != null)
                baseEvents = baseEvents.Where(r => r.Category == _activeCategoryFilter.Value);

            if (!string.IsNullOrEmpty(userQuery))
                baseEvents = baseEvents.Where(r => r.UserID.ToLower().Contains(userQuery) || r.UserName.ToLower().Contains(userQuery));

            var rows = baseEvents.OrderByDescending(r => r.Date).ToList();

            EventCountRun.Text = $"{rows.Count} EVENTS";
            var hiddenCount = _showLogins ? 0 : _summary.LoginEvents.Count;
            HiddenLoginsRun.Text = hiddenCount > 0 ? $"{hiddenCount} logins hidden" : "";
            ShowLoginsBtn.Visibility = hiddenCount > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (_summary.Anomalies.Count > 0)
            {
                AnomalyBanner.Visibility = Visibility.Visible;
                var n = _summary.Anomalies.Count;
                AnomalyCountRun.Text = $"{n} anomal{(n == 1 ? "y" : "ies")} flagged";
            }
            else
            {
                AnomalyBanner.Visibility = Visibility.Collapsed;
            }

            EventsList.Children.Clear();

            if (rows.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                return;
            }
            EmptyText.Visibility = Visibility.Collapsed;

            string? lastDateLabel = null;
            foreach (var row in rows)
            {
                var dateLabel = row.Date.ToString("dd MMM yyyy").ToUpper();
                if (dateLabel != lastDateLabel)
                {
                    var header = new Border { Background = new SolidColorBrush(Color.FromRgb(0xEC, 0xEC, 0xEE)), Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(0, lastDateLabel == null ? 0 : 10, 0, 0) };
                    header.Child = new TextBlock { Text = dateLabel, Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x6B, 0x72)), FontSize = 12, FontWeight = FontWeights.Bold };
                    EventsList.Children.Add(header);
                    lastDateLabel = dateLabel;
                }

                EventsList.Children.Add(BuildEventRow(row));
            }
        }

        private Border BuildEventRow(LogRow row)
        {
            var bg = row.IsAnomaly
                ? new SolidColorBrush(Color.FromRgb(0xFC, 0xE6, 0xE3))
                : Brushes.White;
            var textColor = row.IsAnomaly
                ? new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B))
                : Brushes.Black;

            var grid = new Grid { Margin = new Thickness(16, 10, 16, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var time = new TextBlock { Text = row.Date.ToString("hh:mm:ss tt"), Foreground = Brushes.Gray, FontSize = 13 };
            var text = new TextBlock
            {
                Foreground = textColor, FontSize = 14,
                FontWeight = row.IsAnomaly ? FontWeights.Bold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap
            };
            text.Inlines.Add(new Run(row.UserName) { FontWeight = FontWeights.Bold });
            text.Inlines.Add(new Run($" — {row.Operation}"));

            Grid.SetColumn(time, 0);
            Grid.SetColumn(text, 1);
            grid.Children.Add(time);
            grid.Children.Add(text);

            return new Border { Background = bg, Child = grid };
        }

        // ---------------- FILTER PILLS ----------------
        private void SetActiveFilter(LogCategory? category, ToggleButton active)
        {
            _activeCategoryFilter = category;
            FilterAllBtn.IsChecked = active == FilterAllBtn;
            FilterWorkBtn.IsChecked = active == FilterWorkBtn;
            FilterSalesBtn.IsChecked = active == FilterSalesBtn;
            FilterSettingsBtn.IsChecked = active == FilterSettingsBtn;
            FilterLoginsBtn.IsChecked = active == FilterLoginsBtn;
            RenderList();
        }

        private void FilterAll_Click(object sender, RoutedEventArgs e) => SetActiveFilter(null, FilterAllBtn);
        private void FilterWork_Click(object sender, RoutedEventArgs e) => SetActiveFilter(LogCategory.WorkPeriod, FilterWorkBtn);
        private void FilterSales_Click(object sender, RoutedEventArgs e) => SetActiveFilter(LogCategory.SalesVoids, FilterSalesBtn);
        private void FilterSettings_Click(object sender, RoutedEventArgs e) => SetActiveFilter(LogCategory.Settings, FilterSettingsBtn);

        private void FilterLogins_Click(object sender, RoutedEventArgs e)
        {
            _showLogins = true;
            SetActiveFilter(LogCategory.Login, FilterLoginsBtn);
        }

        private void ShowLogins_Click(object sender, RoutedEventArgs e)
        {
            _showLogins = true;
            RenderList();
        }

        private void UserFilterBox_TextChanged(object sender, TextChangedEventArgs e) => RenderList();

        // ---------------- DATE RANGE ----------------
        private void SetQuickRangeActive(ToggleButton? active)
        {
            TodayBtn.IsChecked = active == TodayBtn;
            ThisWeekBtn.IsChecked = active == ThisWeekBtn;
        }

        private async void RunReport_Click(object sender, RoutedEventArgs e)
        {
            SetQuickRangeActive(null);
            await RunReport();
        }

        private async void Today_Click(object sender, RoutedEventArgs e)
        {
            SetQuickRangeActive(TodayBtn);
            var today = DateTime.Today;
            FromDate.SelectedDate = today;
            ToDate.SelectedDate = today;
            await RunReport();
        }

        private async void ThisWeek_Click(object sender, RoutedEventArgs e)
        {
            SetQuickRangeActive(ThisWeekBtn);
            var today = DateTime.Today;
            var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            FromDate.SelectedDate = today.AddDays(-daysSinceMonday);
            ToDate.SelectedDate = today;
            await RunReport();
        }

        // ---------------- PRINT ----------------
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(EventsList, "Clidapos - System Logs");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            new BackOfficeView(_currentUser).Show();
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            new BackOfficeView(_currentUser).Show();
            Close();
        }
    }
}
