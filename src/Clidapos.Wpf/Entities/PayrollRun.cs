using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>
    /// Maps to the real PayrollRun table - genuinely linked to EmployeeRegistration.EmpId
    /// (not the login table), with Kenya-appropriate deductions (NSSF, SHA, Housing Levy,
    /// PAYE) instead of the old Canadian-oriented Payroll/Payroll_MB tables.
    /// </summary>
    public class PayrollRun
    {
        public int Id { get; set; }
        public int EmpId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PayMonth { get; set; }
        public int? PayYear { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal? NSSFPer { get; set; }
        public decimal? NSSF { get; set; }
        public decimal? SHAPer { get; set; }
        public decimal? SHA { get; set; }
        public decimal? HousingLevyPer { get; set; }
        public decimal? HousingLevy { get; set; }
        public decimal? PAYEPer { get; set; }
        public decimal? PAYE { get; set; }
        public decimal? NetPay { get; set; }
        public string? PaymentMode { get; set; }
        public string? Remarks { get; set; }
    }
}