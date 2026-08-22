namespace Clidapos.Wpf.Services
{
    /// <summary>
    /// Tracks who's currently logged in so any screen can record an audit log entry
    /// without needing the current user threaded through every constructor.
    /// Set once in BackOfficeView's constructor, the central hub every Back Office
    /// screen is reached through.
    /// </summary>
    public static class CurrentSession
    {
        public static string UserId { get; set; } = "Unknown";
    }
}