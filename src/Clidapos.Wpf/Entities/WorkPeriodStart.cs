using System;

namespace Clidapos.Wpf.Entities
{
    public class WorkPeriodStart
    {
        public int ID { get; set; }
        public DateTime WPStart { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}