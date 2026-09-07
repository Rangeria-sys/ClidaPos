using System.Linq;
using System.Windows;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class PurchaseHistoryListView : Window
    {
        private readonly Registration _currentUser;
        private readonly PurchaseService _purchaseService = new();
        private System.Collections.Generic.List<PurchaseHistoryRow> _all = new();

        public PurchaseHistoryListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadHistory();
        }

        private async System.Threading.Tasks.Task LoadHistory()
        {
            _all = await _purchaseService.GetHistoryAsync();
            HistoryGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var term = SearchBox.Text.Trim();
            HistoryGrid.ItemsSource = string.IsNullOrEmpty(term)
                ? _all
                : _all.Where(p =>
                    p.InvoiceNo.Contains(term, System.StringComparison.OrdinalIgnoreCase) ||
                    p.SupplierName.Contains(term, System.StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private async void HistoryGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HistoryGrid.SelectedItem is not PurchaseHistoryRow row) return;

            var lines = await _purchaseService.GetPurchaseLinesAsync(row.PurchaseId);

            if (lines.Count == 0)
            {
                MessageBox.Show("No line items found for this purchase.", "Clidapos");
                return;
            }

            var detail = new PurchaseDetailView(row, lines);
            detail.ShowDialog();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var view = new PurchaseEntryView(_currentUser);
            view.Show();
            Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
