using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class LoyaltyLedgerListView : Window
    {
        private readonly Registration _currentUser;
        private readonly LoyaltyService _loyaltyService = new();
        private List<LoyaltyMemberRow> _all = new();

        public LoyaltyLedgerListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _loyaltyService.GetMemberBalancesAsync();
            MemberGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            MemberGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(m => m.Name.ToLower().Contains(q)
                               || m.MemberID.ToString().Contains(q)
                               || m.CardNo.ToLower().Contains(q)).ToList();
        }

        private void MemberGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MemberGrid.SelectedItem is LoyaltyMemberRow row)
            {
                // Show() is non-blocking - reopen this screen (or re-search) after
                // recording points activity to see the updated balance here.
                var detail = new LoyaltyLedgerDetailPopup(row.MemberID, row.Name);
                detail.Show();
            }
        }

        private async void RegisterMember_Click(object sender, RoutedEventArgs e)
        {
            var popup = new LoyaltyMemberPopup { Owner = this };
            popup.ShowDialog();
            await LoadData();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var popup = new LoyaltySettingListView { Owner = this };
            popup.ShowDialog();
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