namespace Clidapos.Wpf.Entities
{
    /// <summary>M-Pesa Daraja API configuration - a singleton, one row.</summary>
    public class MpesaSetting
    {
        public int Id { get; set; }
        public string? ConsumerKey { get; set; }
        public string? ConsumerSecret { get; set; }
        public string? Shortcode { get; set; }
        public string? PassKey { get; set; }
        public string? AccountNumber { get; set; }
        public string? Environment { get; set; }
    }

    /// <summary>SMTP email configuration - genuinely a list (multiple named server
    /// configs, each markable as Default/Active), not a singleton.</summary>
    public class EmailSetting
    {
        public int Id { get; set; }
        public string? ServerName { get; set; }
        public string? SMTPAddress { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int? Port { get; set; }
        public string? TLS_SSL_Required { get; set; }
        public string? IsDefault { get; set; }
        public string? IsActive { get; set; }
    }

    /// <summary>SMS gateway configuration - a URL-template based design (no separate
    /// ApiKey/SenderID columns exist - those are expected to be embedded directly in
    /// the APIURL as query parameters or path segments by whoever configures it).</summary>
    public class SMSSetting
    {
        public int Id { get; set; }
        public string? APIURL { get; set; }
        public string? IsDefault { get; set; }
        public string? IsEnabled { get; set; }
    }
}