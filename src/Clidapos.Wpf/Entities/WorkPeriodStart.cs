using System;

namespace Clidapos.Wpf.Entities
{
    public class WorkPeriodStart
    {
        public int ID { get; set; }
        public DateTime WPStart { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? OpeningCash { get; set; }
        public string? CashierUserID { get; set; }
        public string? CashierName { get; set; }
        public string? TerminalID { get; set; }
    }
}