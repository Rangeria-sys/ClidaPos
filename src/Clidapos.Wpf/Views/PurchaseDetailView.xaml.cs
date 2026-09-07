using System.Collections.Generic;
using System.Media;
using System.Windows;
using System.Windows.Input;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class PurchaseDetailView : Window
    {
        public PurchaseDetailView(PurchaseHistoryRow purchase, List<PurchaseLineHistoryRow> lines)
        {
            InitializeComponent();

            SubtitleText.Text = "Read-only - a record of what was received";
            InvoiceText.Text = purchase.InvoiceNo;
            DateText.Text = purchase.Date.ToString("dd MMM yyyy, hh:mm tt");
            SupplierText.Text = purchase.SupplierName;
            GrandTotalText.Text = $"{purchase.GrandTotal:N2}";
            LinesGrid.ItemsSource = lines;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                SystemSounds.Exclamation.Play();
            }
        }
    }
}
