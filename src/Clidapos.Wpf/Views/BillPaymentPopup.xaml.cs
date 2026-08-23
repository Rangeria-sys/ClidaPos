using System;
using System.Collections.Generic;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class BillPaymentPopup : Window
    {
        private readonly ExpenseService _expenseService = new();
        private readonly VoucherService _voucherService = new();
        private readonly LogService _logService = new();

        public BillPaymentPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                List<Expense> bills = await _expenseService.GetAllAsync();
                BillInput.ItemsSource = bills;
            };
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (BillInput.SelectedItem is not Expense selectedBill)
            {
                ErrorText.Text = "Select which bill this payment is for.";
                return;
            }
            if (string.IsNullOrWhiteSpace(PaymentModeInput.Text))
            {
                ErrorText.Text = "Payment Mode is required.";
                return;
            }
            if (!decimal.TryParse(AmountInput.Text, out var amount) || amount <= 0)
            {
                ErrorText.Text = "Enter a valid Amount greater than zero.";
                return;
            }

            var billName = selectedBill.ExpenseName.Trim();
            var paidTo = string.IsNullOrWhiteSpace(PaidToInput.Text) ? billName : PaidToInput.Text.Trim();

            var lines = new List<VoucherLine>
            {
                new VoucherLine { Particulars = billName, Amount = amount, Note = NotesInput.Text.Trim() }
            };

            try
            {
                var voucher = await _voucherService.SaveVoucherAsync(paidTo, PaymentModeInput.Text, NotesInput.Text, lines);
                await _logService.LogAsync(CurrentSession.UserId,
                    $"Recorded Bill Payment for '{billName}' - {amount:N2} ({voucher.VoucherNo.Trim()})");

                MessageBox.Show($"Payment recorded. Voucher {voucher.VoucherNo.Trim()}.", "Clidapos");
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