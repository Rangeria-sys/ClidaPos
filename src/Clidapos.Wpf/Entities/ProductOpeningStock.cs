namespace Clidapos.Wpf.Entities
{
    public class ProductOpeningStock
    {
        public int PS_ID { get; set; }
        public int ProductID { get; set; }
        public string Warehouse { get; set; } = "Main Store";
        public decimal Qty { get; set; }
        public string? HasExpiryDate { get; set; }
        public string? ExpiryDate { get; set; }
    }
}