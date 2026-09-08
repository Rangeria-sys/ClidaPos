using System;
using System.Media;
using System.Windows;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class BankBranchPopup : Window
    {
        private readonly Registration _currentUser;
        private readonly BankingService _bankingService = new();
        private readonly LogService _logService = new();
        private BankBranch? _editing;

        public BankBranchPopup(Registration currentUser, BankBranch? editBranch = null)
        {
            InitializeComponent();
            _currentUser = currentUser;

            Loaded += async (s, e) =>
            {
                BankNameInput.ItemsSource = await _bankingService.GetBankNamesAsync();

                if (editBranch != null)
                {
                    _editing = editBranch;
                    BankNameInput.Text = editBranch.BankName?.Trim() ?? "";
                    BranchNameInput.Text = editBranch.BranchName?.Trim() ?? "";
                    AddressInput.Text = editBranch.Address?.Trim() ?? "";
                    ContactNoInput.Text = editBranch.ContactNo?.Trim() ?? "";
                    SwiftCodeInput.Text = editBranch.SwiftCode?.Trim() ?? "";
                }
            };
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            BankNameInput.Text = "";
            BranchNameInput.Text = "";
            AddressInput.Text = "";
            ContactNoInput.Text = "";
            SwiftCodeInput.Text = "";
            ErrorText.Text = "";
            BankNameInput.Focus();
        }

        private bool Validate()
        {
            ErrorText.Text = "";
            if (string.IsNullOrWhiteSpace(BankNameInput.Text))
            {
                ErrorText.Text = "Bank Name is required.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(BranchNameInput.Text))
            {
                ErrorText.Text = "Branch Name is required.";
                return false;
            }
            return true;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;

            try
            {
                await _bankingService.EnsureBankAsync(BankNameInput.Text.Trim());
                await _bankingService.EnsureBranchAsync(
                    BankNameInput.Text.Trim(), BranchNameInput.Text.Trim(),
                    AddressInput.Text.Trim(), ContactNoInput.Text.Trim(), SwiftCodeInput.Text.Trim(), null);

                await _logService.LogAsync(CurrentSession.UserId,
                    $"Registered Bank Branch '{BranchNameInput.Text.Trim()}' at {BankNameInput.Text.Trim()}");

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
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a bank, then edit and Update.";
                return;
            }
            if (!Validate()) return;

            var confirm = MessageBox.Show($"Update branch '{_editing.BranchName?.Trim()}'?",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _bankingService.EnsureBankAsync(BankNameInput.Text.Trim());

                _editing.BankName = BankNameInput.Text.Trim();
                _editing.BranchName = BranchNameInput.Text.Trim();
                _editing.Address = AddressInput.Text.Trim();
                _editing.ContactNo = ContactNoInput.Text.Trim();
                _editing.SwiftCode = SwiftCodeInput.Text.Trim();

                await _bankingService.UpdateBranchAsync(_editing);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Bank Branch '{_editing.BranchName}'");
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
                ErrorText.Text = "Use Get Data, pick a bank, then Delete.";
                return;
            }

            var confirm = MessageBox.Show(
                $"Remove branch '{_editing.BranchName?.Trim()}'? Any bank accounts already registered at this branch will be unaffected.",
                "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var deletedName = _editing.BranchName?.Trim() ?? "";
            await _bankingService.DeleteBranchAsync(_editing.Id);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Bank Branch '{deletedName}'");

            MessageBox.Show("Removed.", "Clidapos");
            New_Click(sender, e);
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new BankBranchListView(_currentUser);
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var listView = new BankBranchListView(_currentUser);
            listView.Show();
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
