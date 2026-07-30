using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>Audit record for a sale removed after payment - matches DeletedInvoices table.</summary>
    public class DeletedInvoice
    {
        public int Id { get; set; }
        public string? BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public decimal? GrandTotal { get; set; }
        public string? Operator { get; set; }
        public string? PaymentMode { get; set; }
        public string? Reason { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string? BillType { get; set; }
        public string? Canceled_Deleted { get; set; }
    }
}