namespace Clidapos.Wpf.Entities
{
    /// <summary>Default shift configuration - a singleton, one row. Distinct from
    /// WorkPeriodStart/WorkPeriodEnd, which record the actual daily open/close events;
    /// this is just the defaults/preferences used to guide that flow.</summary>
    public class WorkPeriodSetting
    {
        public int Id { get; set; }
        public string? DefaultStartTime { get; set; }
        public string? DefaultEndTime { get; set; }
        public string? AutoCloseEnabled { get; set; }
        public int? ReminderMinutesBeforeClose { get; set; }
    }
}