using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>The chronological transaction log - one row per journal entry.</summary>
    public class JournalEntry
    {
        public int ID { get; set; }
        public string? DebitAccount { get; set; }
        public string? CreditAccount { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Amount { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>The same data reorganized by account - two rows written per Journal entry
    /// (one Debit row against the debited account, one Credit row against the credited
    /// account), giving a genuine per-account running balance.</summary>
    public class LedgerBookEntry
    {
        public int Id { get; set; }
        public DateTime? Date { get; set; }
        public string? Name { get; set; }
        public string? LedgerNo { get; set; }
        public string? Label { get; set; }
        public string? AccLedger { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public string? PartyID { get; set; }
    }
}