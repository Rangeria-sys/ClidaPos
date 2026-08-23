using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class VoucherLine
    {
        public string Particulars { get; set; } = "";
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }

    public class ExpensePaymentModeRow
    {
        public string Mode { get; set; } = "";
        public decimal Total { get; set; }
    }

    public class ExpenseParticularRow
    {
        public string Particulars { get; set; } = "";
        public decimal Total { get; set; }
    }

    public class ExpenseReportSummary
    {
        public decimal TotalSpent { get; set; }
        public int VoucherCount { get; set; }
        public List<ExpensePaymentModeRow> ByPaymentMode { get; set; } = new();
        public List<ExpenseParticularRow> TopParticulars { get; set; } = new();
        public List<Voucher> Vouchers { get; set; } = new();
    }

    public class VoucherSummaryRow
    {
        public int ID { get; set; }
        public string VoucherNo { get; set; } = "";
        public string Name { get; set; } = "";
        public string PaymentMode { get; set; } = "";
        public DateTime Date { get; set; }
        public decimal GrandTotal { get; set; }
        public string Particulars { get; set; } = "";
    }

    public class VoucherService
    {
        public async Task<int> GetNextVoucherIdAsync()
        {
            using var db = new ClidaposDbContext();
            var maxId = await db.Set<Voucher>().Select(v => (int?)v.ID).MaxAsync();
            return (maxId ?? 0) + 1;
        }

        public async Task<List<Voucher>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Set<Voucher>().OrderByDescending(v => v.Date).ToListAsync();
        }

        /// <summary>Every voucher with its line-item description flattened in, for list
        /// views where showing "what was this payment for" matters (e.g. bill payments).</summary>
        public async Task<List<VoucherSummaryRow>> GetVoucherSummariesAsync()
        {
            using var db = new ClidaposDbContext();
            var vouchers = await db.Set<Voucher>().OrderByDescending(v => v.Date).ToListAsync();
            var voucherIds = vouchers.Select(v => v.ID).ToList();
            var allLines = await db.Set<VoucherOtherDetail>().Where(l => voucherIds.Contains(l.VoucherID)).ToListAsync();

            return vouchers.Select(v =>
            {
                var linesForThis = allLines.Where(l => l.VoucherID == v.ID).ToList();
                var particulars = linesForThis.Count == 1
                    ? linesForThis[0].Particulars.Trim()
                    : linesForThis.Count > 1 ? $"{linesForThis.Count} items" : "";

                return new VoucherSummaryRow
                {
                    ID = v.ID,
                    VoucherNo = v.VoucherNo.Trim(),
                    Name = v.Name?.Trim() ?? "",
                    PaymentMode = v.PaymentMode.Trim(),
                    Date = v.Date,
                    GrandTotal = v.GrandTotal,
                    Particulars = particulars
                };
            }).ToList();
        }

        public async Task<List<VoucherOtherDetail>> GetLinesForVoucherAsync(int voucherId)
        {
            using var db = new ClidaposDbContext();
            return await db.Set<VoucherOtherDetail>().Where(l => l.VoucherID == voucherId).ToListAsync();
        }

        /// <summary>Saves a voucher header plus all its line items in one transaction,
        /// with GrandTotal computed as the real sum of the line amounts.</summary>
        public async Task<Voucher> SaveVoucherAsync(string name, string paymentMode, string? details, List<VoucherLine> lines)
        {
            if (lines == null || lines.Count == 0)
                throw new InvalidOperationException("Add at least one line item to the voucher.");

            using var db = new ClidaposDbContext();
            using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                var maxVoucherId = await db.Set<Voucher>().Select(v => (int?)v.ID).MaxAsync() ?? 0;
                var newId = maxVoucherId + 1;
                var grandTotal = Math.Round(lines.Sum(l => l.Amount), 2);

                var voucher = new Voucher
                {
                    ID = newId,
                    VoucherNo = $"PV-{newId}",
                    Name = name.Trim(),
                    Date = DateTime.Now,
                    Details = details?.Trim(),
                    PaymentMode = paymentMode.Trim(),
                    GrandTotal = grandTotal
                };
                db.Set<Voucher>().Add(voucher);
                await db.SaveChangesAsync();

                var maxLineId = await db.Set<VoucherOtherDetail>().Select(l => (int?)l.VD_ID).MaxAsync() ?? 0;
                foreach (var line in lines)
                {
                    maxLineId++;
                    db.Set<VoucherOtherDetail>().Add(new VoucherOtherDetail
                    {
                        VD_ID = maxLineId,
                        VoucherID = newId,
                        Particulars = line.Particulars.Trim(),
                        Amount = Math.Round(line.Amount, 2),
                        Note = line.Note?.Trim()
                    });
                }
                await db.SaveChangesAsync();

                await tx.CommitAsync();
                return voucher;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>Real expense report: every payment voucher in the date range, with
        /// breakdowns by payment mode and by line-item particulars (spending category).</summary>
        public async Task<ExpenseReportSummary> GetExpenseReportAsync(DateTime from, DateTime to)
        {
            using var db = new ClidaposDbContext();
            var toExclusive = to.Date.AddDays(1);

            var vouchers = await db.Set<Voucher>()
                .Where(v => v.Date >= from.Date && v.Date < toExclusive)
                .OrderByDescending(v => v.Date)
                .ToListAsync();

            var voucherIds = vouchers.Select(v => v.ID).ToList();
            var lines = await db.Set<VoucherOtherDetail>()
                .Where(l => voucherIds.Contains(l.VoucherID))
                .ToListAsync();

            return new ExpenseReportSummary
            {
                TotalSpent = vouchers.Sum(v => v.GrandTotal),
                VoucherCount = vouchers.Count,
                Vouchers = vouchers,
                ByPaymentMode = vouchers
                    .GroupBy(v => v.PaymentMode.Trim())
                    .Select(g => new ExpensePaymentModeRow { Mode = g.Key, Total = g.Sum(v => v.GrandTotal) })
                    .OrderByDescending(r => r.Total)
                    .ToList(),
                TopParticulars = lines
                    .GroupBy(l => l.Particulars.Trim())
                    .Select(g => new ExpenseParticularRow { Particulars = g.Key, Total = g.Sum(l => l.Amount) })
                    .OrderByDescending(r => r.Total)
                    .Take(10)
                    .ToList()
            };
        }
    }
}