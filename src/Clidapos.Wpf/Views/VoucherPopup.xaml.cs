using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class VoucherPopup : Window
    {
        private readonly VoucherService _voucherService = new();
        private readonly LogService _logService = new();
        private readonly ObservableCollection<VoucherLine> _lines = new();

        public VoucherPopup()
        {
            InitializeComponent();
            LinesGrid.ItemsSource = _lines;
            _lines.CollectionChanged += (s, e) => RecomputeTotal();
        }

        private void RecomputeTotal()
        {
            GrandTotalText.Text = _lines.Sum(l => l.Amount).ToString("N2");
        }

        private void AddLine_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(ParticularsInput.Text))
            {
                ErrorText.Text = "Enter a description for this line item.";
                return;
            }
            if (!decimal.TryParse(AmountInput.Text, out var amount) || amount <= 0)
            {
                ErrorText.Text = "Enter a valid amount greater than zero.";
                return;
            }

            _lines.Add(new VoucherLine { Particulars = ParticularsInput.Text.Trim(), Amount = amount });
            ParticularsInput.Clear();
            AmountInput.Clear();
            ParticularsInput.Focus();
        }

        private async void SaveVoucher_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                ErrorText.Text = "Paid To is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(PaymentModeInput.Text))
            {
                ErrorText.Text = "Payment Mode is required.";
                return;
            }
            if (_lines.Count == 0)
            {
                ErrorText.Text = "Add at least one line item before saving.";
                return;
            }

            try
            {
                var voucher = await _voucherService.SaveVoucherAsync(
                    NameInput.Text, PaymentModeInput.Text, DetailsInput.Text, _lines.ToList());

                await _logService.LogAsync(CurrentSession.UserId,
                    $"Created Payment Voucher {voucher.VoucherNo.Trim()} to '{NameInput.Text.Trim()}' - {voucher.GrandTotal:N2}");

                MessageBox.Show($"Voucher {voucher.VoucherNo.Trim()} saved. Total: {voucher.GrandTotal:N2}", "Clidapos");
                Close();
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                ErrorText.Text = detail;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}