using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>Maps to the Logs table - one row per meaningful action taken in the app.</summary>
    public class LogEntry
    {
        public int Id { get; set; }
        public string UserID { get; set; } = "";
        public string Operation { get; set; } = "";
        public DateTime Date { get; set; }
    }
}