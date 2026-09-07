using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class PayrollEmployeePickerView : Window
    {
        private readonly Registration _currentUser;
        private readonly EmployeeRegistrationService _employeeService = new();
        private List<EmployeeRegistration> _all = new();

        public PayrollEmployeePickerView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _employeeService.GetAllAsync();
            EmployeeGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var term = SearchBox.Text.Trim();
            EmployeeGrid.ItemsSource = string.IsNullOrEmpty(term)
                ? _all
                : _all.Where(emp =>
                    emp.EmployeeName.Trim().Contains(term, System.StringComparison.OrdinalIgnoreCase) ||
                    emp.EmployeeID.Trim().Contains(term, System.StringComparison.OrdinalIgnoreCase) ||
                    emp.City.Trim().Contains(term, System.StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private void EmployeeGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EmployeeGrid.SelectedItem is not EmployeeRegistration employee) return;

            var payrollPopup = new PayrollPopup(_currentUser, employee);
            payrollPopup.Show();
            Close();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var payrollList = new PayrollListView(_currentUser);
            payrollList.Show();
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
