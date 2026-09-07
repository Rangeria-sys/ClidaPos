using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class UserSecurityRolesPopup : Window
    {
        private readonly RegistrationService _registrationService = new();
        private readonly UserRightsService _userRightsService = new();
        private readonly LogService _logService = new();
        private string? _editingOriginalUserId;

        public UserSecurityRolesPopup(Registration? editUser = null)
        {
            InitializeComponent();

            if (editUser != null)
            {
                LoadForEditing(editUser);
                SetMode(isExisting: true);
                Loaded += async (s, e) => await LoadRightsGrid(editUser.UserID.Trim());
            }
            else
            {
                JoiningDateInput.SelectedDate = DateTime.Today;
                ActiveInput.SelectedIndex = 0;
                SetMode(isExisting: false);
                LoadBlankRightsGrid();
            }
        }

        /// <summary>
        /// Save only ever creates a brand-new user. Once something has been
        /// loaded via Get Data (double-click), only Update can change it - this
        /// keeps "adding a new one" and "editing an existing one" from ever being
        /// confused with each other.
        /// </summary>
        private void SetMode(bool isExisting)
        {
            SaveBtn.IsEnabled = !isExisting;
            UpdateBtn.IsEnabled = isExisting;
            DeleteBtn.IsEnabled = isExisting;
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
        }

        private async System.Threading.Tasks.Task LoadRightsGrid(string userId)
        {
            RightsGrid.ItemsSource = await _userRightsService.GetForUserAsync(userId);
        }

        private void LoadBlankRightsGrid()
        {
            RightsGrid.ItemsSource = UserRightsService.Modules
                .Select(m => new UserRight { ModuleName = m, UR_Save = false, UR_Update = false, UR_Delete = false, UR_View = false })
                .ToList();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editingOriginalUserId = null;
            UserIdInput.Text = "";
            NameInput.Text = "";
            PasswordInput.Text = "";
            UserTypeInput.Text = "";
            ActiveInput.SelectedIndex = 0;
            JoiningDateInput.SelectedDate = DateTime.Today;
            ContactInput.Text = "";
            EmailInput.Text = "";
            ErrorText.Text = "";
            LoadBlankRightsGrid();
            SetMode(isExisting: false);
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
                CardNo = "",
                AutoLogout = ""
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

        private List<UserRight> CurrentRightsRows() => (RightsGrid.ItemsSource as List<UserRight>) ?? new List<UserRight>();

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            if (!ValidateRequired()) return;

            var userId = UserIdInput.Text.Trim();

            try
            {
                await _registrationService.AddAsync(BuildFromInputs());
                await _userRightsService.SaveForUserAsync(userId, CurrentRightsRows());
                await _logService.LogAsync(CurrentSession.UserId,
                    $"Added User '{NameInput.Text.Trim()}' ({userId})");

                MessageBox.Show("Saved.", "Clidapos");
                New_Click(sender, e);
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

            if (_editingOriginalUserId == null)
            {
                ErrorText.Text = "Use Get Data, pick a user, then edit and Update.";
                return;
            }
            if (!ValidateRequired()) return;

            var confirm = MessageBox.Show($"Update user '{NameInput.Text.Trim()}'?",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var name = NameInput.Text.Trim();
                var newUserId = UserIdInput.Text.Trim();

                // This screen doesn't show SSN/Payroll Type/Card No/Auto Logout
                // (those are set by the user themselves via Profile Settings) -
                // carry the existing values forward so Update doesn't blank them.
                var current = await _registrationService.GetByUserIdAsync(_editingOriginalUserId);
                var updated = BuildFromInputs();
                updated.SSN = current?.SSN;
                updated.PayrollType = current?.PayrollType;
                updated.CardNo = current?.CardNo;
                updated.AutoLogout = current?.AutoLogout;

                await _registrationService.UpdateAsync(_editingOriginalUserId, updated);
                await _userRightsService.SaveForUserAsync(newUserId, CurrentRightsRows());
                await _logService.LogAsync(CurrentSession.UserId, $"Updated User '{name}'");

                _editingOriginalUserId = newUserId;
                MessageBox.Show("Updated.", "Clidapos");
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                ErrorText.Text = detail;
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
            await _userRightsService.DeleteForUserAsync(_editingOriginalUserId);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted User '{deletedName}'");

            MessageBox.Show("Removed.", "Clidapos");
            New_Click(sender, e);
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

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
        }
    }
}
