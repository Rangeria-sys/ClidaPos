using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>
    /// Shared helper so account numbers are masked the same way everywhere
    /// they're displayed - shows only the last 4 digits, like any real
    /// banking app, so a glance at the screen or a screenshot never exposes
    /// the full number.
    /// </summary>
    public static class AccountNumberMasker
    {
        public static string Mask(string? accountNo)
        {
            var value = (accountNo ?? "").Trim();
            if (value.Length <= 4) return value.Length == 0 ? "" : new string('•', value.Length);
            return new string('•', value.Length - 4) + value[^4..];
        }
    }

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
        public string MaskedAccountNo => AccountNumberMasker.Mask(AccountNo);
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