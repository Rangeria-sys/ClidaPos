using System;

namespace Clidapos.Wpf.Entities
{
    public class ClockEntry
    {
        public int Id { get; set; }
        public string UserID { get; set; } = "";
        public string? UserName { get; set; }
        public DateTime WorkDate { get; set; }
        public DateTime ClockInTime { get; set; }
        public DateTime? ClockOutTime { get; set; }
    }
}