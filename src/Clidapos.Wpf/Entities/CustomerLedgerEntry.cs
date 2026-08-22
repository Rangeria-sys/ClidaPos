using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>
    /// Maps to CreditCustomerLedger - real transactions with a customer.
    /// CreditCustomer_ID is a genuine int link to Customer.ID.
    /// </summary>
    public class CustomerLedgerEntry
    {
        public int Id { get; set; }
        public DateTime? Date { get; set; }
        public string? LedgerNo { get; set; }
        public string? Label { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public int? CreditCustomer_ID { get; set; }
    }
}