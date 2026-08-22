using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class CustomerPopup : Window
    {
        private readonly CustomerService _customerService = new();
        private readonly LogService _logService = new();
        private Customer? _editing;

        public CustomerPopup(Customer? editCustomer = null)
        {
            InitializeComponent();

            if (editCustomer != null)
            {
                _editing = editCustomer;
                CodeInput.Text = editCustomer.CustomerID.Trim();
                NameInput.Text = editCustomer.Name.Trim();
                ContactInput.Text = editCustomer.ContactNo?.Trim() ?? "";
                EmailInput.Text = editCustomer.Email?.Trim() ?? "";
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            CodeInput.Text = "";
            NameInput.Text = "";
            ContactInput.Text = "";
            EmailInput.Text = "";
            ErrorText.Text = "";
            NameInput.Focus();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Name is required.";
                return;
            }

            var newId = await _customerService.GetNextIdAsync();
            var code = string.IsNullOrWhiteSpace(CodeInput.Text)
                ? $"CUST-{newId}"
                : CodeInput.Text.Trim();

            var customer = new Customer
            {
                ID = newId,
                CustomerID = code,
                Name = name,
                ContactNo = ContactInput.Text.Trim(),
                Email = EmailInput.Text.Trim()
            };

            try
            {
                await _customerService.AddAsync(customer);
                await _logService.LogAsync(CurrentSession.UserId, $"Added Customer '{name}' ({code})");
                New_Click(sender, e);
                ErrorText.Text = "Saved.";
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a customer, then edit and Update.";
                return;
            }

            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Name is required.";
                return;
            }

            try
            {
                _editing.CustomerID = string.IsNullOrWhiteSpace(CodeInput.Text)
                    ? _editing.CustomerID
                    : CodeInput.Text.Trim();
                _editing.Name = name;
                _editing.ContactNo = ContactInput.Text.Trim();
                _editing.Email = EmailInput.Text.Trim();

                await _customerService.UpdateAsync(_editing);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Customer '{name}'");
                ErrorText.Text = "Updated.";
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a customer, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove customer '{_editing.Name.Trim()}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var deletedName = _editing.Name.Trim();
            await _customerService.DeleteAsync(_editing.ID);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Customer '{deletedName}'");
            New_Click(sender, e);
            ErrorText.Text = "Removed.";
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new CustomerListView();
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}