using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;

namespace Clidapos.Wpf.Services
{
    public enum SalesBreakdownGranularity { Daily, Weekly, Monthly }

    public class SalesBreakdownRow
    {
        public DateTime PeriodStart { get; set; }
        public string PeriodLabel { get; set; } = "";
        public int BillCount { get; set; }
        public decimal GrandTotal { get; set; }
    }

    public class CashierSalesRow
    {
        public string Operator { get; set; } = "";
        public string CashierName { get; set; } = "";
        public int BillCount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AverageSale { get; set; }
    }

    public class VoidedSaleRow
    {
        public string BillNo { get; set; } = "";
        public DateTime? BillDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string Operator { get; set; } = "";
        public string PaymentMode { get; set; } = "";
        public decimal GrandTotal { get; set; }
        public string Reason { get; set; } = "";
    }

    public class VoidedSalesSummary
    {
        public int Count { get; set; }
        public decimal TotalVoided { get; set; }
        public List<VoidedSaleRow> Rows { get; set; } = new();
    }

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
                .Where(b => start.TerminalID == null || b.TerminalID == start.TerminalID)
                .ToListAsync();

            var summary = new ShiftSummary
            {
                PeriodId = periodId,
                StartedAt = from,
                EndedAt = end?.WPEnd,
                BillCount = bills.Count,
                GrandTotal = bills.Sum(b => b.GrandTotal ?? 0),
                TaxableTotal = bills.Sum(b => b.TotalTaxableAmount ?? 0),
                VatTotal = bills.Sum(b => b.TotalTaxAmount ?? 0),
                DiscountsTotal = bills.Sum(b => b.TADiscountAmt ?? 0),
                OpeningCash = start.OpeningCash,
                ClosingCash = end?.ClosingCash,
                CashierName = start.CashierName
            };

            var voided = await GetVoidedSalesAsync(from, to);
            summary.VoidedSalesCount = voided.Count;
            summary.VoidedSalesTotal = voided.TotalVoided;

            // Cashier breakdown - Operator on each sale identifies who rang it
            // up, regardless of the period itself being shared/store-wide.
            var operators = await db.Registrations.ToListAsync();
            var voidsByOperator = voided.Rows
                .GroupBy(v => (v.Operator ?? "").Trim())
                .ToDictionary(g => g.Key, g => g.Count());

            summary.CashierBreakdown = bills
                .GroupBy(b => (b.Operator ?? "").Trim())
                .Select(g =>
                {
                    var op = operators.FirstOrDefault(o => o.UserID.Trim() == g.Key);
                    var name = op != null ? op.Name.Trim() : (string.IsNullOrEmpty(g.Key) ? "Unknown" : g.Key);
                    voidsByOperator.TryGetValue(g.Key, out var voidCount);
                    return new CashierBreakdownRow
                    {
                        CashierName = name,
                        BillCount = g.Count(),
                        Takings = g.Sum(b => b.GrandTotal ?? 0),
                        VoidCount = voidCount
                    };
                })
                .OrderByDescending(c => c.Takings)
                .ToList();

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

        /// <summary>
        /// Totals every sale whose BillDate falls within [from, to] inclusive -
        /// for the Sales Report screen, where the user picks any date range,
        /// not just a single work period/shift.
        /// </summary>
        /// <summary>Store-wide summary combining every terminal's sales for one
        /// calendar day - unlike GetPeriodSummaryAsync (one terminal's period)
        /// or GetSalesReportAsync (missing discounts/voided/cashier breakdown).
        /// Deliberately leaves OpeningCash/ClosingCash/CashVariance null - there's
        /// no single combined drawer across separate tills to reconcile against,
        /// so showing a number there would be misleading.</summary>
        public async Task<ShiftSummary> GetCombinedDailySummaryAsync(DateTime date)
        {
            var from = date.Date;
            var to = date.Date.AddDays(1).AddTicks(-1);

            using var db = new ClidaposDbContext();

            var bills = await db.SaleBills
                .Where(b => b.BillDate >= from && b.BillDate <= to)
                .ToListAsync();

            var summary = new ShiftSummary
            {
                PeriodId = 0,
                StartedAt = from,
                EndedAt = to,
                BillCount = bills.Count,
                GrandTotal = bills.Sum(b => b.GrandTotal ?? 0),
                TaxableTotal = bills.Sum(b => b.TotalTaxableAmount ?? 0),
                VatTotal = bills.Sum(b => b.TotalTaxAmount ?? 0),
                DiscountsTotal = bills.Sum(b => b.TADiscountAmt ?? 0)
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
                    .Take(10)
                    .ToList();
            }

            var voided = await GetVoidedSalesAsync(from, to);
            summary.VoidedSalesCount = voided.Count;
            summary.VoidedSalesTotal = voided.TotalVoided;

            var operators = await db.Registrations.ToListAsync();
            var voidsByOperator = voided.Rows
                .GroupBy(v => (v.Operator ?? "").Trim())
                .ToDictionary(g => g.Key, g => g.Count());

            summary.CashierBreakdown = bills
                .GroupBy(b => (b.Operator ?? "").Trim())
                .Select(g =>
                {
                    var op = operators.FirstOrDefault(o => o.UserID.Trim() == g.Key);
                    var name = op != null ? op.Name.Trim() : (string.IsNullOrEmpty(g.Key) ? "Unknown" : g.Key);
                    voidsByOperator.TryGetValue(g.Key, out var voidCount);
                    return new CashierBreakdownRow
                    {
                        CashierName = name,
                        BillCount = g.Count(),
                        Takings = g.Sum(b => b.GrandTotal ?? 0),
                        VoidCount = voidCount
                    };
                })
                .OrderByDescending(c => c.Takings)
                .ToList();

            return summary;
        }

        public async Task<ShiftSummary> GetSalesReportAsync(DateTime from, DateTime to)
        {
            using var db = new ClidaposDbContext();

            var bills = await db.SaleBills
                .Where(b => b.BillDate >= from && b.BillDate <= to)
                .ToListAsync();

            var summary = new ShiftSummary
            {
                PeriodId = 0,
                StartedAt = from,
                EndedAt = to,
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
                    .Take(10)
                    .ToList();
            }

            return summary;
        }

        /// <summary>
        /// Groups every sale in [from, to] by day, ISO week (Monday start), or
        /// calendar month - for the Sales Report's Daily/Weekly/Monthly Totals tab.
        /// </summary>
        public async Task<List<SalesBreakdownRow>> GetSalesBreakdownAsync(DateTime from, DateTime to, SalesBreakdownGranularity granularity)
        {
            using var db = new ClidaposDbContext();

            var bills = await db.SaleBills
                .Where(b => b.BillDate >= from && b.BillDate <= to)
                .ToListAsync();

            DateTime BucketStart(DateTime d) => granularity switch
            {
                SalesBreakdownGranularity.Daily => d.Date,
                SalesBreakdownGranularity.Weekly => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7)),
                SalesBreakdownGranularity.Monthly => new DateTime(d.Year, d.Month, 1),
                _ => d.Date
            };

            string Label(DateTime bucketStart) => granularity switch
            {
                SalesBreakdownGranularity.Daily => bucketStart.ToString("ddd, dd MMM yyyy"),
                SalesBreakdownGranularity.Weekly => $"{bucketStart:dd MMM} - {bucketStart.AddDays(6):dd MMM yyyy}",
                SalesBreakdownGranularity.Monthly => bucketStart.ToString("MMMM yyyy"),
                _ => bucketStart.ToString("dd MMM yyyy")
            };

            return bills
                .GroupBy(b => BucketStart(b.BillDate))
                .Select(g => new SalesBreakdownRow
                {
                    PeriodStart = g.Key,
                    PeriodLabel = Label(g.Key),
                    BillCount = g.Count(),
                    GrandTotal = g.Sum(b => b.GrandTotal ?? 0)
                })
                .OrderByDescending(r => r.PeriodStart)
                .ToList();
        }

        /// <summary>
        /// Totals every sale in [from, to] by the cashier who rang it up (SaleBill.Operator,
        /// matched against Registration.UserID for the display name) - for the Sales Report's
        /// Sales by Cashier tab.
        /// </summary>
        public async Task<List<CashierSalesRow>> GetSalesByCashierAsync(DateTime from, DateTime to)
        {
            using var db = new ClidaposDbContext();

            var bills = await db.SaleBills
                .Where(b => b.BillDate >= from && b.BillDate <= to)
                .ToListAsync();

            if (bills.Count == 0) return new List<CashierSalesRow>();

            var users = await db.Registrations.ToListAsync();
            var nameLookup = users.ToDictionary(u => u.UserID.Trim(), u => u.Name.Trim());

            return bills
                .GroupBy(b => (b.Operator ?? "(unknown)").Trim())
                .Select(g => new CashierSalesRow
                {
                    Operator = g.Key,
                    CashierName = nameLookup.TryGetValue(g.Key, out var n) ? n : g.Key,
                    BillCount = g.Count(),
                    GrandTotal = g.Sum(b => b.GrandTotal ?? 0),
                    AverageSale = g.Count() > 0 ? g.Sum(b => b.GrandTotal ?? 0) / g.Count() : 0
                })
                .OrderByDescending(c => c.GrandTotal)
                .ToList();
        }

        /// <summary>
        /// Every counter sale voided in [from, to], read from the DeletedInvoices audit
        /// trail (BillType == "TA") written by SaleService.VoidSaleAsync - for the Sales
        /// Report's Voided Sales tab. Filtered on DeletedDate, since the bill itself no
        /// longer exists once voided.
        /// </summary>
        public async Task<VoidedSalesSummary> GetVoidedSalesAsync(DateTime from, DateTime to)
        {
            using var db = new ClidaposDbContext();

            var voided = await db.DeletedInvoices
                .Where(d => d.BillType == "TA" && d.DeletedDate >= from && d.DeletedDate <= to)
                .OrderByDescending(d => d.DeletedDate)
                .ToListAsync();

            // BillNo/Operator/PaymentMode/Reason are all nchar(N) columns - trim the
            // fixed-width padding SQL Server returns before showing these on screen.
            var rows = voided.Select(d => new VoidedSaleRow
            {
                BillNo = (d.BillNo ?? "").Trim(),
                BillDate = d.BillDate,
                DeletedDate = d.DeletedDate,
                Operator = (d.Operator ?? "").Trim(),
                PaymentMode = (d.PaymentMode ?? "").Trim(),
                GrandTotal = d.GrandTotal ?? 0,
                Reason = (d.Reason ?? "").Trim()
            }).ToList();

            return new VoidedSalesSummary
            {
                Count = rows.Count,
                TotalVoided = rows.Sum(r => r.GrandTotal),
                Rows = rows
            };
        }

        /// <summary>The full end-of-day picture the server generates when it
        /// closes: the combined store-wide sales summary, plus every
        /// terminal's own individual cash reconciliation (opening float,
        /// expected, counted, variance) for whichever of their periods ended
        /// on this date - so every cashier's shortage or overage is visible
        /// individually, not just buried in the combined total.</summary>
        public async Task<EndOfDayReport> GetEndOfDayReportAsync(DateTime date)
        {
            var combined = await GetCombinedDailySummaryAsync(date);

            using var db = new ClidaposDbContext();

            var dayStart = date.Date;
            var dayEnd = date.Date.AddDays(1).AddTicks(-1);

            var endedToday = await db.WorkPeriodEnds
                .Where(e => e.WPEnd >= dayStart && e.WPEnd <= dayEnd)
                .ToListAsync();

            var reconciliations = new List<TerminalReconciliationRow>();

            foreach (var end in endedToday)
            {
                var start = await db.WorkPeriodStarts.FirstOrDefaultAsync(s => s.ID == end.Id);
                if (start == null) continue;

                var terminalCashTotal = await db.SaleBills
                    .Where(b => b.BillDate >= start.WPStart && b.BillDate <= end.WPEnd)
                    .Where(b => b.TerminalID == start.TerminalID)
                    .Where(b => (b.PaymentMode ?? "").Trim().Equals("Cash", StringComparison.OrdinalIgnoreCase))
                    .SumAsync(b => b.GrandTotal ?? 0);

                var expected = start.OpeningCash == null ? (decimal?)null : start.OpeningCash.Value + terminalCashTotal;

                reconciliations.Add(new TerminalReconciliationRow
                {
                    TerminalID = start.TerminalID ?? "(unknown)",
                    CashierName = start.CashierName ?? "(unknown)",
                    OpeningCash = start.OpeningCash,
                    ExpectedCash = expected,
                    ClosingCash = end.ClosingCash,
                    Variance = (end.ClosingCash != null && expected != null) ? end.ClosingCash.Value - expected.Value : (decimal?)null
                });
            }

            return new EndOfDayReport
            {
                CombinedSummary = combined,
                TerminalReconciliations = reconciliations.OrderBy(r => r.TerminalID).ToList()
            };
        }
    }

    public class TerminalReconciliationRow
    {
        public string TerminalID { get; set; } = "";
        public string CashierName { get; set; } = "";
        public decimal? OpeningCash { get; set; }
        public decimal? ExpectedCash { get; set; }
        public decimal? ClosingCash { get; set; }
        public decimal? Variance { get; set; }
    }

    public class EndOfDayReport
    {
        public ShiftSummary CombinedSummary { get; set; } = new();
        public List<TerminalReconciliationRow> TerminalReconciliations { get; set; } = new();
    }
}