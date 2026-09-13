using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    /// <summary>
    /// The role hierarchy above the per-module UserRight system:
    /// - Super Admin: full access to everything, including Master Setting.
    ///   Deliberately a fixed check against UserType, not something
    ///   configurable via User Security Roles - a Super Admin can never be
    ///   accidentally locked out of their own system.
    /// - Admin: full access to everything EXCEPT Master Setting.
    /// - Everyone else (Cashier, etc.): restricted per-module by whatever
    ///   User Security Roles has configured for them.
    /// </summary>
    public static class PermissionService
    {
        public static bool IsSuperAdmin(Registration user) =>
            user.UserType.Trim().Equals("Super Admin", System.StringComparison.OrdinalIgnoreCase);

        public static bool IsAdmin(Registration user) =>
            user.UserType.Trim().Equals("Admin", System.StringComparison.OrdinalIgnoreCase);

        public static bool IsManager(Registration user) =>
            user.UserType.Trim().Equals("Manager", System.StringComparison.OrdinalIgnoreCase);

        /// <summary>Admin, Super Admin, or Manager - Cashiers are the only
        /// role that stays completely locked out of Back Office. Manager gets
        /// in, but which specific tiles they see is then further restricted
        /// by ApplyManagerRestrictionsAsync in BackOfficeView.</summary>
        public static bool CanAccessBackOffice(Registration user) =>
            IsAdmin(user) || IsSuperAdmin(user) || IsManager(user);

        /// <summary>Only Super Admin - Master Setting is the one thing even
        /// a regular Admin cannot reach.</summary>
        public static bool CanAccessMasterSetting(Registration user) =>
            IsSuperAdmin(user);
    }
}
