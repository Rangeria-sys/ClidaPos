using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class PayrollHistoryRow
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = "";
        public DateTime PaymentDate { get; set; }
        public string? PayMonth { get; set; }
        public int? PayYear { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal? NetPay { get; set; }
    }

    public class PayrollService
    {
        public async Task<int> GetNextIdAsync()
        {
            using var db = new ClidaposDbContext();
            var maxId = await db.Set<PayrollRun>().Select(p => (int?)p.Id).MaxAsync();
            return (maxId ?? 0) + 1;
        }

        public async Task<List<PayrollHistoryRow>> GetRecentAsync()
        {
            using var db = new ClidaposDbContext();

            var runs = await db.Set<PayrollRun>()
                .OrderByDescending(p => p.PaymentDate)
                .Take(200)
                .ToListAsync();

            var employees = await db.Set<EmployeeRegistration>().ToListAsync();

            return runs.Select(r => new PayrollHistoryRow
            {
                Id = r.Id,
                EmployeeName = employees.FirstOrDefault(e => e.EmpId == r.EmpId)?.EmployeeName.Trim() ?? "(unknown employee)",
                PaymentDate = r.PaymentDate,
                PayMonth = r.PayMonth?.Trim(),
                PayYear = r.PayYear,
                GrossSalary = r.GrossSalary,
                NetPay = r.NetPay
            }).ToList();
        }

        public async Task AddAsync(PayrollRun run)
        {
            using var db = new ClidaposDbContext();
            db.Set<PayrollRun>().Add(run);
            await db.SaveChangesAsync();
        }
    }
}
