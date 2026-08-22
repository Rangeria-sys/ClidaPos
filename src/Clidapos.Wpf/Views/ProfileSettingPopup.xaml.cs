using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class ProfileSettingPopup : Window
    {
        private readonly RegistrationService _registrationService = new();
        private readonly LogService _logService = new();
        private Registration? _me;

        public ProfileSettingPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadMyProfile();
        }

        private async System.Threading.Tasks.Task LoadMyProfile()
        {
            _me = await _registrationService.GetByUserIdAsync(CurrentSession.UserId);

            if (_me == null)
            {
                ErrorText.Text = "Could not load your profile.";
                return;
            }

            UserIdText.Text = _me.UserID.Trim();
            UserTypeText.Text = _me.UserType.Trim();
            NameInput.Text = _me.Name.Trim();
            PasswordInput.Text = _me.Password.Trim();
            ContactInput.Text = _me.ContactNo?.Trim() ?? "";
            EmailInput.Text = _me.EmailID?.Trim() ?? "";
            SsnInput.Text = _me.SSN?.Trim() ?? "";
            PayrollTypeInput.Text = _me.PayrollType?.Trim() ?? "";
            CardNoInput.Text = _me.CardNo?.Trim() ?? "";
            AutoLogoutInput.Text = _me.AutoLogout?.Trim() ?? "";
            JoiningDateText.Text = _me.JoiningDate.ToString("dd MMM yyyy");
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (_me == null)
            {
                ErrorText.Text = "Your profile hasn't loaded yet.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                ErrorText.Text = "Name is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(PasswordInput.Text))
            {
                ErrorText.Text = "PIN / Password is required.";
                return;
            }

            try
            {
                // UserType and Active are deliberately never touched here - those
                // stay admin-only, managed through User Security Roles.
                _me.Name = NameInput.Text.Trim();
                _me.Password = PasswordInput.Text.Trim();
                _me.ContactNo = ContactInput.Text.Trim();
                _me.EmailID = EmailInput.Text.Trim();
                _me.SSN = SsnInput.Text.Trim();
                _me.PayrollType = PayrollTypeInput.Text.Trim();
                _me.CardNo = CardNoInput.Text.Trim();
                _me.AutoLogout = AutoLogoutInput.Text.Trim();

                await _registrationService.UpdateAsync(_me.UserID, _me);
                await _logService.LogAsync(CurrentSession.UserId, "Updated own Profile");

                MessageBox.Show("Profile updated.", "Clidapos");
                ErrorText.Text = "";
            }
            catch (System.Exception ex)
            {
                ErrorText.Text = ex.Message;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}