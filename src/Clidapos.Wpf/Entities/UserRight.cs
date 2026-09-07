namespace Clidapos.Wpf.Entities
{
    /// <summary>
    /// One row per (User, Module) pair - the per-screen Save/Update/Delete/View
    /// permission bits behind the "User Security Roles" screen. ID is DB-identity
    /// (auto-increment); a row with ID == 0 has never been saved yet.
    /// </summary>
    public class UserRight
    {
        public int ID { get; set; }
        public string? ModuleName { get; set; }
        public bool? UR_Save { get; set; }
        public bool? UR_Update { get; set; }
        public bool? UR_Delete { get; set; }
        public bool? UR_View { get; set; }
        public string? UserID { get; set; }
    }
}
