using System.Windows;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SMSSettingListView : Window
    {
        private readonly IntegrationSettingsService _settingsService = new();

        public SMSSettingListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            GatewayGrid.ItemsSource = await _settingsService.GetAllSMSAsync();
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

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}