using System.Windows;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class EmailSettingListView : Window
    {
        private readonly IntegrationSettingsService _settingsService = new();

        public EmailSettingListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            ServerGrid.ItemsSource = await _settingsService.GetAllEmailAsync();
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

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}