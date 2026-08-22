namespace Clidapos.Wpf.Entities
{
    /// <summary>Maps to the real Customer table - the customer master data.</summary>
    public class Customer
    {
        public int ID { get; set; }
        public string CustomerID { get; set; } = "";
        public string Name { get; set; } = "";
        public string? ContactNo { get; set; }
        public string? Email { get; set; }
    }
}