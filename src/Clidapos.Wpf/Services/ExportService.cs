using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Clidapos.Wpf.Services
{
    /// <summary>
    /// Shared Excel/PDF export used by every report screen (Sales, Stock, Purchase,
    /// Expense, Accounting). Each screen builds a simple table (headers + string rows)
    /// plus an optional key/value summary block, and hands it to one of these two
    /// methods. Handles the Save dialog and opens the file afterwards.
    /// </summary>
    public class ExportService
    {
        /// <summary>Exports a table (with an optional summary block above it) to .xlsx. Returns the saved path, or null if the user cancelled.</summary>
        public string? ExportToExcel(
            string reportTitle,
            string subtitle,
            IReadOnlyList<(string Label, string Value)> summary,
            IReadOnlyList<string> columns,
            IEnumerable<string[]> rows,
            string suggestedFileName)
        {
            var path = PromptForPath(suggestedFileName, "Excel Workbook (*.xlsx)|*.xlsx");
            if (path == null) return null;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(SafeSheetName(reportTitle));

            var row = 1;
            ws.Cell(row, 1).Value = reportTitle;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 16;
            row++;

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                ws.Cell(row, 1).Value = subtitle;
                ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#6A6A70");
                row++;
            }

            row++;

            foreach (var (label, value) in summary)
            {
                ws.Cell(row, 1).Value = label;
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 2).Value = value;
                row++;
            }

            if (summary.Count > 0)
                row++;

            var headerRow = row;
            for (var c = 0; c < columns.Count; c++)
            {
                var cell = ws.Cell(headerRow, c + 1);
                cell.Value = columns[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3A123");
                cell.Style.Font.FontColor = XLColor.White;
            }
            row++;

            foreach (var r in rows)
            {
                for (var c = 0; c < r.Length; c++)
                    ws.Cell(row, c + 1).Value = r[c];
                row++;
            }

            if (columns.Count > 0)
                ws.Range(headerRow, 1, Math.Max(headerRow, row - 1), columns.Count).SetAutoFilter();

            ws.Columns().AdjustToContents();
            wb.SaveAs(path);

            TryOpen(path);
            return path;
        }

        /// <summary>Builds the day-end closing report as PDF bytes, for emailing
        /// to the owner - matches the printout layout: takings, payment
        /// breakdown, cashier breakdown, cash drawer reconciliation with the
        /// variance called out, signed off by.</summary>
        public byte[] GenerateDayEndPdfBytes(
            string storeName,
            string periodLabel,
            string openedAt,
            string closedAt,
            string currency,
            ShiftSummary s,
            string closedByName,
            string closedAtDisplay)
        {
            return QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(storeName).FontSize(20).Bold().FontColor("#0E5E5A");
                        col.Item().Text($"Day End Summary — {periodLabel}").FontSize(11).FontColor("#6A6A70");
                        col.Item().Text($"Opened {openedAt} · Closed {closedAt}").FontSize(9).FontColor("#8A8A92");
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#E4E4E8");
                    });

                    page.Content().PaddingTop(14).Column(col =>
                    {
                        col.Item().PaddingBottom(14).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("TOTAL TAKINGS").FontSize(9).FontColor("#8A8A92").Bold();
                                c.Item().Text($"{currency} {s.GrandTotal:N2}").FontSize(24).Bold().FontColor("#0E5E5A");
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignRight().Text($"Sales (bills): {s.BillCount}").FontSize(9);
                                c.Item().AlignRight().Text($"Items sold: {s.ItemCount:N0}").FontSize(9);
                                c.Item().AlignRight().Text($"Average sale: {currency} {s.AverageSale:N2}").FontSize(9);
                            });
                        });

                        col.Item().PaddingBottom(6).Text("BY PAYMENT METHOD").FontSize(10).Bold().FontColor("#0E5E5A");
                        col.Item().PaddingBottom(4).Row(r => { r.RelativeItem().Text("Cash"); r.RelativeItem().AlignRight().Text($"{currency} {s.CashTotal:N2}"); });
                        col.Item().PaddingBottom(4).Row(r => { r.RelativeItem().Text("M-Pesa"); r.RelativeItem().AlignRight().Text($"{currency} {s.MpesaTotal:N2}"); });
                        col.Item().PaddingBottom(4).Row(r => { r.RelativeItem().Text("Card"); r.RelativeItem().AlignRight().Text($"{currency} {s.CardTotal:N2}"); });
                        if (s.OtherTotal > 0)
                            col.Item().PaddingBottom(4).Row(r => { r.RelativeItem().Text("Other"); r.RelativeItem().AlignRight().Text($"{currency} {s.OtherTotal:N2}"); });
                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#E4E4E8");

                        if (s.CashierBreakdown.Count > 0)
                        {
                            col.Item().PaddingBottom(6).Text("CASHIER BREAKDOWN").FontSize(10).Bold().FontColor("#0E5E5A");
                            col.Item().PaddingBottom(10).Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(2);
                                    cd.RelativeColumn();
                                    cd.RelativeColumn();
                                    cd.RelativeColumn();
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Text("Cashier").FontSize(9).Bold();
                                    h.Cell().AlignRight().Text("Bills").FontSize(9).Bold();
                                    h.Cell().AlignRight().Text("Takings").FontSize(9).Bold();
                                    h.Cell().AlignRight().Text("Voids").FontSize(9).Bold();
                                });
                                foreach (var c in s.CashierBreakdown)
                                {
                                    table.Cell().Text(c.CashierName).FontSize(9);
                                    table.Cell().AlignRight().Text(c.BillCount.ToString()).FontSize(9);
                                    table.Cell().AlignRight().Text($"{currency} {c.Takings:N2}").FontSize(9);
                                    table.Cell().AlignRight().Text(c.VoidCount.ToString()).FontSize(9);
                                }
                            });
                        }

                        col.Item().PaddingBottom(6).Text("ADJUSTMENTS").FontSize(10).Bold().FontColor("#0E5E5A");
                        col.Item().PaddingBottom(4).Row(r => { r.RelativeItem().Text("Discounts given"); r.RelativeItem().AlignRight().Text($"{currency} {s.DiscountsTotal:N2}"); });
                        col.Item().PaddingBottom(4).Row(r => { r.RelativeItem().Text("Voided sales"); r.RelativeItem().AlignRight().Text(s.VoidedSalesCount.ToString()); });
                        col.Item().PaddingBottom(10).Row(r => { r.RelativeItem().Text("Tax collected (VAT)"); r.RelativeItem().AlignRight().Text($"{currency} {s.VatTotal:N2}"); });

                        col.Item().PaddingBottom(6).Text("CASH DRAWER RECONCILIATION").FontSize(10).Bold().FontColor("#0E5E5A");
                        col.Item().PaddingBottom(4).Row(r => { r.RelativeItem().Text("Opening float"); r.RelativeItem().AlignRight().Text(s.OpeningCash == null ? "Not recorded" : $"{currency} {s.OpeningCash:N2}"); });
                        col.Item().PaddingBottom(4).Row(r => { r.RelativeItem().Text("Expected in drawer"); r.RelativeItem().AlignRight().Text(s.ExpectedCash == null ? "—" : $"{currency} {s.ExpectedCash:N2}"); });
                        col.Item().PaddingBottom(8).Row(r => { r.RelativeItem().Text("Counted cash"); r.RelativeItem().AlignRight().Text(s.ClosingCash == null ? "Not recorded" : $"{currency} {s.ClosingCash:N2}"); });

                        if (s.CashVariance != null)
                        {
                            var varianceLabel = s.CashVariance == 0 ? "Variance — exact match" : s.CashVariance > 0 ? "Variance — over" : "Variance — SHORT";
                            var varianceColor = s.CashVariance == 0 ? "#1B8A3D" : s.CashVariance > 0 ? "#1B8A3D" : "#C0392B";
                            col.Item().Background("#FCF3E3").Padding(10).Row(r =>
                            {
                                r.RelativeItem().Text(varianceLabel).FontSize(10).Bold().FontColor(varianceColor);
                                r.RelativeItem().AlignRight().Text($"{currency} {Math.Abs(s.CashVariance.Value):N2}").FontSize(11).Bold().FontColor(varianceColor);
                            });
                        }

                        if (s.TopItems.Count > 0)
                        {
                            col.Item().PaddingTop(14).PaddingBottom(6).Text("TOP ITEMS SOLD").FontSize(10).Bold().FontColor("#0E5E5A");
                            var i = 1;
                            foreach (var item in s.TopItems)
                            {
                                col.Item().PaddingBottom(3).Row(r =>
                                {
                                    r.RelativeItem().Text($"{i}. {item.Name}").FontSize(9);
                                    r.RelativeItem().AlignRight().Text($"{item.Qty:N0} sold").FontSize(9);
                                });
                                i++;
                            }
                        }

                        col.Item().PaddingTop(16).LineHorizontal(1).LineColor("#E4E4E8");
                        col.Item().PaddingTop(8).Text($"Signed off by {closedByName} · {closedAtDisplay}").FontSize(9).FontColor("#8A8A92");
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Generated ").FontSize(8).FontColor("#8A8A92");
                        t.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm")).FontSize(8).FontColor("#8A8A92");
                        t.Span("  •  Clida POS").FontSize(8).FontColor("#8A8A92");
                    });
                });
            }).GeneratePdf();
        }

        /// <summary>Exports a table (with an optional summary block above it) to .pdf. Returns the saved path, or null if the user cancelled.</summary>
        public string? ExportToPdf(
            string reportTitle,
            string subtitle,
            IReadOnlyList<(string Label, string Value)> summary,
            IReadOnlyList<string> columns,
            IEnumerable<string[]> rows,
            string suggestedFileName)
        {
            var path = PromptForPath(suggestedFileName, "PDF Document (*.pdf)|*.pdf");
            if (path == null) return null;

            var rowList = rows.ToList();

            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(reportTitle).FontSize(18).Bold().FontColor("#1E1E24");
                        if (!string.IsNullOrWhiteSpace(subtitle))
                            col.Item().Text(subtitle).FontSize(10).FontColor("#6A6A70");
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#E4E4E8");
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        if (summary.Count > 0)
                        {
                            col.Item().PaddingBottom(10).Row(sumRow =>
                            {
                                foreach (var (label, value) in summary)
                                {
                                    sumRow.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text(label).FontSize(8).FontColor("#8A8A92");
                                        c.Item().Text(value).FontSize(13).Bold();
                                    });
                                }
                            });
                        }

                        if (columns.Count > 0)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    for (var i = 0; i < columns.Count; i++)
                                        cd.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    foreach (var c in columns)
                                    {
                                        header.Cell().Background("#F3A123").Padding(5)
                                            .Text(c).FontColor(Colors.White).Bold().FontSize(9);
                                    }
                                });

                                var alt = false;
                                foreach (var r in rowList)
                                {
                                    var bg = alt ? "#FAFAFB" : "#FFFFFF";
                                    foreach (var cell in r)
                                    {
                                        table.Cell().Background(bg).Padding(5)
                                            .Text(cell ?? "").FontSize(9);
                                    }
                                    alt = !alt;
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Generated ").FontSize(8).FontColor("#8A8A92");
                        t.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm")).FontSize(8).FontColor("#8A8A92");
                        t.Span("  •  Page ").FontSize(8).FontColor("#8A8A92");
                        t.CurrentPageNumber().FontSize(8).FontColor("#8A8A92");
                        t.Span(" of ").FontSize(8).FontColor("#8A8A92");
                        t.TotalPages().FontSize(8).FontColor("#8A8A92");
                    });
                });
            }).GeneratePdf(path);

            TryOpen(path);
            return path;
        }

        private static string? PromptForPath(string suggestedFileName, string filter)
        {
            var dlg = new SaveFileDialog
            {
                FileName = suggestedFileName,
                Filter = filter,
                AddExtension = true
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        private static void TryOpen(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch
            {
                // Non-fatal - the file is saved either way, just couldn't auto-open it.
            }
        }

        private static string SafeSheetName(string name)
        {
            var invalid = new[] { '\\', '/', '?', '*', '[', ']', ':' };
            var clean = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            return clean.Length > 31 ? clean[..31] : (clean.Length == 0 ? "Report" : clean);
        }
    }
}
