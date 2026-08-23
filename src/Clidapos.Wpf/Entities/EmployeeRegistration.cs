using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>
    /// Maps to the EmployeeRegistration table - real HR/personal details for staff.
    /// Photo is NOT NULL in the real database - a real upload UI is a separate task,
    /// so the service inserts an empty placeholder for now rather than a real photo.
    /// </summary>
    public class EmployeeRegistration
    {
        public int EmpId { get; set; }
        public string EmployeeID { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string ContactNo { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime DateOfJoining { get; set; }
        public string? Active { get; set; }
        public byte[]? Photo { get; set; }
    }
}