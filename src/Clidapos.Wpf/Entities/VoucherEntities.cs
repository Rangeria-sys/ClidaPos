using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>A payment voucher header - an accounting record of a payment made.</summary>
    public class Voucher
    {
        public int ID { get; set; }
        public string VoucherNo { get; set; } = "";
        public string? Name { get; set; }
        public DateTime Date { get; set; }
        public string? Details { get; set; }
        public string PaymentMode { get; set; } = "";
        public decimal GrandTotal { get; set; }
    }

    /// <summary>One itemized line under a Voucher - linked via VoucherID.</summary>
    public class VoucherOtherDetail
    {
        public int VD_ID { get; set; }
        public int VoucherID { get; set; }
        public string Particulars { get; set; } = "";
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>A time-windowed special rate on a specific item ("Dish" naming matches
    /// SaleItem.Dish elsewhere in this schema, but works for supermarket products too).</summary>
    public class Promotion
    {
        public int Id { get; set; }
        public string? Dish { get; set; }
        public decimal? Rate { get; set; }
        public string? PDay { get; set; }
        public DateTime? TimeFrom { get; set; }
        public DateTime? TimeTo { get; set; }
        public string? Active { get; set; }
    }
}