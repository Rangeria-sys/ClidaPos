using System;

namespace Clidapos.Wpf.Entities
{
    public class Bank
    {
        public string BankName { get; set; } = "";
    }

    public class BankBranch
    {
        public int Id { get; set; }
        public string? BranchName { get; set; }
        public string? Address { get; set; }
        public string? ContactNo { get; set; }
        public string? SwiftCode { get; set; }
        public string? IFSCCode { get; set; }
        public string BankName { get; set; } = "";
    }

    public class BankAccountRegistration
    {
        public string AccountNo { get; set; } = "";
        public string? AccountName { get; set; }
        public string? AccountType { get; set; }
        public DateTime? OpeningDate { get; set; }
        public decimal? BalanceAmount { get; set; }
        public string? Active { get; set; }
        public int? BranchID { get; set; }
        public int? Id { get; set; }
    }

    public class BankAccountLedger
    {
        public int Id { get; set; }
        public DateTime? Date { get; set; }
        public string? AccNo { get; set; }
        public string? LedgerNo { get; set; }
        public string? Label { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
    }
}