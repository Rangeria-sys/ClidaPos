using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SalesHistoryView : Window
    {
        private readonly Registration _currentUser;
        private readonly SaleService _saleService = new();
        private readonly LogService _logService = new();
        private int? _selectedBillId;

        public SalesHistoryView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadSales();
        }

        private async System.Threading.Tasks.Task LoadSales()
        {
            SalesGrid.ItemsSource = await _saleService.GetRecentSalesAsync();
        }

        private async void SalesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetVoidUI();

            if (SalesGrid.SelectedItem is not SaleBill bill)
            {
                _selectedBillId = null;
                ReceiptHeaderText.Text = "";
                ReceiptTotalText.Text = "";
                ReceiptItemsList.ItemsSource = null;
                return;
            }

            _selectedBillId = bill.Id;

            ReceiptHeaderText.Text =
                $"{bill.BillNo}  ·  {bill.BillDate:dd MMM yyyy, hh:mm tt}  ·  {bill.PaymentMode}" +
                (string.IsNullOrWhiteSpace(bill.CustomerName) ? "" : $"  ·  {bill.CustomerName}");

            ReceiptTotalText.Text = $"{AppSettings.CurrencySymbol} {bill.GrandTotal:N2}";

            ReceiptItemsList.ItemsSource = await _saleService.GetSaleItemsAsync(bill.Id);
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Printing is coming soon - a receipt printer hasn't been configured for this till yet.",
                "Clidapos");
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
            ReceiptHeaderText.Text = "";
            ReceiptTotalText.Text = "";
            ReceiptItemsList.ItemsSource = null;
            ResetVoidUI();

            await LoadSales();
        }

        private void CancelVoid_Click(object sender, RoutedEventArgs e)
        {
            ResetVoidUI();
        }

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

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Close Clidapos?", "Confirm Exit",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
    }
}