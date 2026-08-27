using System;
using System.Drawing.Printing;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class TerminalSettingPopup : Window
    {
        private readonly TerminalLicenseService _settingsService = new();
        private readonly LogService _logService = new();
        private TerminalSetting? _setting;

        public TerminalSettingPopup()
        {
            InitializeComponent();

            // Real installed Windows printers - no invented list.
            var printers = new System.Collections.Generic.List<string>();
            foreach (string printer in PrinterSettings.InstalledPrinters)
                printers.Add(printer);
            PrinterInput.ItemsSource = printers;

            Loaded += async (s, e) =>
            {
                _setting = await _settingsService.GetOrCreateTerminalAsync();
                TerminalNameInput.Text = _setting.TerminalName?.Trim() ?? "";
                PrinterInput.Text = _setting.PrinterName?.Trim() ?? "";
                PaperWidthInput.Text = _setting.ReceiptPaperWidth?.Trim() ?? "80mm";
                ScannerNotesInput.Text = _setting.ScannerNotes?.Trim() ?? "";
                WifiInput.Text = _setting.WifiNetworkName?.Trim() ?? "";
            };
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            if (_setting == null) { ErrorText.Text = "Settings haven't loaded yet."; return; }

            _setting.TerminalName = TerminalNameInput.Text.Trim();
            _setting.PrinterName = PrinterInput.Text.Trim();
            _setting.ReceiptPaperWidth = PaperWidthInput.Text.Trim();
            _setting.ScannerNotes = ScannerNotesInput.Text.Trim();
            _setting.WifiNetworkName = WifiInput.Text.Trim();

            try
            {
                await _settingsService.SaveTerminalAsync(_setting);
                await _logService.LogAsync(CurrentSession.UserId, "Updated Terminal Setting");
                MessageBox.Show("Terminal settings saved.", "Clidapos");
                Close();
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.InnerException?.Message ?? ex.Message;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}