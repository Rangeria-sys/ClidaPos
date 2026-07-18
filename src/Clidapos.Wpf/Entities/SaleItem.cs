namespace Clidapos.Wpf.Entities
{
    /// <summary>Maps to RestaurantPOS_OrderedProductBillTA - one line on a counter sale.</summary>
    public class SaleItem
    {
        public int OP_ID { get; set; }
        public int? BillID { get; set; }
        public string? Dish { get; set; }
        public decimal? Rate { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Amount { get; set; }
        public decimal? VATPer { get; set; }
        public decimal? VATAmount { get; set; }
        public decimal? DiscountPer { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Category { get; set; }
        public string? ItemStatus { get; set; }
    }
}