using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SystemLogsView : Window
    {
        private readonly Registration _currentUser;
        private readonly LogService _logService = new();
        private List<LogEntry> _all = new();

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
            if (FromDate.SelectedDate == null || ToDate.SelectedDate == null)
                return;

            var from = FromDate.SelectedDate.Value.Date;
            var to = ToDate.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);

            _all = await _logService.GetLogsAsync(from, to);
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (_all.Count == 0)
            {
                ResultsCard.Visibility = Visibility.Collapsed;
                EmptyText.Visibility = Visibility.Visible;
                return;
            }

            var q = SearchBox.Text.Trim().ToLower();
            var filtered = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(l => l.UserID.ToLower().Contains(q) || l.Operation.ToLower().Contains(q)).ToList();

            if (filtered.Count == 0)
            {
                ResultsCard.Visibility = Visibility.Collapsed;
                EmptyText.Visibility = Visibility.Visible;
                return;
            }

            EmptyText.Visibility = Visibility.Collapsed;
            ResultsCard.Visibility = Visibility.Visible;
            ResultCountText.Text = $"{filtered.Count} ENTRIES";
            LogGrid.ItemsSource = filtered;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

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

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsCard.Visibility != Visibility.Visible)
            {
                MessageBox.Show("Run a report with results before printing.", "Clidapos");
                return;
            }

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(ReportContent, "Clidapos - System Logs");
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