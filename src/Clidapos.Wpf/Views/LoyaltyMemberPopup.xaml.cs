using System;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class LoyaltyMemberPopup : Window
    {
        private readonly Registration _currentUser;
        private readonly LoyaltyService _loyaltyService = new();
        private readonly LogService _logService = new();
        private LoyaltyMember? _editing;

        public LoyaltyMemberPopup(Registration currentUser, LoyaltyMember? editMember = null)
        {
            InitializeComponent();
            _currentUser = currentUser;

            if (editMember != null)
            {
                LoadForEditing(editMember);
                SetMode(isExisting: true);
            }
            else
            {
                MemberIdText.Text = "Member ID will be assigned automatically on Save.";
                ActiveInput.SelectedIndex = 0;
                SetMode(isExisting: false);
            }
        }

        /// <summary>
        /// Save only ever creates a brand-new member. Once something has been
        /// loaded via Get Data (double-click), only Update can change it.
        /// </summary>
        private void SetMode(bool isExisting)
        {
            SaveBtn.IsEnabled = !isExisting;
            UpdateBtn.IsEnabled = isExisting;
            DeleteBtn.IsEnabled = isExisting;
        }

        private void LoadForEditing(LoyaltyMember member)
        {
            _editing = member;
            MemberIdText.Text = $"Member ID: {member.MemberID}";
            NameInput.Text = member.Name?.Trim() ?? "";
            CardNoInput.Text = member.CardNo?.Trim() ?? "";
            ContactInput.Text = member.ContactNo?.Trim() ?? "";
            AddressInput.Text = member.Address?.Trim() ?? "";
            ActiveInput.Text = member.Active?.Trim() ?? "Y";
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            MemberIdText.Text = "Member ID will be assigned automatically on Save.";
            NameInput.Text = "";
            CardNoInput.Text = "";
            ContactInput.Text = "";
            AddressInput.Text = "";
            ActiveInput.SelectedIndex = 0;
            ErrorText.Text = "";
            SetMode(isExisting: false);
            NameInput.Focus();
        }

        /// <summary>Validates Name/Contact. Returns (name, contact) on success, or null
        /// (with ErrorText already set) if something's invalid.</summary>
        private (string name, string contact)? ValidateRequired()
        {
            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Name is required.";
                return null;
            }

            var contact = ContactInput.Text.Trim();
            if (!Regex.IsMatch(contact, @"^07\d{8}$"))
            {
                ErrorText.Text = "Contact No must start with 07 and be exactly 10 digits (e.g. 0712345678).";
                return null;
            }

            return (name, contact);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            var validated = ValidateRequired();
            if (validated == null) return;
            var (name, contact) = validated.Value;

            var existing = await _loyaltyService.GetAllMembersAsync();
            if (existing.Any(m => (m.Name ?? "").Trim().Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorText.Text = $"A member named '{name}' already exists.";
                return;
            }

            try
            {
                var newId = await _loyaltyService.GetNextMemberIdAsync();

                var member = new LoyaltyMember
                {
                    MemberID = newId,
                    Name = name,
                    CardNo = CardNoInput.Text.Trim(),
                    ContactNo = contact,
                    Address = AddressInput.Text.Trim(),
                    RegistrationDate = DateTime.Today,
                    Active = ActiveInput.Text.Trim()
                };

                await _loyaltyService.AddMemberAsync(member);
                await _logService.LogAsync(CurrentSession.UserId, $"Registered Loyalty Member '{name}' (ID {newId})");

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

            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a member, then edit and Update.";
                return;
            }

            var validated = ValidateRequired();
            if (validated == null) return;
            var (name, contact) = validated.Value;

            var existing = await _loyaltyService.GetAllMembersAsync();
            var nameTaken = existing.Any(m =>
                m.MemberID != _editing.MemberID &&
                (m.Name ?? "").Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
            if (nameTaken)
            {
                ErrorText.Text = $"Another member is already named '{name}'.";
                return;
            }

            var confirm = MessageBox.Show($"Update member '{_editing.Name?.Trim()}'?",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                _editing.Name = name;
                _editing.CardNo = CardNoInput.Text.Trim();
                _editing.ContactNo = contact;
                _editing.Address = AddressInput.Text.Trim();
                _editing.Active = ActiveInput.Text.Trim();

                await _loyaltyService.UpdateMemberAsync(_editing);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Loyalty Member '{name}'");

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
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a member, then Delete.";
                return;
            }

            var confirm = MessageBox.Show(
                $"Remove loyalty member '{_editing.Name?.Trim()}'? Their points history will be kept as a permanent record.",
                "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var deletedName = _editing.Name?.Trim() ?? "";
            await _loyaltyService.DeleteMemberAsync(_editing.MemberID);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Loyalty Member '{deletedName}'");

            MessageBox.Show("Removed.", "Clidapos");
            New_Click(sender, e);
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new LoyaltyMemberListView(_currentUser);
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
                SystemSounds.Exclamation.Play();
            }
        }
    }
}
