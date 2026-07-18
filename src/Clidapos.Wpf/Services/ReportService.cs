using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;

namespace Clidapos.Wpf.Services
{
    public class ReportService
    {
        /// <summary>
        /// Totals every sale that falls inside a work period.
        /// If the period is still open, the window runs up to now.
        /// </summary>
        public async Task<ShiftSummary?> GetPeriodSummaryAsync(int periodId)
        {
            using var db = new ClidaposDbContext();

            var start = await db.WorkPeriodStarts.FirstOrDefaultAsync(p => p.ID == periodId);
            if (start == null) return null;

            var end = await db.WorkPeriodEnds.FirstOrDefaultAsync(e => e.Id == periodId);

            var from = start.WPStart;
            var to = end?.WPEnd ?? DateTime.Now;

            var bills = await db.SaleBills
                .Where(b => b.BillDate >= from && b.BillDate <= to)
                .ToListAsync();

            var summary = new ShiftSummary
            {
                PeriodId = periodId,
                StartedAt = from,
                EndedAt = end?.WPEnd,
                BillCount = bills.Count,
                GrandTotal = bills.Sum(b => b.GrandTotal ?? 0),
                TaxableTotal = bills.Sum(b => b.TotalTaxableAmount ?? 0),
                VatTotal = bills.Sum(b => b.TotalTaxAmount ?? 0)
            };

            foreach (var b in bills)
            {
                var mode = (b.PaymentMode ?? "").Trim();
                var amount = b.GrandTotal ?? 0;

                if (mode.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                    summary.CashTotal += amount;
                else if (mode.Equals("M-Pesa", StringComparison.OrdinalIgnoreCase))
                    summary.MpesaTotal += amount;
                else if (mode.Equals("Card", StringComparison.OrdinalIgnoreCase))
                    summary.CardTotal += amount;
                else
                    summary.OtherTotal += amount;
            }

            if (bills.Count > 0)
            {
                var billIds = bills.Select(b => b.Id).ToList();

                var items = await db.SaleItems
                    .Where(i => i.BillID != null && billIds.Contains(i.BillID.Value))
                    .ToListAsync();

                summary.ItemCount = items.Sum(i => i.Quantity ?? 0);

                summary.TopItems = items
                    .GroupBy(i => (i.Dish ?? "").Trim())
                    .Select(g => new TopItem
                    {
                        Name = g.Key,
                        Qty = g.Sum(x => x.Quantity ?? 0),
                        Value = g.Sum(x => x.TotalAmount ?? 0)
                    })
                    .OrderByDescending(t => t.Value)
                    .Take(5)
                    .ToList();
            }

            return summary;
        }
    }
}