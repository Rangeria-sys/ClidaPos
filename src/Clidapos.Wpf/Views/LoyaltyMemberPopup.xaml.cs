using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class LoyaltyMemberPopup : Window
    {
        private readonly LoyaltyService _loyaltyService = new();
        private readonly LogService _logService = new();
        private LoyaltyMember? _editing;

        public LoyaltyMemberPopup(LoyaltyMember? editMember = null)
        {
            InitializeComponent();

            if (editMember != null)
            {
                _editing = editMember;
                MemberIdText.Text = $"Member ID: {editMember.MemberID}";
                NameInput.Text = editMember.Name?.Trim() ?? "";
                CardNoInput.Text = editMember.CardNo?.Trim() ?? "";
                ContactInput.Text = editMember.ContactNo?.Trim() ?? "";
                AddressInput.Text = editMember.Address?.Trim() ?? "";
                ActiveInput.Text = editMember.Active?.Trim() ?? "Y";
            }
            else
            {
                MemberIdText.Text = "Member ID will be assigned automatically on Save.";
                ActiveInput.Text = "Y";
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            MemberIdText.Text = "Member ID will be assigned automatically on Save.";
            NameInput.Text = "";
            CardNoInput.Text = "";
            ContactInput.Text = "";
            AddressInput.Text = "";
            ActiveInput.Text = "Y";
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

            var newId = await _loyaltyService.GetNextMemberIdAsync();

            var member = new LoyaltyMember
            {
                MemberID = newId,
                Name = name,
                CardNo = CardNoInput.Text.Trim(),
                ContactNo = ContactInput.Text.Trim(),
                Address = AddressInput.Text.Trim(),
                RegistrationDate = DateTime.Today,
                Active = ActiveInput.Text.Trim()
            };

            try
            {
                await _loyaltyService.AddMemberAsync(member);
                await _logService.LogAsync(CurrentSession.UserId, $"Registered Loyalty Member '{name}' (ID {newId})");
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
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a member, then edit and Update.";
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
                _editing.Name = name;
                _editing.CardNo = CardNoInput.Text.Trim();
                _editing.ContactNo = ContactInput.Text.Trim();
                _editing.Address = AddressInput.Text.Trim();
                _editing.Active = ActiveInput.Text.Trim();

                await _loyaltyService.UpdateMemberAsync(_editing);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Loyalty Member '{name}'");
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
            var listView = new LoyaltyMemberListView();
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}