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

            if (tag == "MasterSetting")
            {
                var masterSettingsView = new MasterSettingsView(_currentUser);
                masterSettingsView.Show();
                Close();
                return;
            }

            var sectionName = tag switch
            {
                "Profile" => "Profile Setting",
                "UserRoles" => "User Security Roles",
                "Backup" => "Backup Setting",
                "PurchaseEntry" => "Purchase Entry",
                "StockLevels" => "Stock Levels",
                "StockAdjustment" => "Stock Adjustment",
                "Warehouse" => "Warehouse Management",
                "StockTransfer" => "Stock Transfer",
                "Supplier" => "Supplier",
                "HR" => "HR & Payroll",
                "Customers" => "Customer Ledger",
                "SupplierLedger" => "Supplier Ledger",
                "Expenses" => "Expense Log",
                "ExpenseMaster" => "Expense",
                "Finance" => "Finance & Banking",
                "Loyalty" => "Loyalty & Membership",
                "Vouchers" => "Vouchers & Promotions",
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

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}