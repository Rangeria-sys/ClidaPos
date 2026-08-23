using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class PayrollListView : Window
    {
        private readonly Registration _currentUser;
        private readonly PayrollService _payrollService = new();

        public PayrollListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            HistoryGrid.ItemsSource = await _payrollService.GetRecentAsync();
        }

        private async void RunPayroll_Click(object sender, RoutedEventArgs e)
        {
            var popup = new PayrollPopup { Owner = this };
            popup.ShowDialog();
            await LoadData();
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