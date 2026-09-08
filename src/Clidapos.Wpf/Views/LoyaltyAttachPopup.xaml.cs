using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class LoyaltyAttachPopup : Window
    {
        private readonly LoyaltyService _loyaltyService = new();
        private List<LoyaltyMemberRow> _all = new();

        /// <summary>The member the user picked, if they picked one and clicked
        /// through (DialogResult == true). Null otherwise.</summary>
        public LoyaltyMemberRow? Selected { get; private set; }

        public LoyaltyAttachPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                _all = await _loyaltyService.GetMemberBalancesAsync();
                // Nothing shows until the cashier actually searches - a
                // phone number or card number the customer has given them,
                // not a browsable full list of every member's details.
                SearchBox.Focus();
            };
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            ResultsList.ItemsSource = string.IsNullOrEmpty(q)
                ? null
                : _all.Where(m => (m.ContactNo ?? "").ToLower().Contains(q)
                               || (m.CardNo ?? "").ToLower().Contains(q)).ToList();
        }

        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsList.SelectedItem is LoyaltyMemberRow member)
            {
                Selected = member;
                DialogResult = true;
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
