using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class BankAccountPopup : Window
    {
        private readonly BankingService _bankingService = new();
        private readonly LogService _logService = new();
        private BankAccountRegistration? _editing;

        public BankAccountPopup(BankAccountRegistration? editAccount = null)
        {
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                BankNameInput.ItemsSource = await _bankingService.GetBankNamesAsync();

                if (editAccount != null)
                {
                    LoadForEditing(editAccount);
                }
                else
                {
                    OpeningDateInput.SelectedDate = DateTime.Today;
                    ActiveInput.Text = "Y";
                }
            };
        }

        private void LoadForEditing(BankAccountRegistration account)
        {
            _editing = account;
            AccountNoInput.Text = account.AccountNo.Trim();
            AccountNoInput.IsEnabled = false; // account number is the key - not editable once created
            AccountNameInput.Text = account.AccountName?.Trim() ?? "";
            AccountTypeInput.Text = account.AccountType?.Trim() ?? "";
            BalanceInput.Text = account.BalanceAmount?.ToString("0.00") ?? "0";
            ActiveInput.Text = account.Active?.Trim() ?? "Y";
            OpeningDateInput.SelectedDate = account.OpeningDate ?? DateTime.Today;
            // Bank/Branch fields are shown blank on edit - update only touches account-level fields.
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            AccountNoInput.IsEnabled = true;
            AccountNoInput.Text = "";
            AccountNameInput.Text = "";
            AccountTypeInput.Text = "";
            BalanceInput.Text = "0";
            ActiveInput.Text = "Y";
            OpeningDateInput.SelectedDate = DateTime.Today;
            BankNameInput.Text = "";
            BranchNameInput.Text = "";
            BranchAddressInput.Text = "";
            BranchContactInput.Text = "";
            SwiftCodeInput.Text = "";
            ErrorText.Text = "";
            AccountNoInput.Focus();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(AccountNoInput.Text))
            {
                ErrorText.Text = "Account No is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(AccountNameInput.Text))
            {
                ErrorText.Text = "Account Name is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(BankNameInput.Text))
            {
                ErrorText.Text = "Bank Name is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(BranchNameInput.Text))
            {
                ErrorText.Text = "Branch Name is required.";
                return;
            }
            if (!decimal.TryParse(BalanceInput.Text, out var balance))
            {
                ErrorText.Text = "Opening Balance must be a valid number.";
                return;
            }

            try
            {
                await _bankingService.EnsureBankAsync(BankNameInput.Text.Trim());
                var branchId = await _bankingService.EnsureBranchAsync(
                    BankNameInput.Text.Trim(), BranchNameInput.Text.Trim(),
                    BranchAddressInput.Text.Trim(), BranchContactInput.Text.Trim(), SwiftCodeInput.Text.Trim(), null);

                var account = new BankAccountRegistration
                {
                    AccountNo = AccountNoInput.Text.Trim(),
                    AccountName = AccountNameInput.Text.Trim(),
                    AccountType = AccountTypeInput.Text.Trim(),
                    OpeningDate = OpeningDateInput.SelectedDate ?? DateTime.Today,
                    BalanceAmount = balance,
                    Active = ActiveInput.Text.Trim(),
                    BranchID = branchId
                };

                await _bankingService.AddAccountAsync(account);
                await _logService.LogAsync(CurrentSession.UserId,
                    $"Registered Bank Account '{account.AccountName}' ({account.AccountNo}) at {BankNameInput.Text.Trim()} - {BranchNameInput.Text.Trim()}");

                New_Click(sender, e);
                ErrorText.Text = "Saved.";
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                ErrorText.Text = detail;
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick an account, then edit and Update.";
                return;
            }
            if (string.IsNullOrWhiteSpace(AccountNameInput.Text))
            {
                ErrorText.Text = "Account Name is required.";
                return;
            }
            if (!decimal.TryParse(BalanceInput.Text, out var balance))
            {
                ErrorText.Text = "Opening Balance must be a valid number.";
                return;
            }

            try
            {
                _editing.AccountName = AccountNameInput.Text.Trim();
                _editing.AccountType = AccountTypeInput.Text.Trim();
                _editing.OpeningDate = OpeningDateInput.SelectedDate ?? DateTime.Today;
                _editing.BalanceAmount = balance;
                _editing.Active = ActiveInput.Text.Trim();

                await _bankingService.UpdateAccountAsync(_editing);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Bank Account '{_editing.AccountName}'");
                ErrorText.Text = "Updated.";
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                ErrorText.Text = detail;
            }
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new BankAccountListView();
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}