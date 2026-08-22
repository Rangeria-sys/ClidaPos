using System;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class UserSecurityRolesPopup : Window
    {
        private readonly RegistrationService _registrationService = new();
        private readonly LogService _logService = new();
        private string? _editingOriginalUserId;

        public UserSecurityRolesPopup(Registration? editUser = null)
        {
            InitializeComponent();

            if (editUser != null)
            {
                LoadForEditing(editUser);
            }
            else
            {
                JoiningDateInput.SelectedDate = DateTime.Today;
                ActiveInput.Text = "Y";
            }
        }

        private void LoadForEditing(Registration user)
        {
            _editingOriginalUserId = user.UserID.Trim();
            UserIdInput.Text = user.UserID.Trim();
            NameInput.Text = user.Name.Trim();
            PasswordInput.Text = user.Password.Trim();
            UserTypeInput.Text = user.UserType.Trim();
            ActiveInput.Text = user.Active?.Trim() ?? "Y";
            JoiningDateInput.SelectedDate = user.JoiningDate;
            ContactInput.Text = user.ContactNo?.Trim() ?? "";
            EmailInput.Text = user.EmailID?.Trim() ?? "";
            SsnInput.Text = user.SSN?.Trim() ?? "";
            PayrollTypeInput.Text = user.PayrollType?.Trim() ?? "";
            CardNoInput.Text = user.CardNo?.Trim() ?? "";
            AutoLogoutInput.Text = user.AutoLogout?.Trim() ?? "";
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editingOriginalUserId = null;
            UserIdInput.Text = "";
            NameInput.Text = "";
            PasswordInput.Text = "";
            UserTypeInput.Text = "";
            ActiveInput.Text = "Y";
            JoiningDateInput.SelectedDate = DateTime.Today;
            ContactInput.Text = "";
            EmailInput.Text = "";
            SsnInput.Text = "";
            PayrollTypeInput.Text = "";
            CardNoInput.Text = "";
            AutoLogoutInput.Text = "";
            ErrorText.Text = "";
            UserIdInput.Focus();
        }

        private Registration BuildFromInputs()
        {
            return new Registration
            {
                UserID = UserIdInput.Text.Trim(),
                Name = NameInput.Text.Trim(),
                Password = PasswordInput.Text.Trim(),
                UserType = UserTypeInput.Text.Trim(),
                Active = ActiveInput.Text.Trim(),
                JoiningDate = JoiningDateInput.SelectedDate ?? DateTime.Today,
                ContactNo = ContactInput.Text.Trim(),
                EmailID = EmailInput.Text.Trim(),
                SSN = SsnInput.Text.Trim(),
                PayrollType = PayrollTypeInput.Text.Trim(),
                CardNo = CardNoInput.Text.Trim(),
                AutoLogout = AutoLogoutInput.Text.Trim()
            };
        }

        private bool ValidateRequired()
        {
            if (string.IsNullOrWhiteSpace(UserIdInput.Text))
            {
                ErrorText.Text = "User ID is required.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                ErrorText.Text = "Name is required.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(PasswordInput.Text))
            {
                ErrorText.Text = "PIN / Password is required.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(UserTypeInput.Text))
            {
                ErrorText.Text = "User Type is required.";
                return false;
            }
            return true;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            if (!ValidateRequired()) return;

            try
            {
                await _registrationService.AddAsync(BuildFromInputs());
                await _logService.LogAsync(CurrentSession.UserId,
                    $"Added User '{NameInput.Text.Trim()}' ({UserIdInput.Text.Trim()})");
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
            ErrorText.Text = "";

            if (_editingOriginalUserId == null)
            {
                ErrorText.Text = "Use Get Data, pick a user, then edit and Update.";
                return;
            }
            if (!ValidateRequired()) return;

            try
            {
                var name = NameInput.Text.Trim();
                await _registrationService.UpdateAsync(_editingOriginalUserId, BuildFromInputs());
                await _logService.LogAsync(CurrentSession.UserId, $"Updated User '{name}'");
                _editingOriginalUserId = UserIdInput.Text.Trim();
                ErrorText.Text = "Updated.";
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_editingOriginalUserId == null)
            {
                ErrorText.Text = "Use Get Data, pick a user, then Delete.";
                return;
            }

            if (string.Equals(_editingOriginalUserId, CurrentSession.UserId, StringComparison.OrdinalIgnoreCase))
            {
                ErrorText.Text = "You can't delete the account you're currently logged in as.";
                return;
            }

            var confirm = MessageBox.Show($"Delete user '{NameInput.Text.Trim()}'?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var deletedName = NameInput.Text.Trim();
            await _registrationService.RemoveAsync(_editingOriginalUserId);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted User '{deletedName}'");
            New_Click(sender, e);
            ErrorText.Text = "Removed.";
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new UserSecurityRolesListView();
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}