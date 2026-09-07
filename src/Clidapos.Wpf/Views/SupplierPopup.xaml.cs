using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class SupplierPopup : Window
    {
        private readonly SupplierService _supplierService = new();
        private readonly LogService _logService = new();
        private Supplier? _editing;

        public SupplierPopup(Supplier? editSupplier = null)
        {
            InitializeComponent();

            if (editSupplier != null)
            {
                _editing = editSupplier;
                LoadIntoForm(editSupplier);
            }
        }

        private void LoadIntoForm(Supplier s)
        {
            CodeInput.Text = s.SupplierID.Trim();
            NameInput.Text = (s.Name ?? "").Trim();
            ContactInput.Text = (s.ContactNo ?? "").Trim();
            EmailInput.Text = (s.EmailID ?? "").Trim();
            CityInput.Text = (s.City ?? "").Trim();
            BankInput.Text = (s.Bank ?? "").Trim();
            BranchInput.Text = (s.Branch ?? "").Trim();
            AccountNameInput.Text = (s.AccountName ?? "").Trim();
            AccountNumberInput.Text = (s.AccountNumber ?? "").Trim();
            OpeningBalanceInput.Text = s.OpeningBalance.HasValue ? s.OpeningBalance.Value.ToString("0.00") : "";
            AddressInput.Text = (s.Address ?? "").Trim();
            RemarksInput.Text = (s.Remarks ?? "").Trim();

            var type = (s.OpeningBalanceType ?? "").Trim();
            OpeningBalanceTypeInput.SelectedIndex = type.StartsWith("Dr", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }

        private void ClearForm()
        {
            CodeInput.Text = "";
            NameInput.Text = "";
            ContactInput.Text = "";
            EmailInput.Text = "";
            CityInput.Text = "";
            BankInput.Text = "";
            BranchInput.Text = "";
            AccountNameInput.Text = "";
            AccountNumberInput.Text = "";
            OpeningBalanceInput.Text = "";
            AddressInput.Text = "";
            RemarksInput.Text = "";
            OpeningBalanceTypeInput.SelectedIndex = 0;
        }

        private string SelectedBalanceType =>
            (OpeningBalanceTypeInput.SelectedItem as ComboBoxItem)?.Content?.ToString()?.StartsWith("Dr") == true
                ? "Dr"
                : "Cr";

        /// <summary>Validates the 3 required fields. Returns (name, contact, email) on success, or null (with ErrorText already set) if something's invalid.</summary>
        private (string name, string contact, string email)? ValidateRequired()
        {
            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Supplier Name is required.";
                return null;
            }

            var contact = ContactInput.Text.Trim();
            if (!Regex.IsMatch(contact, @"^07\d{8}$"))
            {
                ErrorText.Text = "Contact No must start with 07 and be exactly 10 digits (e.g. 0712345678).";
                return null;
            }

            var email = EmailInput.Text.Trim();
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorText.Text = "Enter a valid email address.";
                return null;
            }

            return (name, contact, email);
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            ClearForm();
            ErrorText.Text = "";
            NameInput.Focus();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var validated = ValidateRequired();
            if (validated == null) return;
            var (name, contact, email) = validated.Value;

            decimal? openingBalance = null;
            if (!string.IsNullOrWhiteSpace(OpeningBalanceInput.Text))
            {
                if (!decimal.TryParse(OpeningBalanceInput.Text, out var ob))
                {
                    ErrorText.Text = "Opening Balance must be a valid number.";
                    return;
                }
                openingBalance = ob;
            }

            var newId = await _supplierService.GetNextIdAsync();
            var code = string.IsNullOrWhiteSpace(CodeInput.Text)
                ? $"SUPP-{newId}"
                : CodeInput.Text.Trim();

            var supplier = new Supplier
            {
                ID = newId,
                SupplierID = code,
                Name = name,
                ContactNo = contact,
                EmailID = email,
                City = CityInput.Text.Trim(),
                Bank = BankInput.Text.Trim(),
                Branch = BranchInput.Text.Trim(),
                AccountName = AccountNameInput.Text.Trim(),
                AccountNumber = AccountNumberInput.Text.Trim(),
                OpeningBalance = openingBalance,
                OpeningBalanceType = SelectedBalanceType,
                Address = AddressInput.Text.Trim(),
                Remarks = RemarksInput.Text.Trim()
            };

            await _supplierService.AddAsync(supplier);
            await _logService.LogAsync(CurrentSession.UserId, $"Added Supplier '{name}' ({code})");
            _editing = null;
            ClearForm();
            MessageBox.Show("Saved.", "Clidapos");
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a supplier, then edit and Update.";
                return;
            }

            var validated = ValidateRequired();
            if (validated == null) return;
            var (name, contact, email) = validated.Value;

            decimal? openingBalance = null;
            if (!string.IsNullOrWhiteSpace(OpeningBalanceInput.Text))
            {
                if (!decimal.TryParse(OpeningBalanceInput.Text, out var ob))
                {
                    ErrorText.Text = "Opening Balance must be a valid number.";
                    return;
                }
                openingBalance = ob;
            }

            var confirm = MessageBox.Show($"Update supplier '{_editing.Name?.Trim()}'?",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                _editing.SupplierID = string.IsNullOrWhiteSpace(CodeInput.Text)
                    ? _editing.SupplierID
                    : CodeInput.Text.Trim();
                _editing.Name = name;
                _editing.ContactNo = contact;
                _editing.EmailID = email;
                _editing.City = CityInput.Text.Trim();
                _editing.Bank = BankInput.Text.Trim();
                _editing.Branch = BranchInput.Text.Trim();
                _editing.AccountName = AccountNameInput.Text.Trim();
                _editing.AccountNumber = AccountNumberInput.Text.Trim();
                _editing.OpeningBalance = openingBalance;
                _editing.OpeningBalanceType = SelectedBalanceType;
                _editing.Address = AddressInput.Text.Trim();
                _editing.Remarks = RemarksInput.Text.Trim();

                await _supplierService.UpdateAsync(_editing);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Supplier '{name}'");
                MessageBox.Show("Updated.", "Clidapos");
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
                ErrorText.Text = "Use Get Data, pick a supplier, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove supplier '{_editing.Name?.Trim()}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var deletedName = _editing.Name?.Trim() ?? "";
            await _supplierService.DeleteAsync(_editing.ID);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Supplier '{deletedName}'");
            _editing = null;
            ClearForm();
            MessageBox.Show("Removed.", "Clidapos");
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new SupplierListView();
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
