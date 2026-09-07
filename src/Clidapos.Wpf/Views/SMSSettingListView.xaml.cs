using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SMSSettingListView : Window
    {
        private readonly IntegrationSettingsService _settingsService = new();
        private List<SMSSetting> _all = new();

        public SMSSettingListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _settingsService.GetAllSMSAsync();
            ApplyFilter();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            var q = SearchBox.Text.Trim().ToLower();
            GatewayGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(x => (x.IsDefault ?? "").ToLower().Contains(q) || (x.IsEnabled ?? "").ToLower().Contains(q)).ToList();
        }

        private async void AddGateway_Click(object sender, RoutedEventArgs e)
        {
            var popup = new SMSSettingPopup { Owner = this };
            popup.ShowDialog();
            await LoadData();
        }

        private async void GatewayGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GatewayGrid.SelectedItem is SMSSetting setting)
            {
                var popup = new SMSSettingPopup(setting) { Owner = this };
                popup.ShowDialog();
                await LoadData();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e) => Close();

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
