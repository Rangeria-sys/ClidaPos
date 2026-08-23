using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class PromotionListView : Window
    {
        private readonly Registration _currentUser;
        private readonly PromotionService _promotionService = new();
        private List<Promotion> _all = new();

        public PromotionListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _promotionService.GetAllAsync();
            PromotionGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            PromotionGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(p => (p.Dish ?? "").ToLower().Contains(q)
                               || (p.PDay ?? "").ToLower().Contains(q)).ToList();
        }

        private async void PromotionGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PromotionGrid.SelectedItem is Promotion promotion)
            {
                var popup = new PromotionPopup(promotion) { Owner = this };
                popup.ShowDialog();
                await LoadData();
            }
        }

        private async void NewPromotion_Click(object sender, RoutedEventArgs e)
        {
            var popup = new PromotionPopup { Owner = this };
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