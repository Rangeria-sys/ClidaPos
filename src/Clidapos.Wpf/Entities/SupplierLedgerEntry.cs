using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>
    /// Maps to SupplierLedgerBook - real financial transactions with a supplier.
    /// PartyID stores Supplier.SupplierID (the business code, e.g. "SUPP-3"),
    /// giving a genuine link back to the real Supplier table instead of just
    /// free-text names.
    /// </summary>
    public class SupplierLedgerEntry
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; } = "";
        public string LedgerNo { get; set; } = "";
        public string Label { get; set; } = "";
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string? PartyID { get; set; }
    }
}