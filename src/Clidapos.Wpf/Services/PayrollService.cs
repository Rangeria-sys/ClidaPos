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
        public int EmpId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string EmployeeID { get; set; } = "";
        public string NationalID { get; set; } = "";
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

            return runs.Select(r =>
            {
                var emp = employees.FirstOrDefault(e => e.EmpId == r.EmpId);
                return new PayrollHistoryRow
                {
                    Id = r.Id,
                    EmpId = r.EmpId,
                    EmployeeName = emp?.EmployeeName.Trim() ?? "(unknown employee)",
                    EmployeeID = emp?.EmployeeID.Trim() ?? "",
                    NationalID = emp?.NationalID?.Trim() ?? "",
                    PaymentDate = r.PaymentDate,
                    PayMonth = r.PayMonth?.Trim(),
                    PayYear = r.PayYear,
                    GrossSalary = r.GrossSalary,
                    NetPay = r.NetPay
                };
            }).ToList();
        }

        public async Task<PayrollRun?> GetByIdAsync(int id)
        {
            using var db = new ClidaposDbContext();
            return await db.Set<PayrollRun>().FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task UpdateAsync(PayrollRun run)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<PayrollRun>().FirstOrDefaultAsync(p => p.Id == run.Id);
            if (existing == null) return;

            existing.PaymentDate = run.PaymentDate;
            existing.PayMonth = run.PayMonth;
            existing.PayYear = run.PayYear;
            existing.GrossSalary = run.GrossSalary;
            existing.NSSFPer = run.NSSFPer;
            existing.NSSF = run.NSSF;
            existing.SHAPer = run.SHAPer;
            existing.SHA = run.SHA;
            existing.HousingLevyPer = run.HousingLevyPer;
            existing.HousingLevy = run.HousingLevy;
            existing.PAYEPer = run.PAYEPer;
            existing.PAYE = run.PAYE;
            existing.NetPay = run.NetPay;

            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<PayrollRun>().FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null) return;

            db.Set<PayrollRun>().Remove(existing);
            await db.SaveChangesAsync();
        }

        public async Task AddAsync(PayrollRun run)
        {
            using var db = new ClidaposDbContext();
            db.Set<PayrollRun>().Add(run);
            await db.SaveChangesAsync();
        }
    }
}
