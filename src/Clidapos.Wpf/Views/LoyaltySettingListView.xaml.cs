using System.Windows;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class LoyaltySettingListView : Window
    {
        private readonly LoyaltyService _loyaltyService = new();

        public LoyaltySettingListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            RuleGrid.ItemsSource = await _loyaltyService.GetAllSettingsAsync();
        }

        private async void AddRule_Click(object sender, RoutedEventArgs e)
        {
            var popup = new LoyaltySettingPopup { Owner = this };
            popup.ShowDialog();
            await LoadData();
        }

        private async void RuleGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RuleGrid.SelectedItem is LoyaltySetting setting)
            {
                var popup = new LoyaltySettingPopup(setting) { Owner = this };
                popup.ShowDialog();
                await LoadData();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}