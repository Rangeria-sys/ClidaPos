using System;

namespace Clidapos.Wpf.Entities
{
    public class Purchase
    {
        public int ST_ID { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string PurchaseType { get; set; } = "Initial";
        public int Supplier_ID { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountPer { get; set; }
        public decimal Discount { get; set; }
        public decimal PreviousDue { get; set; }
        public decimal FreightCharges { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal Total { get; set; }
        public decimal RoundOff { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalPayment { get; set; }
        public decimal PaymentDue { get; set; }
    }
}