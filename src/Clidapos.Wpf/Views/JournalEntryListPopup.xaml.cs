using System.Windows;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class JournalEntryListPopup : Window
    {
        private readonly AccountingService _accountingService = new();

        public JournalEntryListPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) => EntryGrid.ItemsSource = await _accountingService.GetAllJournalEntriesAsync();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}