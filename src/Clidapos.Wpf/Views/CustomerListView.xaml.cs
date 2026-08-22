using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class CustomerListView : Window
    {
        private readonly CustomerService _customerService = new();
        private List<Customer> _all = new();

        public CustomerListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _customerService.GetAllAsync();
            CustomerGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            CustomerGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(c => c.Name.Trim().ToLower().Contains(q)
                               || c.CustomerID.Trim().ToLower().Contains(q)).ToList();
        }

        private void CustomerGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CustomerGrid.SelectedItem is Customer customer)
            {
                var popup = new CustomerPopup(customer);
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