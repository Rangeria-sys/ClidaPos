using System.Windows;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Views
{
    public partial class VoucherHubView : Window
    {
        private readonly Registration _currentUser;

        public VoucherHubView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
        }

        private void Vouchers_Click(object sender, RoutedEventArgs e)
        {
            var voucherView = new VoucherListView(_currentUser);
            voucherView.Show();
            Close();
        }

        private void Promotions_Click(object sender, RoutedEventArgs e)
        {
            var promotionView = new PromotionListView(_currentUser);
            promotionView.Show();
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