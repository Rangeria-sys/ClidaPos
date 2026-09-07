using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class PayrollListView : Window
    {
        private readonly Registration _currentUser;
        private readonly PayrollService _payrollService = new();
        private readonly EmployeeRegistrationService _employeeService = new();
        private List<PayrollHistoryRow> _all = new();

        public PayrollListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _payrollService.GetRecentAsync();
            HistoryGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var term = SearchBox.Text.Trim();
            HistoryGrid.ItemsSource = string.IsNullOrEmpty(term)
                ? _all
                : _all.Where(p =>
                    p.EmployeeName.Contains(term, System.StringComparison.OrdinalIgnoreCase) ||
                    p.EmployeeID.Contains(term, System.StringComparison.OrdinalIgnoreCase) ||
                    p.NationalID.Contains(term, System.StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var picker = new PayrollEmployeePickerView(_currentUser);
            picker.Show();
            Close();
        }

        private async void HistoryGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HistoryGrid.SelectedItem is not PayrollHistoryRow row) return;

            var run = await _payrollService.GetByIdAsync(row.Id);
            if (run == null)
            {
                MessageBox.Show("That payroll run could not be found - it may have already been removed.", "Clidapos");
                return;
            }

            var employees = await _employeeService.GetAllAsync();
            var employee = employees.FirstOrDefault(emp => emp.EmpId == row.EmpId);
            if (employee == null)
            {
                MessageBox.Show("The employee for this payroll run could not be found.", "Clidapos");
                return;
            }

            var popup = new PayrollPopup(_currentUser, employee, run);
            popup.Show();
            Close();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var backOffice = new BackOfficeView(_currentUser);
            backOffice.Show();
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
