using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class ExpenseReportView : Window
    {
        private readonly Registration _currentUser;
        private readonly VoucherService _voucherService = new();
        private ExpenseReportSummary? _summary;
        private DateTime _from;
        private DateTime _to;

        public ExpenseReportView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            var today = DateTime.Today;
            FromDateInput.SelectedDate = new DateTime(today.Year, today.Month, 1);
            ToDateInput.SelectedDate = today;

            Loaded += async (s, e) => await RunReport();
        }

        private void SetQuickRangeActive(ToggleButton? active)
        {
            TodayBtn.IsChecked = active == TodayBtn;
            ThisWeekBtn.IsChecked = active == ThisWeekBtn;
            ThisMonthBtn.IsChecked = active == ThisMonthBtn;
        }

        private async System.Threading.Tasks.Task RunReport()
        {
            _from = FromDateInput.SelectedDate ?? DateTime.Today;
            _to = ToDateInput.SelectedDate ?? DateTime.Today;

            _summary = await _voucherService.GetExpenseReportAsync(_from, _to);
            var s = _summary;
            var cur = AppSettings.CurrencySymbol;

            TotalSpentText.Text = $"{cur} {s.TotalSpent:N2}";
            VoucherCountText.Text = s.VoucherCount.ToString();
            AvgVoucherText.Text = $"{cur} {s.AvgVoucher:N2}";

            if (s.TopParticulars.Count > 0)
            {
                TopCategoryCard.Visibility = Visibility.Visible;
                var top = s.TopParticulars[0];
                var pct = s.TotalSpent == 0 ? 0 : Math.Round(top.Total / s.TotalSpent * 100, 0);
                TopCategoryNameText.Text = top.Particulars;
                TopCategoryDetailText.Text = $"{cur} {top.Total:N2} · {pct:N0}% of total spend";
            }
            else
            {
                TopCategoryCard.Visibility = Visibility.Collapsed;
            }

            BuildPaymentModeList(s, cur);
            BuildCategoryBars(s, cur);
            BuildVoucherList(s, cur);
        }

        private void BuildPaymentModeList(ExpenseReportSummary s, string cur)
        {
            PaymentModeList.Children.Clear();

            foreach (var m in s.ByPaymentMode)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                var mode = new TextBlock { Text = m.Mode, Foreground = Brushes.Black, FontSize = 15 };
                var total = new TextBlock
                {
                    Text = $"{cur} {m.Total:N2}", Foreground = new SolidColorBrush(Color.FromRgb(0x0E, 0x5E, 0x5A)),
                    FontSize = 16, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right
                };
                row.Children.Add(mode);
                row.Children.Add(total);
                PaymentModeList.Children.Add(row);
            }
        }

        private void BuildCategoryBars(ExpenseReportSummary s, string cur)
        {
            CategoryBarsList.Children.Clear();

            var maxValue = s.TopParticulars.Count > 0 ? s.TopParticulars.Max(c => c.Total) : 0;

            foreach (var c in s.TopParticulars)
            {
                var block = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };

                var header = new Grid();
                var name = new TextBlock { Text = c.Particulars, Foreground = Brushes.Black, FontSize = 14 };
                var value = new TextBlock
                {
                    Text = $"{cur} {c.Total:N2}", Foreground = Brushes.Gray, FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                header.Children.Add(name);
                header.Children.Add(value);
                block.Children.Add(header);

                var barTrack = new Border { Background = new SolidColorBrush(Color.FromRgb(0xEC, 0xEC, 0xEE)), CornerRadius = new CornerRadius(6), Height = 10, Margin = new Thickness(0, 6, 0, 0) };
                var fillWidth = maxValue == 0 ? 0 : (double)(c.Total / maxValue);
                var barFill = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x0E, 0x5E, 0x5A)),
                    CornerRadius = new CornerRadius(6),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 420 * Math.Max(0.02, fillWidth)
                };
                var barGrid = new Grid();
                barGrid.Children.Add(barTrack);
                barGrid.Children.Add(barFill);
                block.Children.Add(barGrid);

                CategoryBarsList.Children.Add(block);
            }
        }

        private void BuildVoucherList(ExpenseReportSummary s, string cur)
        {
            VoucherList.Children.Clear();

            if (s.Vouchers.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                return;
            }
            EmptyText.Visibility = Visibility.Collapsed;

            foreach (var v in s.Vouchers)
            {
                var row = new Grid { Margin = new Thickness(12, 10, 12, 10) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var no = new TextBlock { Text = v.VoucherNo, Foreground = Brushes.Black, FontSize = 14 };
                var category = new TextBlock { Text = v.Particulars, Foreground = Brushes.Black, FontSize = 14 };
                var mode = new TextBlock { Text = v.PaymentMode, Foreground = Brushes.Black, FontSize = 14 };
                var date = new TextBlock { Text = v.Date.ToString("dd MMM yyyy"), Foreground = Brushes.Black, FontSize = 14 };
                var amount = new TextBlock
                {
                    Text = $"{cur} {v.GrandTotal:N2}", Foreground = Brushes.Black, FontSize = 14,
                    FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right
                };

                Grid.SetColumn(no, 0);
                Grid.SetColumn(category, 1);
                Grid.SetColumn(mode, 2);
                Grid.SetColumn(date, 3);
                Grid.SetColumn(amount, 4);
                row.Children.Add(no);
                row.Children.Add(category);
                row.Children.Add(mode);
                row.Children.Add(date);
                row.Children.Add(amount);

                var border = new Border { Child = row };
                VoucherList.Children.Add(border);
            }
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
            FromDateInput.SelectedDate = today;
            ToDateInput.SelectedDate = today;
            await RunReport();
        }

        private async void ThisWeek_Click(object sender, RoutedEventArgs e)
        {
            SetQuickRangeActive(ThisWeekBtn);
            var today = DateTime.Today;
            var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            FromDateInput.SelectedDate = today.AddDays(-daysSinceMonday);
            ToDateInput.SelectedDate = today;
            await RunReport();
        }

        private async void ThisMonth_Click(object sender, RoutedEventArgs e)
        {
            SetQuickRangeActive(ThisMonthBtn);
            var today = DateTime.Today;
            FromDateInput.SelectedDate = new DateTime(today.Year, today.Month, 1);
            ToDateInput.SelectedDate = today;
            await RunReport();
        }

        // ---------------- PRINT (native, no external app) ----------------
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (_summary == null) return;

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true) return;

            var doc = BuildPrintDocument();
            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
            printDialog.PrintDocument(paginator, "Expense Report");
        }

        private FlowDocument BuildPrintDocument()
        {
            var s = _summary!;
            var cur = AppSettings.CurrencySymbol;

            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                PagePadding = new Thickness(30)
            };

            void AddHeading(string text)
            {
                doc.Blocks.Add(new Paragraph(new Run(text))
                {
                    FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x0E, 0x5E, 0x5A)),
                    Margin = new Thickness(0, 12, 0, 4)
                });
            }

            void AddRow(string label, string value)
            {
                var p = new Paragraph { Margin = new Thickness(0, 0, 0, 3) };
                p.Inlines.Add(new Run(label));
                p.Inlines.Add(new Run(new string(' ', Math.Max(1, 50 - label.Length - value.Length))));
                p.Inlines.Add(new Run(value) { FontWeight = FontWeights.Bold });
                doc.Blocks.Add(p);
            }

            doc.Blocks.Add(new Paragraph(new Run(AppSettings.StoreName)) { FontSize = 20, FontWeight = FontWeights.Bold });
            doc.Blocks.Add(new Paragraph(new Run($"Expense Report — {_from:dd MMM yyyy} to {_to:dd MMM yyyy}"))
            {
                FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 8)
            });

            AddHeading("SUMMARY");
            AddRow("Total spent", $"{cur} {s.TotalSpent:N2}");
            AddRow("Vouchers", s.VoucherCount.ToString());
            AddRow("Average voucher", $"{cur} {s.AvgVoucher:N2}");

            if (s.ByPaymentMode.Count > 0)
            {
                AddHeading("BY PAYMENT MODE");
                foreach (var m in s.ByPaymentMode)
                    AddRow(m.Mode, $"{cur} {m.Total:N2}");
            }

            if (s.TopParticulars.Count > 0)
            {
                AddHeading("TOP SPENDING CATEGORIES");
                foreach (var c in s.TopParticulars)
                    AddRow(c.Particulars, $"{cur} {c.Total:N2}");
            }

            AddHeading("VOUCHERS IN RANGE");
            foreach (var v in s.Vouchers)
                AddRow($"{v.Date:dd MMM yyyy} · {v.VoucherNo} · {v.Particulars} · {v.PaymentMode}", $"{cur} {v.GrandTotal:N2}");

            return doc;
        }

        // ---------------- DOWNLOAD PDF ----------------
        private void DownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_summary == null) return;

            var s = _summary;
            var cur = AppSettings.CurrencySymbol;

            var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(AppSettings.StoreName).FontSize(20).Bold().FontColor("#0E5E5A");
                        col.Item().Text($"Expense Report — {_from:dd MMM yyyy} to {_to:dd MMM yyyy}").FontSize(11).FontColor("#6A6A70");
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#E4E4E8");
                    });

                    page.Content().PaddingTop(14).Column(col =>
                    {
                        col.Item().PaddingBottom(6).Text("SUMMARY").FontSize(11).Bold().FontColor("#0E5E5A");
                        col.Item().PaddingBottom(3).Row(r => { r.RelativeItem().Text("Total spent"); r.RelativeItem().AlignRight().Text($"{cur} {s.TotalSpent:N2}"); });
                        col.Item().PaddingBottom(3).Row(r => { r.RelativeItem().Text("Vouchers"); r.RelativeItem().AlignRight().Text(s.VoucherCount.ToString()); });
                        col.Item().PaddingBottom(10).Row(r => { r.RelativeItem().Text("Average voucher"); r.RelativeItem().AlignRight().Text($"{cur} {s.AvgVoucher:N2}"); });

                        if (s.ByPaymentMode.Count > 0)
                        {
                            col.Item().PaddingBottom(6).Text("BY PAYMENT MODE").FontSize(11).Bold().FontColor("#0E5E5A");
                            foreach (var m in s.ByPaymentMode)
                                col.Item().PaddingBottom(3).Row(r => { r.RelativeItem().Text(m.Mode); r.RelativeItem().AlignRight().Text($"{cur} {m.Total:N2}"); });
                        }

                        if (s.TopParticulars.Count > 0)
                        {
                            col.Item().PaddingTop(10).PaddingBottom(6).Text("TOP SPENDING CATEGORIES").FontSize(11).Bold().FontColor("#0E5E5A");
                            foreach (var c in s.TopParticulars)
                                col.Item().PaddingBottom(3).Row(r => { r.RelativeItem().Text(c.Particulars); r.RelativeItem().AlignRight().Text($"{cur} {c.Total:N2}"); });
                        }

                        col.Item().PaddingTop(10).PaddingBottom(6).Text("VOUCHERS IN RANGE").FontSize(11).Bold().FontColor("#0E5E5A");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn();
                                cd.RelativeColumn(1.3f);
                                cd.RelativeColumn();
                                cd.RelativeColumn();
                                cd.RelativeColumn();
                            });
                            table.Header(h =>
                            {
                                h.Cell().Text("Voucher No").FontSize(9).Bold();
                                h.Cell().Text("Category").FontSize(9).Bold();
                                h.Cell().Text("Payment Mode").FontSize(9).Bold();
                                h.Cell().Text("Date").FontSize(9).Bold();
                                h.Cell().AlignRight().Text("Amount").FontSize(9).Bold();
                            });
                            foreach (var v in s.Vouchers)
                            {
                                table.Cell().Text(v.VoucherNo).FontSize(9);
                                table.Cell().Text(v.Particulars).FontSize(9);
                                table.Cell().Text(v.PaymentMode).FontSize(9);
                                table.Cell().Text(v.Date.ToString("dd MMM yyyy")).FontSize(9);
                                table.Cell().AlignRight().Text($"{cur} {v.GrandTotal:N2}").FontSize(9);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Generated ").FontSize(8).FontColor("#8A8A92");
                        t.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm")).FontSize(8).FontColor("#8A8A92");
                    });
                });
            }).GeneratePdf();

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"ExpenseReport_{_from:yyyyMMdd}_{_to:yyyyMMdd}.pdf",
                Filter = "PDF Document (*.pdf)|*.pdf",
                AddExtension = true
            };

            if (dlg.ShowDialog() == true)
            {
                System.IO.File.WriteAllBytes(dlg.FileName, pdfBytes);
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                }
                catch
                {
                    // Non-fatal - file is saved either way.
                }
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
