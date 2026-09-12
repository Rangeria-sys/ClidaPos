using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    /// <summary>One entry in the period picker dropdown.</summary>
    public class PeriodPickerItem
    {
        public int PeriodId { get; set; }
        public string Label { get; set; } = "";
    }

    public partial class DailyReportView : Window
    {
        private readonly Registration _currentUser;
        private int _periodId;
        private readonly ReportService _reportService = new();
        private readonly ShiftService _shiftService = new();
        private ShiftSummary? _summary;
        private bool _suppressPickerEvent;

        public DailyReportView(Registration currentUser, int periodId)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _periodId = periodId;
            Loaded += async (s, e) => await LoadPeriodPicker();
        }

        /// <summary>Loads every period ever recorded on this terminal into the
        /// picker, most recent first, then selects whichever one was
        /// originally requested - selecting it fires PeriodPicker_SelectionChanged,
        /// which loads that period's summary, so there's no separate/duplicate
        /// call to load the summary here.</summary>
        private const int CombinedViewSentinel = -1;

        private async System.Threading.Tasks.Task LoadPeriodPicker()
        {
            var periods = await _shiftService.GetAllPeriodsAsync();

            var items = new List<PeriodPickerItem>
            {
                new PeriodPickerItem { PeriodId = CombinedViewSentinel, Label = "Today — All Terminals" }
            };
            foreach (var p in periods)
            {
                items.Add(new PeriodPickerItem
                {
                    PeriodId = p.ID,
                    Label = $"Period {p.ID} — {p.WPStart:dd MMM yyyy, hh:mm tt}"
                });
            }

            _suppressPickerEvent = true;
            PeriodPicker.ItemsSource = items;

            var match = items.Find(i => i.PeriodId == _periodId);
            PeriodPicker.SelectedItem = match ?? (items.Count > 0 ? items[0] : null);
            _suppressPickerEvent = false;

            if (PeriodPicker.SelectedItem is PeriodPickerItem selected)
            {
                _periodId = selected.PeriodId;
                await LoadSelectedSummary();
            }
            else
            {
                MessageBox.Show("No sales period has been recorded yet on this terminal.", "Clidapos");
                Close();
            }
        }

        private async void PeriodPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressPickerEvent) return;
            if (PeriodPicker.SelectedItem is not PeriodPickerItem selected) return;

            _periodId = selected.PeriodId;
            await LoadSelectedSummary();
        }

        private async System.Threading.Tasks.Task LoadSelectedSummary()
        {
            if (_periodId == CombinedViewSentinel)
            {
                _summary = await _reportService.GetCombinedDailySummaryAsync(DateTime.Today);
                CashDrawerCard.Visibility = Visibility.Collapsed;
                RenderSummary(_summary, isCombined: true);
                return;
            }

            _summary = await _reportService.GetPeriodSummaryAsync(_periodId);
            if (_summary == null)
            {
                MessageBox.Show("This period could not be found.", "Clidapos");
                return;
            }
            CashDrawerCard.Visibility = Visibility.Visible;
            RenderSummary(_summary, isCombined: false);
        }

        private void RenderSummary(ShiftSummary s, bool isCombined)
        {
            var cur = AppSettings.CurrencySymbol;

            StoreText.Text = AppSettings.StoreName;

            GrandTotalText.Text = $"{cur} {s.GrandTotal:N2}";
            BillCountText.Text = s.BillCount.ToString();
            ItemCountText.Text = s.ItemCount.ToString("N0");
            AverageText.Text = $"{cur} {s.AverageSale:N2}";

            CashText.Text = $"{cur} {s.CashTotal:N2}";
            MpesaText.Text = $"{cur} {s.MpesaTotal:N2}";
            CardText.Text = $"{cur} {s.CardTotal:N2}";

            DiscountsText.Text = $"{cur} {s.DiscountsTotal:N2}";
            VoidedCountText.Text = s.VoidedSalesCount.ToString();
            VatText.Text = $"{cur} {s.VatTotal:N2}";

            BuildCashierRows(s, cur);
            BuildTopItemsRows(s);

            if (!isCombined)
            {
                OpeningCashText.Text = s.OpeningCash == null ? "Not recorded" : $"{cur} {s.OpeningCash:N2}";
                ExpectedCashText.Text = s.ExpectedCash == null ? "—" : $"{cur} {s.ExpectedCash:N2}";
                ClosingCashText.Text = s.ClosingCash == null ? "Not recorded" : $"{cur} {s.ClosingCash:N2}";
                UpdateVarianceCard(s, cur);
            }

            SignOffText.Text = isCombined
                ? $"Combined totals across all terminals for {DateTime.Today:dd MMM yyyy}."
                : s.IsOpen
                    ? "Period still open."
                    : $"Signed off by {s.CashierName ?? _currentUser.Name.Trim()} · {s.EndedAt:dd MMM yyyy, hh:mm tt}";
        }

        private void BuildCashierRows(ShiftSummary s, string cur)
        {
            CashierList.Children.Clear();

            if (s.CashierBreakdown.Count == 0)
            {
                CashierCard.Visibility = Visibility.Collapsed;
                return;
            }

            foreach (var c in s.CashierBreakdown)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var name = new TextBlock { Text = c.CashierName, Foreground = System.Windows.Media.Brushes.Black, FontWeight = FontWeights.Bold, FontSize = 14 };
                var bills = new TextBlock { Text = c.BillCount.ToString(), Foreground = System.Windows.Media.Brushes.Black, FontSize = 14 };
                var takings = new TextBlock { Text = $"{cur} {c.Takings:N2}", Foreground = System.Windows.Media.Brushes.Black, FontSize = 14 };
                var voids = new TextBlock { Text = c.VoidCount.ToString(), Foreground = System.Windows.Media.Brushes.Black, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Right };

                Grid.SetColumn(name, 0);
                Grid.SetColumn(bills, 1);
                Grid.SetColumn(takings, 2);
                Grid.SetColumn(voids, 3);

                row.Children.Add(name);
                row.Children.Add(bills);
                row.Children.Add(takings);
                row.Children.Add(voids);

                CashierList.Children.Add(row);
            }
        }

        private void BuildTopItemsRows(ShiftSummary s)
        {
            TopItemsList.Children.Clear();

            if (s.TopItems.Count == 0)
            {
                TopItemsList.Children.Add(new TextBlock
                {
                    Text = "No items sold yet.",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontSize = 13
                });
                return;
            }

            var i = 1;
            foreach (var item in s.TopItems)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var name = new TextBlock { Text = $"{i}. {item.Name}", Foreground = System.Windows.Media.Brushes.Black, FontSize = 14 };
                var qty = new TextBlock { Text = $"{item.Qty:N0} sold", Foreground = System.Windows.Media.Brushes.Gray, FontSize = 13, HorizontalAlignment = HorizontalAlignment.Right };

                Grid.SetColumn(name, 0);
                Grid.SetColumn(qty, 1);
                row.Children.Add(name);
                row.Children.Add(qty);

                TopItemsList.Children.Add(row);
                i++;
            }
        }

        private void UpdateVarianceCard(ShiftSummary s, string cur)
        {
            if (s.CashVariance == null)
            {
                VarianceCard.Visibility = Visibility.Collapsed;
                return;
            }

            var brush = s.CashVariance == 0 || s.CashVariance > 0
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1B, 0x8A, 0x3D))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC0, 0x39, 0x2B));

            VarianceLabel.Text = s.CashVariance == 0 ? "Variance — exact match" : s.CashVariance > 0 ? "Variance — over" : "Variance — SHORT";
            VarianceLabel.Foreground = brush;
            VarianceText.Text = $"{cur} {Math.Abs(s.CashVariance.Value):N2}";
            VarianceText.Foreground = brush;
        }

        private byte[] GenerateCurrentPdf()
        {
            return new ExportService().GenerateDayEndPdfBytes(
                AppSettings.StoreName,
                $"Period {_periodId}",
                _summary!.StartedAt.ToString("dd MMM yyyy, hh:mm tt"),
                (_summary.EndedAt ?? DateTime.Now).ToString("dd MMM yyyy, hh:mm tt"),
                AppSettings.CurrencySymbol,
                _summary,
                _summary.CashierName ?? _currentUser.Name.Trim(),
                (_summary.EndedAt ?? DateTime.Now).ToString("dd MMM yyyy, hh:mm tt"));
        }

        private void DownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_summary == null) return;

            var pdfBytes = GenerateCurrentPdf();

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"DayEnd_Period{_periodId}.pdf",
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

        /// <summary>Sends the report straight to the default printer via
        /// Windows' own "print" verb on the PDF file's default handler -
        /// the standard, reliable way to print a file from .NET without
        /// building a custom print pipeline. Saved to a temp file first,
        /// since the print verb needs an actual file on disk to act on.</summary>
        /// <summary>Sends the report straight to the printer using WPF's own
        /// print pipeline - a FlowDocument built directly from the same data
        /// on screen, printed via PrintDialog.PrintDocument. Nothing external
        /// is launched at any point; this stays entirely inside the app.</summary>
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (_summary == null) return;

            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() != true) return;

            var doc = BuildPrintDocument();
            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
            printDialog.PrintDocument(paginator, $"Day End Summary - Period {_periodId}");
        }

        private FlowDocument BuildPrintDocument()
        {
            var s = _summary!;
            var cur = AppSettings.CurrencySymbol;

            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                PagePadding = new Thickness(30),
                ColumnWidth = double.PositiveInfinity
            };

            void AddHeading(string text, double size = 14)
            {
                doc.Blocks.Add(new Paragraph(new Run(text))
                {
                    FontSize = size,
                    FontWeight = FontWeights.Bold,
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

            doc.Blocks.Add(new Paragraph(new Run(AppSettings.StoreName))
            {
                FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 2)
            });
            doc.Blocks.Add(new Paragraph(new Run($"Day End Summary — Period {_periodId}"))
            {
                FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 2)
            });
            doc.Blocks.Add(new Paragraph(new Run(
                $"Opened {s.StartedAt:dd MMM yyyy, hh:mm tt}" +
                (s.IsOpen ? " (still open)" : $" · Closed {s.EndedAt:dd MMM yyyy, hh:mm tt}")))
            {
                FontSize = 10, Foreground = Brushes.Gray
            });

            AddHeading("TOTAL TAKINGS");
            doc.Blocks.Add(new Paragraph(new Run($"{cur} {s.GrandTotal:N2}"))
            {
                FontSize = 22, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x0E, 0x5E, 0x5A))
            });
            AddRow("Sales (bills)", s.BillCount.ToString());
            AddRow("Items sold", s.ItemCount.ToString("N0"));
            AddRow("Average sale", $"{cur} {s.AverageSale:N2}");

            AddHeading("BY PAYMENT METHOD");
            AddRow("Cash", $"{cur} {s.CashTotal:N2}");
            AddRow("M-Pesa", $"{cur} {s.MpesaTotal:N2}");
            AddRow("Card", $"{cur} {s.CardTotal:N2}");

            AddHeading("ADJUSTMENTS");
            AddRow("Discounts given", $"{cur} {s.DiscountsTotal:N2}");
            AddRow("Voided sales", s.VoidedSalesCount.ToString());
            AddRow("Tax collected (VAT)", $"{cur} {s.VatTotal:N2}");

            if (s.CashierBreakdown.Count > 0)
            {
                AddHeading("CASHIER BREAKDOWN");
                foreach (var c in s.CashierBreakdown)
                    AddRow(c.CashierName, $"{c.BillCount} bills · {cur} {c.Takings:N2} · {c.VoidCount} voids");
            }

            if (s.TopItems.Count > 0)
            {
                AddHeading("TOP ITEMS SOLD");
                var i = 1;
                foreach (var item in s.TopItems)
                {
                    AddRow($"{i}. {item.Name}", $"{item.Qty:N0} sold");
                    i++;
                }
            }

            AddHeading("CASH DRAWER RECONCILIATION");
            AddRow("Opening float", s.OpeningCash == null ? "Not recorded" : $"{cur} {s.OpeningCash:N2}");
            AddRow("Expected in drawer", s.ExpectedCash == null ? "—" : $"{cur} {s.ExpectedCash:N2}");
            AddRow("Counted cash", s.ClosingCash == null ? "Not recorded" : $"{cur} {s.ClosingCash:N2}");
            if (s.CashVariance != null)
            {
                var label = s.CashVariance == 0 ? "Variance (exact match)" : s.CashVariance > 0 ? "Variance (over)" : "Variance (SHORT)";
                AddRow(label, $"{cur} {Math.Abs(s.CashVariance.Value):N2}");
            }

            doc.Blocks.Add(new Paragraph(new Run(
                s.IsOpen ? "Period still open." : $"Signed off by {s.CashierName ?? _currentUser.Name.Trim()} · {s.EndedAt:dd MMM yyyy, hh:mm tt}"))
            {
                FontSize = 10, Foreground = Brushes.Gray, Margin = new Thickness(0, 16, 0, 0)
            });

            return doc;
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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            new FrontOfficeHubView(_currentUser).Show();
            Close();
        }
    }
}
