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
