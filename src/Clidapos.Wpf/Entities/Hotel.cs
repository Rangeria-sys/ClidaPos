namespace Clidapos.Wpf.Entities
{
    /// <summary>
    /// Maps to the Hotel table - the business profile. A singleton: exactly one row.
    /// AddressLine1/2 are repurposed for this business as Till/Paybill Number and
    /// Account Number (shown on receipts) rather than a literal postal address -
    /// the column names stay as-is, only their meaning and on-screen labels change.
    /// DBLocation (a technical database path) is deliberately left out.
    /// </summary>
    public class Hotel
    {
        public int Id { get; set; }
        public string? HotelName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? AddressLine3 { get; set; }
        public string? ContactNo { get; set; }
        public string? EmailID { get; set; }
        public string? TIN { get; set; }
        public string? STNo { get; set; }
        public string? CIN { get; set; }
        public string? BaseCurrency { get; set; }
        public string? CurrencyCode { get; set; }
        public string? TicketFooterMessage { get; set; }
        public string? ShowLogo { get; set; }
        public decimal? CapitalAccount { get; set; }
        public byte[]? Logo { get; set; }
    }
}