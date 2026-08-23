using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class LoyaltyMemberListView : Window
    {
        private readonly LoyaltyService _loyaltyService = new();
        private List<LoyaltyMember> _all = new();

        public LoyaltyMemberListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _loyaltyService.GetAllMembersAsync();
            MemberGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            MemberGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(m => (m.Name ?? "").ToLower().Contains(q)
                               || m.MemberID.ToString().Contains(q)
                               || (m.CardNo ?? "").ToLower().Contains(q)).ToList();
        }

        private void MemberGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MemberGrid.SelectedItem is LoyaltyMember member)
            {
                var popup = new LoyaltyMemberPopup(member);
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