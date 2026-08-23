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

            // Every Back Office screen is reached through here, so this is where
            // the audit-log session gets set for the whole session.
            CurrentSession.UserId = currentUser.UserID.Trim();

            // Safe to call every time - EnsureStarted() does nothing if the
            // background checker is already running from earlier this session.
            AutoBackupScheduler.EnsureStarted();

            // Stock Transfer is restaurant-only for now (its underlying table is
            // built around Warehouse -> Kitchen transfers) - hidden in Supermarket mode,
            // same pattern as Menu Layout and Table Setting in Master Settings.
            StockTransferTile.Visibility = AppSettings.Mode == StoreMode.Restaurant
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
                var categoryTypeSelectView = new CategoryTypeSelectView(_currentUser);
                categoryTypeSelectView.Show();
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

            if (tag == "Supplier")
            {
                var supplierPopup = new SupplierPopup { Owner = this };
                supplierPopup.Show();
                return;
            }

            if (tag == "Warehouse")
            {
                var warehousePopup = new WarehousePopup { Owner = this };
                warehousePopup.Show();
                return;
            }

            if (tag == "PurchaseEntry")
            {
                var purchaseEntryView = new PurchaseEntryView(_currentUser);
                purchaseEntryView.Show();
                Close();
                return;
            }

            if (tag == "StockLevels")
            {
                var stockLevelsView = new StockLevelsView(_currentUser);
                stockLevelsView.Show();
                Close();
                return;
            }

            if (tag == "StockAdjustment")
            {
                var stockAdjustmentPopup = new StockAdjustmentPopup { Owner = this };
                stockAdjustmentPopup.Show();
                return;
            }

            if (tag == "SalesReports")
            {
                var salesReportView = new SalesReportView(_currentUser);
                salesReportView.Show();
                Close();
                return;
            }

            if (tag == "StockReports")
            {
                var stockReportView = new StockReportView(_currentUser);
                stockReportView.Show();
                Close();
                return;
            }

            if (tag == "PurchaseReports")
            {
                var purchaseReportView = new PurchaseReportView(_currentUser);
                purchaseReportView.Show();
                Close();
                return;
            }

            if (tag == "ExpenseMaster")
            {
                var expensePopup = new ExpensePopup { Owner = this };
                expensePopup.Show();
                return;
            }

            if (tag == "Logs")
            {
                var systemLogsView = new SystemLogsView(_currentUser);
                systemLogsView.Show();
                Close();
                return;
            }

            if (tag == "Profile")
            {
                var hotelProfilePopup = new HotelProfilePopup { Owner = this };
                hotelProfilePopup.Show();
                return;
            }

            if (tag == "UserRoles")
            {
                var userRolesPopup = new UserSecurityRolesPopup { Owner = this };
                userRolesPopup.Show();
                return;
            }

            if (tag == "EmployeeRegistration")
            {
                var employeeRegPopup = new EmployeeRegistrationPopup { Owner = this };
                employeeRegPopup.Show();
                return;
            }

            if (tag == "Backup")
            {
                var backupPopup = new BackupSettingPopup { Owner = this };
                backupPopup.Show();
                return;
            }

            if (tag == "SupplierLedger")
            {
                var supplierLedgerView = new SupplierLedgerListView(_currentUser);
                supplierLedgerView.Show();
                Close();
                return;
            }

            if (tag == "Customers")
            {
                var customerPopup = new CustomerPopup { Owner = this };
                customerPopup.Show();
                return;
            }

            if (tag == "CustomerLedger")
            {
                var customerLedgerView = new CustomerLedgerListView(_currentUser);
                customerLedgerView.Show();
                Close();
                return;
            }

            if (tag == "HR")
            {
                var payrollView = new PayrollListView(_currentUser);
                payrollView.Show();
                Close();
                return;
            }

            if (tag == "Finance")
            {
                var bankLedgerView = new BankLedgerListView(_currentUser);
                bankLedgerView.Show();
                Close();
                return;
            }

            if (tag == "Loyalty")
            {
                var loyaltyView = new LoyaltyLedgerListView(_currentUser);
                loyaltyView.Show();
                Close();
                return;
            }

            if (tag == "Vouchers")
            {
                var voucherHubView = new VoucherHubView(_currentUser);
                voucherHubView.Show();
                Close();
                return;
            }

            if (tag == "AccountingReports")
            {
                var accountingReportView = new AccountingReportView(_currentUser);
                accountingReportView.Show();
                Close();
                return;
            }

            if (tag == "ExpenseReports")
            {
                var expenseReportView = new ExpenseReportView(_currentUser);
                expenseReportView.Show();
                Close();
                return;
            }

            if (tag == "Expenses")
            {
                var expenseLogView = new ExpenseLogListView(_currentUser);
                expenseLogView.Show();
                Close();
                return;
            }

            var sectionName = tag switch
            {
                "StockTransfer" => "Stock Transfer",
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