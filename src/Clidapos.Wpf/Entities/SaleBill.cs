using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>Maps to RestaurantPOS_BillingInfoTA - the counter sale bill header.</summary>
    public class SaleBill
    {
        public int Id { get; set; }
        public string BillNo { get; set; } = "";
        public DateTime BillDate { get; set; }
        public decimal? SubTotal { get; set; }
        public decimal? TADiscountPer { get; set; }
        public decimal? TADiscountAmt { get; set; }
        public decimal? GrandTotal { get; set; }
        public decimal? Cash { get; set; }
        public decimal? Change { get; set; }
        public string? Operator { get; set; }
        public string? PaymentMode { get; set; }
        public string? CustomerName { get; set; }
        public string? PhoneNo { get; set; }
        public string? TA_Status { get; set; }
        public string? TaxType { get; set; }
        public decimal? Card { get; set; }
        public decimal? TotalTaxableAmount { get; set; }
        public decimal? TotalTaxAmount { get; set; }
    }
}