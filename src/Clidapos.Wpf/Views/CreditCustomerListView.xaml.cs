using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class CreditCustomerListView : Window
    {
        private readonly Registration _currentUser;
        private readonly CreditCustomerService _service = new();

        public CreditCustomerListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            CustomerGrid.ItemsSource = await _service.GetAllAsync();
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CustomerGrid.ItemsSource = await _service.SearchAsync(SearchBox.Text);
        }

        private async void CustomerGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CustomerGrid.SelectedItem is CreditCustomer customer)
            {
                var popup = new CreditCustomerPopup(_currentUser, customer) { Owner = this };
                popup.ShowDialog();
                await LoadData();
            }
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