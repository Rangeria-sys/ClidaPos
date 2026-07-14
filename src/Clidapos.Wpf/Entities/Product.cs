namespace Clidapos.Wpf.Entities
{
    public class Product
    {
        public int PID { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? Unit { get; set; }
        public decimal Price { get; set; }
        public int ReorderPoint { get; set; }
        public string? P_Supplier { get; set; }
    }
}