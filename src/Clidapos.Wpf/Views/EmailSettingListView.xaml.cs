using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class EmailSettingListView : Window
    {
        private readonly IntegrationSettingsService _settingsService = new();
        private List<EmailSetting> _all = new();

        public EmailSettingListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _settingsService.GetAllEmailAsync();
            ApplyFilter();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            var q = SearchBox.Text.Trim().ToLower();
            ServerGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(x =>
                    (x.ServerName ?? "").ToLower().Contains(q) ||
                    (x.SMTPAddress ?? "").ToLower().Contains(q)).ToList();
        }

        private async void AddServer_Click(object sender, RoutedEventArgs e)
        {
            var popup = new EmailSettingPopup { Owner = this };
            popup.ShowDialog();
            await LoadData();
        }

        private async void ServerGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ServerGrid.SelectedItem is EmailSetting setting)
            {
                var popup = new EmailSettingPopup(setting) { Owner = this };
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
