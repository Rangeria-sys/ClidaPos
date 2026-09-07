using System;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
                LoadForEditing(editCustomer);
                SetMode(isExisting: true);
            }
            else
            {
                ActiveInput.SelectedIndex = 0;
                BalanceTypeInput.SelectedIndex = 0;
                SetMode(isExisting: false);
            }
        }

        /// <summary>
        /// Save only ever creates a brand-new customer. Once something has been
        /// loaded via Get Data (double-click), only Update can change it.
        /// </summary>
        private void SetMode(bool isExisting)
        {
            SaveBtn.IsEnabled = !isExisting;
            UpdateBtn.IsEnabled = isExisting;
            DeleteBtn.IsEnabled = isExisting;

            // Opening Balance is a one-time starting point, set only when the
            // customer is first registered - letting it be edited afterward would
            // let the running balance be quietly rewritten with no audit trail.
            // If it was genuinely entered wrong, delete the customer and start
            // fresh rather than editing it here.
            OpeningBalanceInput.IsEnabled = !isExisting;
            BalanceTypeInput.IsEnabled = !isExisting;
            OpeningBalanceLabel.Text = isExisting
                ? "Opening Balance (locked - see Adjust below)"
                : "Opening Balance";
            AdjustOpeningBalanceLink.Visibility = isExisting ? Visibility.Visible : Visibility.Collapsed;
            AdjustOpeningBalancePanel.Visibility = Visibility.Collapsed;
        }

        private void LoadForEditing(CreditCustomer customer)
        {
            _editing = customer;
            CodeInput.Text = customer.CreditCustomerID?.Trim() ?? "";
            NameInput.Text = customer.Name?.Trim() ?? "";
            ContactInput.Text = customer.ContactNo?.Trim() ?? "";
            EmailInput.Text = customer.EmailID?.Trim() ?? "";
            AddressInput.Text = customer.Address?.Trim() ?? "";
            OpeningBalanceInput.Text = customer.OpeningBalance?.ToString("N2") ?? "0";
            BalanceTypeInput.SelectedIndex = (customer.OpeningBalanceType?.Trim() ?? "").StartsWith("Cr", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            ActiveInput.Text = customer.Active?.Trim() ?? "Y";
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
            BalanceTypeInput.SelectedIndex = 0;
            ActiveInput.SelectedIndex = 0;
            ErrorText.Text = "";
            SetMode(isExisting: false);
            NameInput.Focus();
        }

        private string SelectedBalanceType =>
            (BalanceTypeInput.SelectedItem as ComboBoxItem)?.Content?.ToString()?.StartsWith("Cr") == true
                ? "Cr"
                : "Dr";

        /// <summary>Validates Name/Contact/Email/Opening Balance. Returns the values on success, or null (with ErrorText already set) if something's invalid.</summary>
        private (string name, string contact, string email, decimal opening)? ValidateRequired()
        {
            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Name is required.";
                return null;
            }

            var phone = ContactInput.Text.Trim();
            if (!Regex.IsMatch(phone, @"^07\d{8}$"))
            {
                ErrorText.Text = "Contact No must be exactly 10 digits starting with 07 (e.g. 0712345678).";
                return null;
            }

            var email = EmailInput.Text.Trim();
            if (email.Length > 0 && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorText.Text = "Enter a valid email address (e.g. name@domain.com).";
                return null;
            }

            if (!decimal.TryParse(OpeningBalanceInput.Text.Trim(), out var opening) || opening < 0)
            {
                ErrorText.Text = "Opening Balance must be a valid amount of 0 or more.";
                return null;
            }

            return (name, phone, email, opening);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            var validated = ValidateRequired();
            if (validated == null) return;
            var (name, phone, email, opening) = validated.Value;

            var existing = await _service.GetAllAsync();
            if (existing.Any(c => (c.Name ?? "").Trim().Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorText.Text = $"A customer named '{name}' already exists.";
                return;
            }

            var customer = new CreditCustomer
            {
                CreditCustomerID = CodeInput.Text.Trim(),
                Name = name,
                ContactNo = phone,
                EmailID = email,
                Address = AddressInput.Text.Trim(),
                OpeningBalance = opening,
                OpeningBalanceType = SelectedBalanceType,
                Active = ActiveInput.Text.Trim()
            };

            try
            {
                await _service.AddAsync(customer);
                await _logService.LogAsync(CurrentSession.UserId, $"Added Credit Customer '{name}'");

                MessageBox.Show("Saved.", "Clidapos");
                New_Click(sender, e);
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.InnerException?.Message ?? ex.Message;
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a customer, then edit and Update.";
                return;
            }

            var validated = ValidateRequired();
            if (validated == null) return;
            var (name, phone, email, opening) = validated.Value;

            var existing = await _service.GetAllAsync();
            var nameTaken = existing.Any(c =>
                c.CC_ID != _editing.CC_ID &&
                (c.Name ?? "").Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
            if (nameTaken)
            {
                ErrorText.Text = $"Another customer is already named '{name}'.";
                return;
            }

            var confirm = MessageBox.Show($"Update customer '{_editing.Name?.Trim()}'?",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var customer = new CreditCustomer
                {
                    CC_ID = _editing.CC_ID,
                    CreditCustomerID = CodeInput.Text.Trim(),
                    Name = name,
                    ContactNo = phone,
                    EmailID = email,
                    Address = AddressInput.Text.Trim(),
                    OpeningBalance = opening,
                    OpeningBalanceType = SelectedBalanceType,
                    Active = ActiveInput.Text.Trim()
                };

                await _service.UpdateAsync(customer);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Credit Customer '{name}'");

                MessageBox.Show("Updated.", "Clidapos");
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

            var deletedName = _editing.Name?.Trim() ?? "";
            await _service.DeleteAsync(_editing.CC_ID);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Credit Customer '{deletedName}'");

            MessageBox.Show("Removed.", "Clidapos");
            New_Click(sender, e);
        }

        private void AdjustOpeningBalanceLink_Click(object sender, RoutedEventArgs e)
        {
            AdjustNewBalanceInput.Text = OpeningBalanceInput.Text;
            AdjustReasonInput.Text = "";
            AdjustOpeningBalancePanel.Visibility = Visibility.Visible;
        }

        private void AdjustOpeningBalanceCancel_Click(object sender, RoutedEventArgs e)
        {
            AdjustOpeningBalancePanel.Visibility = Visibility.Collapsed;
        }

        private async void AdjustOpeningBalanceConfirm_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a customer, then adjust their Opening Balance.";
                return;
            }

            if (!decimal.TryParse(AdjustNewBalanceInput.Text.Trim(), out var newBalance) || newBalance < 0)
            {
                ErrorText.Text = "Enter a valid new Opening Balance of 0 or more.";
                return;
            }

            var reason = AdjustReasonInput.Text.Trim();
            if (string.IsNullOrEmpty(reason))
            {
                ErrorText.Text = "A reason is required for every Opening Balance correction.";
                return;
            }

            var oldBalanceText = OpeningBalanceInput.Text;
            var confirm = MessageBox.Show(
                $"Change Opening Balance for '{_editing.Name?.Trim()}' from {oldBalanceText} to {newBalance:N2}?\n\nReason: {reason}\n\nThis is permanent and will be logged.",
                "Confirm Correction", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var oldBalance = await _service.AdjustOpeningBalanceAsync(_editing.CC_ID, newBalance);
            if (oldBalance == null)
            {
                ErrorText.Text = "That customer could not be found - it may have been deleted.";
                return;
            }

            await _logService.LogAsync(CurrentSession.UserId,
                $"Adjusted Opening Balance for Credit Customer '{_editing.Name?.Trim()}' from {oldBalance.Value:N2} to {newBalance:N2} - Reason: {reason}");

            OpeningBalanceInput.Text = newBalance.ToString("N2");
            _editing.OpeningBalance = newBalance;
            AdjustOpeningBalancePanel.Visibility = Visibility.Collapsed;

            MessageBox.Show("Opening Balance corrected.", "Clidapos");
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new CreditCustomerListView(_currentUser);
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                SystemSounds.Exclamation.Play();
            }
        }
    }
}
