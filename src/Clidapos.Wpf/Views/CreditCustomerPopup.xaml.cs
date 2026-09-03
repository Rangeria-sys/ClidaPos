using System;
using System.Text.RegularExpressions;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class CreditCustomerPopup : Window
    {
        private readonly Registration _currentUser;
        private readonly CreditCustomerService _service = new();
        private readonly LogService _logService = new();
        private CreditCustomer? _editing;

        public CreditCustomerPopup(Registration currentUser, CreditCustomer? editCustomer = null)
        {
            InitializeComponent();
            _currentUser = currentUser;

            if (editCustomer != null)
            {
                _editing = editCustomer;
                CodeInput.Text = editCustomer.CreditCustomerID?.Trim() ?? "";
                NameInput.Text = editCustomer.Name?.Trim() ?? "";
                ContactInput.Text = editCustomer.ContactNo?.Trim() ?? "";
                EmailInput.Text = editCustomer.EmailID?.Trim() ?? "";
                AddressInput.Text = editCustomer.Address?.Trim() ?? "";
                OpeningBalanceInput.Text = editCustomer.OpeningBalance?.ToString() ?? "0";
                ActiveInput.Text = editCustomer.Active?.Trim() ?? "Y";
            }
            else
            {
                ActiveInput.Text = "Y";
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            CodeInput.Text = "";
            NameInput.Text = "";
            ContactInput.Text = "";
            EmailInput.Text = "";
            AddressInput.Text = "";
            OpeningBalanceInput.Text = "0";
            ActiveInput.Text = "Y";
            ErrorText.Text = "";
            NameInput.Focus();
        }

        // Kenyan format: exactly 10 digits, starting with 07 (e.g. 0712345678).
        private static bool IsValidPhone(string phone) =>
            Regex.IsMatch(phone.Trim(), @"^07\d{8}$");

        private static bool IsValidEmail(string email) =>
            Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        private bool TryBuildCustomer(out CreditCustomer customer)
        {
            customer = new CreditCustomer();
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                ErrorText.Text = "Name is required.";
                return false;
            }

            var phone = ContactInput.Text.Trim();
            if (phone.Length > 0 && !IsValidPhone(phone))
            {
                ErrorText.Text = "Contact No must be exactly 10 digits starting with 07 (e.g. 0712345678).";
                return false;
            }

            var email = EmailInput.Text.Trim();
            if (email.Length > 0 && !IsValidEmail(email))
            {
                ErrorText.Text = "Enter a valid email address (e.g. name@domain.com).";
                return false;
            }

            if (!decimal.TryParse(OpeningBalanceInput.Text.Trim(), out var opening) || opening < 0)
            {
                ErrorText.Text = "Opening Balance must be a valid amount of 0 or more.";
                return false;
            }

            customer.CreditCustomerID = CodeInput.Text.Trim();
            customer.Name = NameInput.Text.Trim();
            customer.ContactNo = phone;
            customer.EmailID = email;
            customer.Address = AddressInput.Text.Trim();
            customer.OpeningBalance = opening;
            customer.Active = ActiveInput.Text.Trim();
            return true;
        }

        // Save only ever creates a genuinely new customer. If a record is already
        // loaded for editing, Save refuses outright - use Update instead - so a
        // stray click can never silently split someone's history into two records.
        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_editing != null)
            {
                ErrorText.Text = $"{NameInput.Text.Trim()} is already an existing customer - click Update to save changes, or New to start a fresh registration.";
                return;
            }

            if (!TryBuildCustomer(out var customer)) return;

            try
            {
                await _service.AddAsync(customer);
                await _logService.LogAsync(CurrentSession.UserId, $"Added Credit Customer '{customer.Name}'");
                New_Click(sender, e);
                ErrorText.Text = "Saved.";
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.InnerException?.Message ?? ex.Message;
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a customer, then edit and Update.";
                return;
            }
            if (!TryBuildCustomer(out var customer)) return;
            customer.CC_ID = _editing.CC_ID;

            try
            {
                await _service.UpdateAsync(customer);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Credit Customer '{customer.Name}'");
                ErrorText.Text = "Updated.";
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.InnerException?.Message ?? ex.Message;
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a customer, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove credit customer '{_editing.Name?.Trim()}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _service.DeleteAsync(_editing.CC_ID);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Credit Customer '{_editing.Name?.Trim()}'");
            New_Click(sender, e);
            ErrorText.Text = "Removed.";
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new CreditCustomerListView(_currentUser);
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}