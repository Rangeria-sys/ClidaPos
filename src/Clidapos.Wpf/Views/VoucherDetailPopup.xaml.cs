using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class VoucherDetailPopup : Window
    {
        private readonly VoucherService _voucherService = new();

        public VoucherDetailPopup(Voucher voucher)
        {
            InitializeComponent();

            VoucherNoText.Text = voucher.VoucherNo.Trim();
            SummaryText.Text = $"Paid To: {voucher.Name?.Trim()}   •   {voucher.Date:dd MMM yyyy}   •   {voucher.PaymentMode.Trim()}"
                + (string.IsNullOrWhiteSpace(voucher.Details) ? "" : $"\n{voucher.Details.Trim()}");
            GrandTotalText.Text = voucher.GrandTotal.ToString("N2");

            Loaded += async (s, e) => LinesGrid.ItemsSource = await _voucherService.GetLinesForVoucherAsync(voucher.ID);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}