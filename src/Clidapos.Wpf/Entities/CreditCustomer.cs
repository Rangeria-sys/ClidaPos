using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>
    /// Maps to the real, separate dbo.CreditCustomer table - distinct from the regular
    /// Customer table. This is what CreditCustomerLedger's foreign key actually points
    /// to, discovered when a Credit Sale first tried to write a ledger entry.
    /// </summary>
    public class CreditCustomer
    {
        public int CC_ID { get; set; }
        public string CreditCustomerID { get; set; } = "";
        public string? Name { get; set; }
        public string? ContactNo { get; set; }
        public string? Address { get; set; }
        public decimal? OpeningBalance { get; set; }
        public string? OpeningBalanceType { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string? Active { get; set; }
        public string? EmailID { get; set; }
    }
}
