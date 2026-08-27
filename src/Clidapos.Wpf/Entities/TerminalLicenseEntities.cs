using System;

namespace Clidapos.Wpf.Entities
{
    /// <summary>Terminal hardware reference config - a singleton, one row.
    /// Printer selection is real (drawn from installed Windows printers). Scanner and
    /// WiFi fields are plain reference/label text, not live hardware control -
    /// most USB barcode scanners act as keyboard input and need no configuration here.</summary>
    public class TerminalSetting
    {
        public int Id { get; set; }
        public string? TerminalName { get; set; }
        public string? PrinterName { get; set; }
        public string? ReceiptPaperWidth { get; set; }
        public string? ScannerNotes { get; set; }
        public string? WifiNetworkName { get; set; }
    }

    /// <summary>A local-only activation record - a singleton, one row. This does not
    /// call out to any license server or cryptographically validate anything; it is
    /// simply a stored record of whether this installation has been marked activated.</summary>
    public class LicenseSetting
    {
        public int Id { get; set; }
        public string? LicenseKey { get; set; }
        public DateTime? ActivatedDate { get; set; }
        public string? IsActive { get; set; }
        public string? Notes { get; set; }
    }
}