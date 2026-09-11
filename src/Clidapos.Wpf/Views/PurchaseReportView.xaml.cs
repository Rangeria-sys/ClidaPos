using System;
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
    public partial class PurchaseReportView : Window
    {
        private readonly Registration _currentUser;
        private readonly PurchaseService _purchaseService = new();
        private PurchaseReportSummary? _summary;
        private DateTime _from;
        private DateTime _to;

        public PurchaseReportView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await ThisMonth();
        }

        private void SetQuickRangeActive(ToggleButton? active)
        {
            ThisWeekBtn.IsChecked = active == ThisWeekBtn;
            ThisMonthBtn.IsChecked = active == ThisMonthBtn;
            ThisYearBtn.IsChecked = active == ThisYearBtn;
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

        private async System.Threading.Tasks.Task ThisMonth()
        {
            SetQuickRangeActive(ThisMonthBtn);
            var today = DateTime.Today;
            FromDate.SelectedDate = new DateTime(today.Year, today.Month, 1);
            ToDate.SelectedDate = today;
            await RunReport();
        }

        private async void ThisMonth_Click(object sender, RoutedEventArgs e) => await ThisMonth();

        private async void ThisYear_Click(object sender, RoutedEventArgs e)
        {
            SetQuickRangeActive(ThisYearBtn);
            var today = DateTime.Today;
            FromDate.SelectedDate = new DateTime(today.Year, 1, 1);
            ToDate.SelectedDate = today;
            await RunReport();
        }

        private async void RunReport_Click(object sender, RoutedEventArgs e)
        {
            SetQuickRangeActive(null);
            await RunReport();
        }

        private async System.Threading.Tasks.Task RunReport()
        {
            _from = FromDate.SelectedDate ?? DateTime.Today;
            _to = ToDate.SelectedDate ?? DateTime.Today;

            _summary = await _purchaseService.GetPurchaseReportAsync(_from, _to);
            var s = _summary;
            var cur = AppSettings.CurrencySymbol;

            TotalSpentText.Text = $"{cur} {s.TotalSpent:N2}";
            OrderCountText.Text = s.OrderCount.ToString("N0");
            ItemsReceivedText.Text = s.ItemsReceived.ToString("N2");
            VsLastText.Text = "";

            if (s.OutstandingPayables > 0)
            {
                PayablesCard.Visibility = Visibility.Visible;
                PayablesText.Text = $"{cur} {s.OutstandingPayables:N2}";
                PayablesDetailText.Text = s.UnpaidSupplierSummary;
            }
            else
            {
                PayablesCard.Visibility = Visibility.Collapsed;
            }

            BuildSupplierList(s, cur);
            BuildTopItemsList(s, cur);
            BuildPurchasesList(s, cur);
        }

        private void BuildSupplierList(PurchaseReportSummary s, string cur)
        {
            SupplierList.Children.Clear();

            foreach (var sup in s.BySupplier)
            {
                var row = new Grid { Margin = new Thickness(0, 8, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var name = new TextBlock { Text = sup.SupplierName, Foreground = Brushes.Black, FontSize = 14 };
                var orders = new TextBlock { Text = sup.OrderCount.ToString(), Foreground = Brushes.Black, FontSize = 14 };
                var avg = new TextBlock { Text = $"{cur} {sup.AvgOrder:N2}", Foreground = Brushes.Black, FontSize = 14 };
                var last = new TextBlock { Text = sup.LastOrder.ToString("dd MMM yyyy"), Foreground = Brushes.Black, FontSize = 14 };
                var total = new TextBlock { Text = $"{cur} {sup.Total:N2}", Foreground = Brushes.Black, FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right };

                Grid.SetColumn(name, 0);
                Grid.SetColumn(orders, 1);
                Grid.SetColumn(avg, 2);
                Grid.SetColumn(last, 3);
                Grid.SetColumn(total, 4);
                row.Children.Add(name);
                row.Children.Add(orders);
                row.Children.Add(avg);
                row.Children.Add(last);
                row.Children.Add(total);

                SupplierList.Children.Add(row);
            }
        }

        private void BuildTopItemsList(PurchaseReportSummary s, string cur)
        {
            TopItemsList.Children.Clear();

            if (s.TopItems.Count == 0)
            {
                TopItemsList.Children.Add(new TextBlock { Text = "No items purchased in this range.", Foreground = Brushes.Gray, FontSize = 13 });
                return;
            }

            var i = 1;
            foreach (var item in s.TopItems)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                var name = new TextBlock { Text = $"{i}. {item.ProductName}", Foreground = Brushes.Black, FontSize = 14 };
                var detail = new TextBlock
                {
                    Text = $"{item.Qty:N0} units · {cur} {item.Value:N2}", Foreground = Brushes.Gray, FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                row.Children.Add(name);
                row.Children.Add(detail);
                TopItemsList.Children.Add(row);
                i++;
            }
        }

        private void BuildPurchasesList(PurchaseReportSummary s, string cur)
        {
            PurchasesList.Children.Clear();

            if (s.Purchases.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                return;
            }
            EmptyText.Visibility = Visibility.Collapsed;

            foreach (var p in s.Purchases)
            {
                var rowBg = p.IsPaid ? Brushes.Transparent : new SolidColorBrush(Color.FromRgb(0xFC, 0xEA, 0xE6));
                var border = new Border { Background = rowBg, Padding = new Thickness(12, 10, 12, 10) };

                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var date = new TextBlock { Text = p.Date.ToString("dd MMM yyyy"), Foreground = Brushes.Black, FontSize = 14 };
                var invoice = new TextBlock { Text = p.InvoiceNo, Foreground = Brushes.Black, FontSize = 14 };
                var supplier = new TextBlock { Text = p.SupplierName, Foreground = Brushes.Black, FontSize = 14 };

                var pillColor = p.IsPaid ? Color.FromRgb(0x1B, 0x8A, 0x3D) : Color.FromRgb(0xC0, 0x39, 0x2B);
                var pillBg = p.IsPaid ? Color.FromRgb(0xE3, 0xF3, 0xE9) : Color.FromRgb(0xFA, 0xDA, 0xD6);
                var pill = new Border
                {
                    Background = new SolidColorBrush(pillBg), CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(10, 3, 10, 3), HorizontalAlignment = HorizontalAlignment.Left
                };
                pill.Child = new TextBlock
                {
                    Text = p.IsPaid ? "Paid" : "Unpaid", Foreground = new SolidColorBrush(pillColor),
                    FontSize = 12, FontWeight = FontWeights.Bold
                };

                var total = new TextBlock
                {
                    Text = $"{cur} {p.Total:N2}", Foreground = p.IsPaid ? Brushes.Black : new SolidColorBrush(pillColor),
                    FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right
                };

                Grid.SetColumn(date, 0);
                Grid.SetColumn(invoice, 1);
                Grid.SetColumn(supplier, 2);
                Grid.SetColumn(pill, 3);
                Grid.SetColumn(total, 4);
                row.Children.Add(date);
                row.Children.Add(invoice);
                row.Children.Add(supplier);
                row.Children.Add(pill);
                row.Children.Add(total);

                border.Child = row;
                PurchasesList.Children.Add(border);
            }
        }

        // ---------------- PRINT (native, no external app) ----------------
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (_summary == null) return;

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true) return;

            var doc = BuildPrintDocument();
            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
            printDialog.PrintDocument(paginator, "Purchase Report");
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
            doc.Blocks.Add(new Paragraph(new Run($"Purchase Report — {_from:dd MMM yyyy} to {_to:dd MMM yyyy}"))
            {
                FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 8)
            });

            AddHeading("SUMMARY");
            AddRow("Total spent", $"{cur} {s.TotalSpent:N2}");
            AddRow("Orders", s.OrderCount.ToString());
            AddRow("Items received", s.ItemsReceived.ToString("N2"));
            AddRow("Outstanding payables", $"{cur} {s.OutstandingPayables:N2}");

            if (s.BySupplier.Count > 0)
            {
                AddHeading("BY SUPPLIER");
                foreach (var sup in s.BySupplier)
                    AddRow(sup.SupplierName, $"{sup.OrderCount} orders · {cur} {sup.Total:N2}");
            }

            if (s.TopItems.Count > 0)
            {
                AddHeading("TOP PURCHASED ITEMS");
                var i = 1;
                foreach (var item in s.TopItems)
                {
                    AddRow($"{i}. {item.ProductName}", $"{item.Qty:N0} units · {cur} {item.Value:N2}");
                    i++;
                }
            }

            AddHeading("PURCHASES");
            foreach (var p in s.Purchases)
                AddRow($"{p.Date:dd MMM yyyy} · {p.InvoiceNo} · {p.SupplierName} · {(p.IsPaid ? "Paid" : "Unpaid")}", $"{cur} {p.Total:N2}");

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
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(AppSettings.StoreName).FontSize(20).Bold().FontColor("#0E5E5A");
                        col.Item().Text($"Purchase Report — {_from:dd MMM yyyy} to {_to:dd MMM yyyy}").FontSize(11).FontColor("#6A6A70");
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#E4E4E8");
                    });

                    page.Content().PaddingTop(14).Column(col =>
                    {
                        col.Item().PaddingBottom(6).Text("SUMMARY").FontSize(11).Bold().FontColor("#0E5E5A");
                        col.Item().PaddingBottom(3).Row(r => { r.RelativeItem().Text("Total spent"); r.RelativeItem().AlignRight().Text($"{cur} {s.TotalSpent:N2}"); });
                        col.Item().PaddingBottom(3).Row(r => { r.RelativeItem().Text("Orders"); r.RelativeItem().AlignRight().Text(s.OrderCount.ToString()); });
                        col.Item().PaddingBottom(3).Row(r => { r.RelativeItem().Text("Items received"); r.RelativeItem().AlignRight().Text(s.ItemsReceived.ToString("N2")); });
                        col.Item().PaddingBottom(10).Row(r => { r.RelativeItem().Text("Outstanding payables"); r.RelativeItem().AlignRight().Text($"{cur} {s.OutstandingPayables:N2}"); });

                        if (s.BySupplier.Count > 0)
                        {
                            col.Item().PaddingBottom(6).Text("BY SUPPLIER").FontSize(11).Bold().FontColor("#0E5E5A");
                            foreach (var sup in s.BySupplier)
                                col.Item().PaddingBottom(3).Row(r =>
                                {
                                    r.RelativeItem().Text(sup.SupplierName);
                                    r.RelativeItem().AlignRight().Text($"{sup.OrderCount} orders · {cur} {sup.Total:N2}");
                                });
                        }

                        if (s.TopItems.Count > 0)
                        {
                            col.Item().PaddingTop(10).PaddingBottom(6).Text("TOP PURCHASED ITEMS").FontSize(11).Bold().FontColor("#0E5E5A");
                            var i = 1;
                            foreach (var item in s.TopItems)
                            {
                                col.Item().PaddingBottom(3).Row(r =>
                                {
                                    r.RelativeItem().Text($"{i}. {item.ProductName}");
                                    r.RelativeItem().AlignRight().Text($"{item.Qty:N0} units · {cur} {item.Value:N2}");
                                });
                                i++;
                            }
                        }

                        col.Item().PaddingTop(10).PaddingBottom(6).Text("PURCHASES").FontSize(11).Bold().FontColor("#0E5E5A");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(1.3f);
                                cd.RelativeColumn(1.3f);
                                cd.RelativeColumn(1.5f);
                                cd.RelativeColumn();
                                cd.RelativeColumn();
                            });
                            table.Header(h =>
                            {
                                h.Cell().Text("Date").FontSize(9).Bold();
                                h.Cell().Text("Invoice No").FontSize(9).Bold();
                                h.Cell().Text("Supplier").FontSize(9).Bold();
                                h.Cell().Text("Status").FontSize(9).Bold();
                                h.Cell().AlignRight().Text("Total").FontSize(9).Bold();
                            });
                            foreach (var p in s.Purchases)
                            {
                                table.Cell().Text(p.Date.ToString("dd MMM yyyy")).FontSize(9);
                                table.Cell().Text(p.InvoiceNo).FontSize(9);
                                table.Cell().Text(p.SupplierName).FontSize(9);
                                table.Cell().Text(p.IsPaid ? "Paid" : "Unpaid").FontSize(9)
                                    .FontColor(p.IsPaid ? "#1B8A3D" : "#C0392B");
                                table.Cell().AlignRight().Text($"{cur} {p.Total:N2}").FontSize(9);
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
                FileName = $"PurchaseReport_{_from:yyyyMMdd}_{_to:yyyyMMdd}.pdf",
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
