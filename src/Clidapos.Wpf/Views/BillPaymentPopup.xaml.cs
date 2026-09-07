using System;
using System.Collections.Generic;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class BillPaymentPopup : Window
    {
        private readonly Registration _currentUser;
        private readonly Expense _bill;
        private readonly VoucherService _voucherService = new();
        private readonly LogService _logService = new();

        public BillPaymentPopup(Registration currentUser, Expense bill)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _bill = bill;

            SelectedBillText.Text = bill.ExpenseName.Trim();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

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

            var billName = _bill.ExpenseName.Trim();
            var paidTo = string.IsNullOrWhiteSpace(PaidToInput.Text) ? billName : PaidToInput.Text.Trim();

            var confirm = MessageBox.Show(
                $"Are you sure you want to pay {amount:N2} for '{billName}'?\n\nPaid To: {paidTo}\nPayment Mode: {PaymentModeInput.Text.Trim()}",
                "Confirm Payment", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

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

                var ledger = new ExpenseLogListView(_currentUser);
                ledger.Show();
                Close();
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                ErrorText.Text = detail;
            }
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var picker = new ExpensePickerView(_currentUser);
            picker.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var picker = new ExpensePickerView(_currentUser);
            picker.Show();
            Close();
        }

        private static readonly Regex NumericPattern = new(@"^[0-9]*\.?[0-9]*$");

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                e.Handled = true;
                return;
            }

            var proposedText = textBox.Text
                .Remove(textBox.SelectionStart, textBox.SelectionLength)
                .Insert(textBox.SelectionStart, e.Text);

            e.Handled = !NumericPattern.IsMatch(proposedText);
        }

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                SystemSounds.Exclamation.Play();
            }
        }
    }
}
