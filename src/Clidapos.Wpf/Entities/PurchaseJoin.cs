namespace Clidapos.Wpf.Entities
{
    public class PurchaseJoin
    {
        public int SP_ID { get; set; }
        public int PurchaseID { get; set; }
        public int ProductID { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal TotalAmount { get; set; }
        public string Warehouse { get; set; } = "Main Store";
    }
}