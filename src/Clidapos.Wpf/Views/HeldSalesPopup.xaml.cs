using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class HeldSalesPopup : Window
    {
        private readonly HeldSaleService _service = new();
        private List<HeldSaleWithItems> _all = new();

        /// <summary>Set when the user double-clicks a held sale to resume it.
        /// SalesView reads this after ShowDialog() returns.</summary>
        public HeldSaleWithItems? Resumed { get; private set; }

        public HeldSalesPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _service.GetAllAsync();
            HeldGrid.ItemsSource = _all;

            var hasAny = _all.Count > 0;
            ListBorder.Visibility = hasAny ? Visibility.Visible : Visibility.Collapsed;
            DeleteButton.Visibility = hasAny ? Visibility.Visible : Visibility.Collapsed;
            EmptyText.Visibility = hasAny ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void HeldGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HeldGrid.SelectedItem is not HeldSaleWithItems selected) return;

            Resumed = selected;
            await _service.DeleteAsync(selected.Sale.Id);
            DialogResult = true;
            Close();
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (HeldGrid.SelectedItem is not HeldSaleWithItems selected)
            {
                ErrorText.Text = "Select a held sale first.";
                return;
            }

            var confirm = MessageBox.Show(
                $"Discard this held sale ({selected.Items.Count} item(s))? This cannot be undone.",
                "Confirm Discard", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _service.DeleteAsync(selected.Sale.Id);
            await LoadData();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}