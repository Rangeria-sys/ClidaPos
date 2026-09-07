namespace Clidapos.Wpf.Entities
{
    public class Supplier
    {
        public int ID { get; set; }
        public string SupplierID { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? ContactNo { get; set; }
        public string? EmailID { get; set; }
        public string? Bank { get; set; }
        public string? Branch { get; set; }
        public string? AccountName { get; set; }
        public string? AccountNumber { get; set; }
        public decimal? OpeningBalance { get; set; }
        public string? OpeningBalanceType { get; set; }
        public string? Remarks { get; set; }
    }
}