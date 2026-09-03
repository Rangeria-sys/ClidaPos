using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class ReceiptService
    {
        private readonly SaleService _saleService = new();

        /// <summary>Prints a receipt for an existing, already-saved sale (looked up
        /// by its SaleBill.Id). Used from Sales History.</summary>
        public async Task PrintReceiptAsync(int saleBillId)
        {
            using var db = new ClidaposDbContext();

            var bill = await db.Set<SaleBill>().FirstOrDefaultAsync(b => b.Id == saleBillId);
            if (bill == null)
            {
                MessageBox.Show("That sale could not be found.", "Clidapos");
                return;
            }

            // Reuses the same lookup SalesHistoryView already relies on to show
            // receipt items on screen, rather than guessing at the join field here.
            var items = await _saleService.GetSaleItemsAsync(saleBillId);
            await PrintAsync(bill, items);
        }

        /// <summary>Prints a receipt right after a new sale is completed. Looks
        /// the bill up by its BillNo, since that's what SaveSaleAsync's result
        /// exposes - safer than assuming its exact database Id is available too.</summary>
        public async Task PrintReceiptForNewSaleAsync(string billNo)
        {
            using var db = new ClidaposDbContext();

            var bill = await db.Set<SaleBill>().FirstOrDefaultAsync(b => b.BillNo != null && b.BillNo.Trim() == billNo.Trim());
            if (bill == null)
            {
                MessageBox.Show("That sale could not be found for printing.", "Clidapos");
                return;
            }

            var items = await _saleService.GetSaleItemsAsync(bill.Id);
            await PrintAsync(bill, items);
        }

        private async Task PrintAsync(SaleBill bill, List<SaleItem> items)
        {
            using var db = new ClidaposDbContext();

            var hotel = await db.Set<Hotel>().FirstOrDefaultAsync();
            var terminal = await db.Set<TerminalSetting>().FirstOrDefaultAsync();

            var document = BuildReceiptDocument(bill, items, hotel);

            var printDialog = new PrintDialog();
            var printerName = terminal?.PrinterName?.Trim();

            if (!string.IsNullOrWhiteSpace(printerName))
            {
                try
                {
                    var server = new LocalPrintServer();
                    printDialog.PrintQueue = server.GetPrintQueue(printerName);
                }
                catch
                {
                    // Configured printer not found/reachable - fall back to letting
                    // the user pick one instead of failing silently.
                    if (printDialog.ShowDialog() != true) return;
                }
            }
            else
            {
                if (printDialog.ShowDialog() != true) return;
            }

            IDocumentPaginatorSource idpSource = document;
            printDialog.PrintDocument(idpSource.DocumentPaginator, $"Receipt {bill.BillNo?.Trim()}");
        }

        private FlowDocument BuildReceiptDocument(SaleBill bill, List<SaleItem> items, Hotel? hotel)
        {
            var doc = new FlowDocument
            {
                PageWidth = 320, // narrow, receipt-style layout regardless of the physical printer's page size
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                PagePadding = new Thickness(10)
            };

            void AddCentered(string text, double size = 12, bool bold = false)
            {
                var p = new Paragraph(new Run(text))
                {
                    TextAlignment = TextAlignment.Center,
                    FontSize = size,
                    FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                    Margin = new Thickness(0, 1, 0, 1)
                };
                doc.Blocks.Add(p);
            }

            void AddDivider()
            {
                doc.Blocks.Add(new Paragraph(new Run(new string('-', 40)))
                {
                    Margin = new Thickness(0, 4, 0, 4)
                });
            }

            void AddRow(string left, string right, bool bold = false)
            {
                var p = new Paragraph { Margin = new Thickness(0, 1, 0, 1) };
                var leftRun = new Run(left) { FontWeight = bold ? FontWeights.Bold : FontWeights.Normal };
                var rightRun = new Run(right) { FontWeight = bold ? FontWeights.Bold : FontWeights.Normal };
                p.Inlines.Add(leftRun);
                p.Inlines.Add(new Run(new string(' ', Math.Max(1, 40 - left.Length - right.Length))));
                p.Inlines.Add(rightRun);
                doc.Blocks.Add(p);
            }

            // ---------------- HEADER ----------------
            AddCentered(hotel?.HotelName?.Trim() ?? "CLIDAPOS", 16, true);
            if (!string.IsNullOrWhiteSpace(hotel?.AddressLine1)) AddCentered(hotel.AddressLine1.Trim());
            if (!string.IsNullOrWhiteSpace(hotel?.AddressLine2)) AddCentered(hotel.AddressLine2.Trim());
            if (!string.IsNullOrWhiteSpace(hotel?.ContactNo)) AddCentered($"Tel: {hotel.ContactNo.Trim()}");
            if (!string.IsNullOrWhiteSpace(hotel?.TIN)) AddCentered($"PIN: {hotel.TIN.Trim()}");
            AddDivider();

            // ---------------- BILL INFO ----------------
            AddRow("Bill No:", bill.BillNo?.Trim() ?? "");
            AddRow("Date:", bill.BillDate.ToString("dd MMM yyyy, hh:mm tt"));
            AddRow("Cashier:", bill.Operator?.Trim() ?? "");
            if (!string.IsNullOrWhiteSpace(bill.CustomerName))
                AddRow("Customer:", bill.CustomerName.Trim());
            AddDivider();

            // ---------------- ITEMS ----------------
            foreach (var item in items)
            {
                doc.Blocks.Add(new Paragraph(new Run(item.Dish?.Trim() ?? ""))
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    FontWeight = FontWeights.Bold
                });
                AddRow($"  {item.Quantity:N2} x {item.Rate:N2}", $"{item.TotalAmount:N2}");
            }
            AddDivider();

            // ---------------- TOTALS ----------------
            AddRow("Subtotal:", $"{bill.SubTotal:N2}");
            if (bill.TADiscountAmt > 0)
                AddRow($"Discount ({bill.TADiscountPer:N1}%):", $"-{bill.TADiscountAmt:N2}");
            if (bill.TotalTaxAmount > 0)
                AddRow("VAT:", $"{bill.TotalTaxAmount:N2}");
            AddRow("TOTAL:", $"{bill.GrandTotal:N2}", bold: true);
            AddDivider();

            AddRow("Payment Mode:", bill.PaymentMode?.Trim() ?? "");
            if (bill.PaymentMode?.Trim() == "Cash")
            {
                AddRow("Cash Received:", $"{bill.Cash:N2}");
                AddRow("Change:", $"{bill.Change:N2}");
            }
            AddDivider();

            // ---------------- FOOTER ----------------
            if (!string.IsNullOrWhiteSpace(hotel?.TicketFooterMessage))
            {
                AddCentered(hotel.TicketFooterMessage.Trim(), 11);
            }
            else
            {
                AddCentered("Thank you for shopping with us!", 11);
            }

            return doc;
        }
    }
}
