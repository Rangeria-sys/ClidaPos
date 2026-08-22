using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>
    /// Maps to the EmployeeRegistration table - real HR/personal details for staff.
    /// Photo (binary image) is deliberately left out - same reasoning as Hotel.Logo,
    /// a real upload UI is a separate task, not a text field.
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
    }
}