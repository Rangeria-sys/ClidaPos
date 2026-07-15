namespace Clidapos.Wpf.Entities
{
    public class Category
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal? VAT { get; set; }
        public decimal? ST { get; set; }
        public decimal? SC { get; set; }
        public int? BackColor { get; set; }
        public string? Kitchen { get; set; }
        public int? CAT_ID { get; set; }
    }
}