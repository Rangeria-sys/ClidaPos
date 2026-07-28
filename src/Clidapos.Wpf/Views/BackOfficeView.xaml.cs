using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class BackOfficeView : Window
    {
        private readonly Registration _currentUser;

        public BackOfficeView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            WelcomeText.Text = $"{currentUser.Name.Trim()} ({currentUser.UserType.Trim()})";

            TableLayoutTile.Visibility = AppSettings.Mode == StoreMode.Restaurant
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            var tag = ((Button)sender).Tag?.ToString() ?? "";

            if (tag == "Items")
            {
                var itemsView = new ItemsView(_currentUser);
                itemsView.Show();
                Close();
                return;
            }

            if (tag == "CategoryUnit")
            {
                var categoryUnitView = new CategoryUnitView(_currentUser);
                categoryUnitView.Show();
                Close();
                return;
            }

            if (tag == "MasterSettings")
            {
                MessageBox.Show(
                    "Master Settings — store/restaurant name, address, logo, currency & VAT rate.\n\nComing soon.",
                    "Clidapos");
                return;
            }

            var sectionName = tag switch
            {
                "Peripherals" => "Peripherals & Hardware",
                "UserRoles" => "User Security Roles",
                "BackupRestore" => "Backup & Restore",
                "License" => "License & Activation",
                "TableLayout" => "Table Layout Setup",

                "StockLevels" => "Stock Levels",
                "Warehouse" => "Warehouse Management",
                "Suppliers" => "Suppliers",
                "PurchaseEntry" => "Purchase Entry",
                "StockTransfer" => "Stock Transfer",
                "StockAdjustment" => "Stock Adjustment",

                "HR" => "HR & Payroll",
                "CustomerLedger" => "Customer Ledger",
                "SupplierLedger" => "Supplier Ledger",
                "Expenses" => "Expense Log",
                "Finance" => "Finance & Banking",
                "Loyalty" => "Loyalty & Membership",
                "Vouchers" => "Vouchers, Gift Cards & Promotions",

                "SalesReports" => "Sales Reports",
                "StockReports" => "Stock / Item Reports",
                "PurchaseReports" => "Purchase Reports",
                "ExpenseReports" => "Expense Reports",
                "AccountingReports" => "Accounting Reports",
                "Logs" => "System Logs",

                _ => "Unknown section"
            };

            MessageBox.Show($"{sectionName} — coming soon.", "Clidapos");
        }

        private void BackToGateway_Click(object sender, RoutedEventArgs e)
        {
            var gateway = new GatewayView(_currentUser);
            gateway.Show();
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}