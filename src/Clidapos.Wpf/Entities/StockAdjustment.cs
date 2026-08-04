using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>Maps to StockAdjustment_Warehouse - a log of manual stock corrections.</summary>
    public class StockAdjustment
    {
        public int SA_ID { get; set; }
        public DateTime? Date { get; set; }
        public string? Warehouse { get; set; }
        public int? ProductID { get; set; }
        public string? AdjustmentType { get; set; }
        public decimal? Qty { get; set; }
        public string? Reason { get; set; }
    }
}