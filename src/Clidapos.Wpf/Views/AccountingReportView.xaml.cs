using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class AccountingReportView : Window
    {
        private readonly Registration _currentUser;
        private readonly AccountingService _accountingService = new();
        private TrialBalanceSummary? _summary;

        public AccountingReportView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            AsOfDate.SelectedDate = DateTime.Today;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _summary = await _accountingService.GetTrialBalanceAsync();
            var s = _summary;
            var cur = AppSettings.CurrencySymbol;

            AssetsText.Text = $"{cur} {s.TotalAssets:N0}";
            LiabilitiesText.Text = $"{cur} {s.TotalLiabilities:N0}";
            EquityText.Text = $"{cur} {s.TotalEquity:N0}";
            IncomeText.Text = $"{cur} {s.TotalIncome:N0}";
            ExpensesText.Text = $"{cur} {s.TotalExpenses:N0}";

            BalancedText.Text = s.BooksBalanced ? "Books balanced" : "Books NOT balanced";
            BalancedPill.Background = s.BooksBalanced
                ? new SolidColorBrush(Color.FromRgb(0x1E, 0x7A, 0x75))
                : new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B));
            BalancedText.Foreground = s.BooksBalanced
                ? new SolidColorBrush(Color.FromRgb(0x9F, 0xE1, 0xCB))
                : Brushes.White;

            BuildTrialBalance(s, cur);
            BuildIncomeStatement(s, cur);
            BuildBalanceSheet(s, cur);

            LedgerAccountPicker.ItemsSource = s.Accounts.Select(a => a.AccountName).OrderBy(n => n).ToList();
        }

        // ---------------- TRIAL BALANCE ----------------
        private static readonly (string Type, string Label, string HeaderBg, string HeaderFg)[] TrialSections =
        {
            ("Asset", "ASSETS", "#E3F3E9", "#1B8A3D"),
            ("Liability", "LIABILITIES", "#FCF3E3", "#B8860B"),
            ("Equity", "EQUITY", "#EFEBF7", "#5B4EA6"),
            ("Income", "INCOME", "#E3F3E9", "#1B8A3D"),
            ("Expense", "EXPENSES", "#FCEAE6", "#C0392B"),
        };

        private void BuildTrialBalance(TrialBalanceSummary s, string cur)
        {
            TrialBalanceList.Children.Clear();

            foreach (var section in TrialSections)
            {
                var accounts = s.Accounts.Where(a => a.AccountType == section.Type).ToList();
                if (accounts.Count == 0) continue;

                var header = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFromString(section.HeaderBg)!,
                    Padding = new Thickness(16, 8, 16, 8)
                };
                header.Child = new TextBlock
                {
                    Text = section.Label,
                    Foreground = (Brush)new BrushConverter().ConvertFromString(section.HeaderFg)!,
                    FontSize = 12, FontWeight = FontWeights.Bold
                };
                TrialBalanceList.Children.Add(header);

                foreach (var a in accounts)
                    TrialBalanceList.Children.Add(BuildTrialRow(a, cur, section.HeaderFg));
            }

            var unclassified = s.Accounts.Where(a => a.AccountType == "(unclassified)").ToList();
            if (unclassified.Count > 0)
            {
                var header = new Border { Background = new SolidColorBrush(Color.FromRgb(0xEC, 0xEC, 0xEE)), Padding = new Thickness(16, 8, 16, 8) };
                header.Child = new TextBlock { Text = "UNCLASSIFIED", Foreground = Brushes.Gray, FontSize = 12, FontWeight = FontWeights.Bold };
                TrialBalanceList.Children.Add(header);
                foreach (var a in unclassified)
                    TrialBalanceList.Children.Add(BuildTrialRow(a, cur, "#6B6B72"));
            }
        }

        private Border BuildTrialRow(AccountBalanceRow a, string cur, string netColorHex)
        {
            var row = new Grid { Margin = new Thickness(16, 10, 16, 10) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var name = new TextBlock { Text = a.AccountName, Foreground = Brushes.Black, FontSize = 14 };
            var debit = new TextBlock { Text = a.TotalDebit.ToString("N2"), Foreground = Brushes.Gray, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Right };
            var credit = new TextBlock { Text = a.TotalCredit.ToString("N2"), Foreground = Brushes.Gray, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Right };
            var net = new TextBlock
            {
                Text = $"{a.NetBalance:N2}", FontWeight = FontWeights.Bold, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = (Brush)new BrushConverter().ConvertFromString(netColorHex)!
            };

            Grid.SetColumn(name, 0);
            Grid.SetColumn(debit, 1);
            Grid.SetColumn(credit, 2);
            Grid.SetColumn(net, 3);
            row.Children.Add(name);
            row.Children.Add(debit);
            row.Children.Add(credit);
            row.Children.Add(net);

            var border = new Border { Child = row, Cursor = Cursors.Hand };
            border.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    var popup = new AccountLedgerDetailPopup(a.AccountName) { Owner = this };
                    popup.ShowDialog();
                }
            };
            return border;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_summary == null) return;
            var q = SearchBox.Text.Trim().ToLower();
            var cur = AppSettings.CurrencySymbol;

            if (string.IsNullOrEmpty(q))
            {
                BuildTrialBalance(_summary, cur);
                return;
            }

            TrialBalanceList.Children.Clear();
            foreach (var a in _summary.Accounts.Where(a => a.AccountName.ToLower().Contains(q)))
                TrialBalanceList.Children.Add(BuildTrialRow(a, cur, "#1C1C1F"));
        }

        // ---------------- GENERAL LEDGER ----------------
        private async void LedgerAccountPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LedgerAccountPicker.SelectedItem is not string accountName) return;

            var entries = await _accountingService.GetLedgerForAccountAsync(accountName);
            GeneralLedgerList.Children.Clear();

            if (entries.Count == 0)
            {
                LedgerEmptyText.Text = "No transactions found for this account.";
                LedgerEmptyText.Visibility = Visibility.Visible;
                return;
            }
            LedgerEmptyText.Visibility = Visibility.Collapsed;

            foreach (var entry in entries)
            {
                var row = new Grid { Margin = new Thickness(0, 8, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var date = new TextBlock { Text = entry.Date?.ToString("dd MMM yyyy") ?? "", Foreground = Brushes.Black, FontSize = 14 };
                var ledgerNo = new TextBlock { Text = entry.LedgerNo ?? "", Foreground = Brushes.Black, FontSize = 14 };
                var label = new TextBlock { Text = string.IsNullOrWhiteSpace(entry.Label) ? (entry.Name ?? "") : entry.Label, Foreground = Brushes.Black, FontSize = 14 };
                var debit = new TextBlock { Text = (entry.Debit ?? 0) == 0 ? "" : (entry.Debit ?? 0).ToString("N2"), Foreground = Brushes.Black, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Right };
                var credit = new TextBlock { Text = (entry.Credit ?? 0) == 0 ? "" : (entry.Credit ?? 0).ToString("N2"), Foreground = Brushes.Black, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Right };

                Grid.SetColumn(date, 0);
                Grid.SetColumn(ledgerNo, 1);
                Grid.SetColumn(label, 2);
                Grid.SetColumn(debit, 3);
                Grid.SetColumn(credit, 4);
                row.Children.Add(date);
                row.Children.Add(ledgerNo);
                row.Children.Add(label);
                row.Children.Add(debit);
                row.Children.Add(credit);

                GeneralLedgerList.Children.Add(row);
            }
        }

        // ---------------- INCOME STATEMENT ----------------
        private void BuildIncomeStatement(TrialBalanceSummary s, string cur)
        {
            IncomeList.Children.Clear();
            foreach (var a in s.Accounts.Where(a => a.AccountType == "Income"))
                IncomeList.Children.Add(BuildStatementRow(a.AccountName, a.NetBalance, cur));
            TotalIncomeText.Text = $"{cur} {s.TotalIncome:N2}";

            ExpenseList.Children.Clear();
            foreach (var a in s.Accounts.Where(a => a.AccountType == "Expense"))
                ExpenseList.Children.Add(BuildStatementRow(a.AccountName, a.NetBalance, cur));
            TotalExpensesText.Text = $"{cur} {s.TotalExpenses:N2}";

            var netIncome = s.TotalIncome - s.TotalExpenses;
            NetIncomeText.Text = $"{cur} {netIncome:N2}";
            NetIncomeText.Foreground = netIncome >= 0
                ? new SolidColorBrush(Color.FromRgb(0x1B, 0x8A, 0x3D))
                : new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B));
        }

        private Grid BuildStatementRow(string name, decimal value, string cur)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            var label = new TextBlock { Text = name, Foreground = Brushes.Black, FontSize = 14 };
            var amount = new TextBlock { Text = $"{cur} {value:N2}", Foreground = Brushes.Black, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Right };
            row.Children.Add(label);
            row.Children.Add(amount);
            return row;
        }

        // ---------------- BALANCE SHEET ----------------
        private void BuildBalanceSheet(TrialBalanceSummary s, string cur)
        {
            BsAssetsList.Children.Clear();
            foreach (var a in s.Accounts.Where(a => a.AccountType == "Asset"))
                BsAssetsList.Children.Add(BuildStatementRow(a.AccountName, a.NetBalance, cur));
            BsTotalAssetsText.Text = $"{cur} {s.TotalAssets:N2}";

            BsLiabilitiesList.Children.Clear();
            foreach (var a in s.Accounts.Where(a => a.AccountType == "Liability"))
                BsLiabilitiesList.Children.Add(BuildStatementRow(a.AccountName, a.NetBalance, cur));
            BsTotalLiabilitiesText.Text = $"{cur} {s.TotalLiabilities:N2}";

            BsEquityList.Children.Clear();
            foreach (var a in s.Accounts.Where(a => a.AccountType == "Equity"))
                BsEquityList.Children.Add(BuildStatementRow(a.AccountName, a.NetBalance, cur));
            BsTotalEquityText.Text = $"{cur} {s.TotalEquity:N2}";

            var liabPlusEquity = s.TotalLiabilities + s.TotalEquity;
            BsLiabPlusEquityText.Text = $"{cur} {liabPlusEquity:N2}";

            var balances = Math.Abs(s.TotalAssets - liabPlusEquity) < 0.01m;
            BsLiabPlusEquityText.Foreground = balances
                ? new SolidColorBrush(Color.FromRgb(0x1B, 0x8A, 0x3D))
                : new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B));
            BsCheckCard.Background = balances
                ? new SolidColorBrush(Color.FromRgb(0xE3, 0xF3, 0xE9))
                : new SolidColorBrush(Color.FromRgb(0xFC, 0xEA, 0xE6));
        }

        // ---------------- TABS ----------------
        private void SetActiveTab(int index)
        {
            TabTrialBtn.IsChecked = index == 0;
            TabLedgerBtn.IsChecked = index == 1;
            TabIncomeBtn.IsChecked = index == 2;
            TabBalanceBtn.IsChecked = index == 3;

            TrialBalanceTab.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
            GeneralLedgerTab.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
            IncomeStatementTab.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
            BalanceSheetTab.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TabTrial_Click(object sender, RoutedEventArgs e) => SetActiveTab(0);
        private void TabLedger_Click(object sender, RoutedEventArgs e) => SetActiveTab(1);
        private void TabIncome_Click(object sender, RoutedEventArgs e) => SetActiveTab(2);
        private void TabBalance_Click(object sender, RoutedEventArgs e) => SetActiveTab(3);

        // ---------------- TOOLBAR ACTIONS ----------------
        private async void NewEntry_Click(object sender, RoutedEventArgs e)
        {
            var popup = new JournalEntryPopup { Owner = this };
            popup.ShowDialog();
            await LoadData();
        }

        private void AllEntries_Click(object sender, RoutedEventArgs e)
        {
            var popup = new JournalEntryListPopup { Owner = this };
            popup.ShowDialog();
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
