using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;

namespace Clidapos.Wpf.Views
{
    public partial class SalesHistoryView : Window
    {
        private readonly Registration _currentUser;
        private readonly SaleService _saleService = new();
        private readonly LogService _logService = new();
        private readonly ReceiptService _receiptService = new();
        private int? _selectedBillId;

        public SalesHistoryView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) =>
            {
                await LoadSales();
                await LoadStoreInfo();
            };
        }

        private async System.Threading.Tasks.Task LoadSales()
        {
            SalesGrid.ItemsSource = await _saleService.GetRecentSalesAsync();
        }

        private async System.Threading.Tasks.Task LoadStoreInfo()
        {
            using var db = new ClidaposDbContext();
            var hotel = await db.Set<Hotel>().FirstOrDefaultAsync();
            if (hotel == null) return;

            StoreNameBlock.Text = hotel.HotelName?.Trim() ?? "";
            StoreAddressBlock.Text = string.Join(", ", new[]
            {
                hotel.AddressLine1?.Trim(),
                hotel.AddressLine2?.Trim(),
                hotel.AddressLine3?.Trim()
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
            StoreTelBlock.Text = string.IsNullOrWhiteSpace(hotel.ContactNo) ? "" : $"Tel: {hotel.ContactNo.Trim()}";
            StorePinBlock.Text = string.IsNullOrWhiteSpace(hotel.TIN) ? "" : $"PIN: {hotel.TIN.Trim()}";

            if (!string.IsNullOrWhiteSpace(hotel.TicketFooterMessage))
                FooterBlock.Text = hotel.TicketFooterMessage.Trim();
        }

        private async void SalesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetVoidUI();

            if (SalesGrid.SelectedItem is not SaleBill bill)
            {
                _selectedBillId = null;
                ClearReceiptDetails();
                return;
            }

            _selectedBillId = bill.Id;

            // Header band
            ReceiptHeaderText.Text = $"{bill.BillNo?.Trim()}  ·  {bill.BillDate:dd MMM yyyy, hh:mm tt}";
            ReceiptTotalText.Text = $"{AppSettings.CurrencySymbol} {bill.GrandTotal:N2}";

            // Bill meta
            BillNoBlock.Text = bill.BillNo?.Trim() ?? "";
            BillDateBlock.Text = bill.BillDate.ToString("dd MMM yyyy, hh:mm tt");
            BillCashierBlock.Text = bill.Operator?.Trim() ?? "";

            if (!string.IsNullOrWhiteSpace(bill.CustomerName))
            {
                BillCustomerBlock.Text = bill.CustomerName.Trim();
                BillCustomerRow.Visibility = Visibility.Visible;
            }
            else
            {
                BillCustomerRow.Visibility = Visibility.Collapsed;
            }

            // Items
            ReceiptItemsList.ItemsSource = await _saleService.GetSaleItemsAsync(bill.Id);

            // Totals
            SubtotalBlock.Text = $"{bill.SubTotal:N2}";

            if (bill.TADiscountAmt > 0)
            {
                DiscountLabelBlock.Text = $"Discount ({bill.TADiscountPer:N1}%)";
                DiscountBlock.Text = $"-{bill.TADiscountAmt:N2}";
                DiscountRow.Visibility = Visibility.Visible;
            }
            else
            {
                DiscountRow.Visibility = Visibility.Collapsed;
            }

            if (bill.TotalTaxAmount > 0)
            {
                VatBlock.Text = $"{bill.TotalTaxAmount:N2}";
                VatRow.Visibility = Visibility.Visible;
            }
            else
            {
                VatRow.Visibility = Visibility.Collapsed;
            }

            TotalBlock.Text = $"{bill.GrandTotal:N2}";
            PaymentModeBlock.Text = bill.PaymentMode?.Trim() ?? "";

            if (bill.PaymentMode?.Trim() == "Cash")
            {
                CashBlock.Text = $"{bill.Cash:N2}";
                ChangeBlock.Text = $"{bill.Change:N2}";
                CashRow.Visibility = Visibility.Visible;
                ChangeRow.Visibility = Visibility.Visible;
            }
            else
            {
                CashRow.Visibility = Visibility.Collapsed;
                ChangeRow.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearReceiptDetails()
        {
            ReceiptHeaderText.Text = "";
            ReceiptTotalText.Text = "";
            BillNoBlock.Text = "";
            BillDateBlock.Text = "";
            BillCashierBlock.Text = "";
            BillCustomerRow.Visibility = Visibility.Collapsed;
            ReceiptItemsList.ItemsSource = null;
            SubtotalBlock.Text = "";
            DiscountRow.Visibility = Visibility.Collapsed;
            VatRow.Visibility = Visibility.Collapsed;
            TotalBlock.Text = "";
            PaymentModeBlock.Text = "";
            CashRow.Visibility = Visibility.Collapsed;
            ChangeRow.Visibility = Visibility.Collapsed;
        }

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBillId == null)
            {
                MessageBox.Show("Select a sale first.", "Clidapos");
                return;
            }

            await _receiptService.PrintReceiptAsync(_selectedBillId.Value);
            await _logService.LogAsync(CurrentSession.UserId, $"Printed receipt for Sale ID {_selectedBillId}");
        }

        // ---------------- VOID / REFUND ----------------
        private void StartVoid_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBillId == null)
            {
                MessageBox.Show("Select a sale first.", "Clidapos");
                return;
            }

            ReceiptActionsPanel.Visibility = Visibility.Collapsed;
            VoidReasonPanel.Visibility = Visibility.Visible;
            VoidReasonInput.Clear();
            VoidErrorText.Text = "";
            VoidReasonInput.Focus();
        }

        private async void ConfirmVoid_Click(object sender, RoutedEventArgs e)
        {
            VoidErrorText.Text = "";

            if (_selectedBillId == null)
            {
                VoidErrorText.Text = "Select a sale first.";
                return;
            }

            var reason = VoidReasonInput.Text.Trim();
            if (reason.Length == 0)
            {
                VoidErrorText.Text = "A reason is required.";
                return;
            }

            var result = await _saleService.VoidSaleAsync(_selectedBillId.Value, reason, _currentUser);

            if (!result.Ok)
            {
                VoidErrorText.Text = result.Error;
                return;
            }

            await _logService.LogAsync(CurrentSession.UserId,
                $"Voided Sale (Bill ID {_selectedBillId}) - Reason: {reason}");

            var message = "Sale voided and stock restored.";
            if (result.StockWarnings.Count > 0)
                message += "\n\n" + string.Join("\n", result.StockWarnings);

            MessageBox.Show(message, "Clidapos");

            _selectedBillId = null;
            ClearReceiptDetails();
            ResetVoidUI();
            await LoadSales();
        }

        private void CancelVoid_Click(object sender, RoutedEventArgs e) => ResetVoidUI();

        private void ResetVoidUI()
        {
            ReceiptActionsPanel.Visibility = Visibility.Visible;
            VoidReasonPanel.Visibility = Visibility.Collapsed;
            VoidReasonInput.Clear();
            VoidErrorText.Text = "";
        }

        // ---------------- CHROME ----------------
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            new SalesView(_currentUser).Show();
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Close Clidapos?", "Confirm Exit",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
    }
}
