using System.Linq;
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

            CurrentSession.UserId = currentUser.UserID.Trim();

            AutoBackupScheduler.EnsureStarted();

            StockTransferTile.Visibility = AppSettings.Mode == StoreMode.Restaurant
                ? Visibility.Visible
                : Visibility.Collapsed;

            Loaded += async (s, e) => await ApplyManagerRestrictionsAsync();
        }

        /// <summary>Maps each tile's Tag to its User Security Roles module
        /// name. "MasterSetting" and "UserRoles" are deliberately left out of
        /// this map entirely - they're never granted via rights for Manager,
        /// they're hard-blocked below regardless of what's configured.
        /// "RegisterBanks" (bank branches) falls under the same "Finance &amp;
        /// Banking" module as the main Finance tile, since they're part of
        /// the same area of the business.</summary>
        private static readonly System.Collections.Generic.Dictionary<string, string> TileModuleMap = new()
        {
            ["CategoryUnit"] = "Category Setup",
            ["Items"] = "Items",
            ["PurchaseEntry"] = "Purchase Entry",
            ["StockLevels"] = "Stock Levels",
            ["StockAdjustment"] = "Stock Adjustment",
            ["StockTransfer"] = "Stock Transfer",
            ["Supplier"] = "Supplier",
            ["Warehouse"] = "Warehouse Management",
            ["EmployeeRegistration"] = "Employee Registration",
            ["HR"] = "HR & Payroll",
            ["Customers"] = "Customers",
            ["CustomerLedger"] = "Customer Ledger",
            ["SupplierLedger"] = "Supplier Ledger",
            ["Expenses"] = "Expense Log",
            ["ExpenseMaster"] = "Expense Master",
            ["Finance"] = "Finance & Banking",
            ["RegisterBanks"] = "Finance & Banking",
            ["Loyalty"] = "Loyalty & Membership",
            ["Vouchers"] = "Vouchers & Promotions",
            ["SalesReports"] = "Sales Reports",
            ["StockReports"] = "Stock Reports",
            ["PurchaseReports"] = "Purchase Reports",
            ["ExpenseReports"] = "Expense Reports",
            ["AccountingReports"] = "Accounting Reports",
            ["Logs"] = "System Logs",
            ["Profile"] = "Business Profile",
            ["Backup"] = "Backup Setting"
        };

        private async System.Threading.Tasks.Task ApplyManagerRestrictionsAsync()
        {
            // Super Admin and Admin always see every tile they're otherwise
            // entitled to (Admin still excluded from Master Setting via
            // Nav_Click itself) - only Manager gets filtered per-module here.
            if (!PermissionService.IsManager(_currentUser)) return;

            var rights = await new UserRightsService().GetForUserAsync(_currentUser.UserID.Trim());
            var allowedModules = new System.Collections.Generic.HashSet<string>(
                rights.Where(r => r.UR_View == true).Select(r => r.ModuleName ?? ""));

            foreach (var button in FindVisualChildren<Button>(this))
            {
                if (button.Tag is not string tag) continue;

                // Hard-blocked for Manager no matter what - never even
                // considered against their configured rights.
                if (tag == "MasterSetting" || tag == "UserRoles")
                {
                    button.Visibility = Visibility.Collapsed;
                    continue;
                }

                if (!TileModuleMap.TryGetValue(tag, out var moduleName)) continue;

                button.Visibility = allowedModules.Contains(moduleName)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) yield return typed;
                foreach (var grandchild in FindVisualChildren<T>(child)) yield return grandchild;
            }
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
                if (!PermissionService.CanAccessMasterSetting(_currentUser))
                {
                    MessageBox.Show("Master Setting is restricted to Super Admin only.", "Clidapos", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

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
                if (!PermissionService.IsAdmin(_currentUser) && !PermissionService.IsSuperAdmin(_currentUser))
                {
                    MessageBox.Show("User Security Roles is restricted to Admin and Super Admin only.", "Clidapos", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

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
                var creditCustomerPopup = new CreditCustomerPopup(_currentUser) { Owner = this };
                creditCustomerPopup.Show();
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

            if (tag == "RegisterBanks")
            {
                var bankBranchView = new BankBranchListView(_currentUser);
                bankBranchView.Show();
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