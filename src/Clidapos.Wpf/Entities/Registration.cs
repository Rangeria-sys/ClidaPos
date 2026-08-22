using System;

namespace Clidapos.Wpf.Entities
{
    public class Registration
    {
        public string UserID { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; }
        public string? Active { get; set; }
        public string? ContactNo { get; set; }
        public string? EmailID { get; set; }
        public string? SSN { get; set; }
        public string? PayrollType { get; set; }
        public string? CardNo { get; set; }
        public string? AutoLogout { get; set; }
    }
}