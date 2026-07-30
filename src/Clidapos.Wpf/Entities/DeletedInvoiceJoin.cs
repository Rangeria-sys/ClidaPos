namespace Clidapos.Wpf.Entities
{
    /// <summary>One archived line item belonging to a voided sale - matches DeletedInvoices_Join table.</summary>
    public class DeletedInvoiceJoin
    {
        public int Id { get; set; }
        public string? BillNo { get; set; }
        public string? ItemName { get; set; }
        public decimal? Qty { get; set; }
        public decimal? TotalAmount { get; set; }
    }
}