using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class EmployeeRegistrationListView : Window
    {
        private readonly EmployeeRegistrationService _employeeService = new();
        private List<EmployeeRegistration> _all = new();

        public EmployeeRegistrationListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _employeeService.GetAllAsync();
            EmployeeGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            EmployeeGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(emp => emp.EmployeeName.Trim().ToLower().Contains(q)
                                  || emp.EmployeeID.Trim().ToLower().Contains(q)
                                  || emp.City.Trim().ToLower().Contains(q)).ToList();
        }

        private void EmployeeGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EmployeeGrid.SelectedItem is EmployeeRegistration employee)
            {
                var popup = new EmployeeRegistrationPopup(employee);
                popup.Show();
                Close();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
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