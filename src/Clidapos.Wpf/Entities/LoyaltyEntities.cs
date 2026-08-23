using System;

namespace Clidapos.Wpf.Entities
{
    public class LoyaltyMember
    {
        public int MemberID { get; set; }
        public string? Name { get; set; }
        public string? CardNo { get; set; }
        public string? ContactNo { get; set; }
        public string? Address { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string? Active { get; set; }
    }

    public class LoyaltyMemberLedgerBook
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string LedgerNo { get; set; } = "";
        public string Label { get; set; } = "";
        public decimal PointsEarned { get; set; }
        public decimal PointsRedeem { get; set; }
        public int? MemberID { get; set; }
    }

    /// <summary>
    /// A named loyalty earning rule (e.g. "Standard": spend Amount, earn Points) -
    /// this is genuinely a list of rules, not a singleton settings row.
    /// </summary>
    public class LoyaltySetting
    {
        public string LoyaltyName { get; set; } = "";
        public decimal? Amount { get; set; }
        public decimal? Points { get; set; }
    }
}