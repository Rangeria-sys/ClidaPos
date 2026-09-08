using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class CustomerAccountPopup : Window
    {
        private readonly CreditCustomerService _customerService = new();
        private List<CreditCustomer> _all = new();

        /// <summary>The customer the user picked, if they picked one and clicked
        /// through (DialogResult == true). Null otherwise.</summary>
        public CreditCustomer? Selected { get; private set; }

        public CustomerAccountPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                _all = await _customerService.GetAllAsync();
                ResultsList.ItemsSource = _all;
                SearchBox.Focus();
            };
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            ResultsList.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(c => (c.Name ?? "").ToLower().Contains(q)
                               || (c.ContactNo ?? "").ToLower().Contains(q)).ToList();
        }

        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsList.SelectedItem is CreditCustomer customer)
            {
                Selected = customer;
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
