using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;

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

            var sectionName = tag switch
            {
                "MasterSettings" => "Master Settings",
                "Peripherals" => "Peripherals & Hardware",
                "Items" => "Items & Ingredients Registry",
                "Inventory" => "Inventory Audit",
                "Warehouse" => "Warehouse Management",
                "Purchasing" => "Purchasing & Suppliers",
                "UserRoles" => "User Security Roles",
                "HR" => "HR & Payroll",
                "Customers" => "Customers Ledger",
                "Loyalty" => "Loyalty & Membership",
                "Vouchers" => "Vouchers, Gift Cards & Promotions",
                "Finance" => "Finance & Banking",
                "Expenses" => "Expense Log",
                "TableLayout" => "Table Layout Setup",
                "Reports" => "Reports & Analytics",
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
    }
}