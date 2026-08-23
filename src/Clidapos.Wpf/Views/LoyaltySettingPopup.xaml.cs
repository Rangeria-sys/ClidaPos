using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class LoyaltySettingPopup : Window
    {
        private readonly LoyaltyService _loyaltyService = new();
        private readonly LogService _logService = new();
        private LoyaltySetting? _editing;

        public LoyaltySettingPopup(LoyaltySetting? editSetting = null)
        {
            InitializeComponent();

            if (editSetting != null)
            {
                _editing = editSetting;
                NameInput.Text = editSetting.LoyaltyName.Trim();
                NameInput.IsEnabled = false;
                AmountInput.Text = editSetting.Amount?.ToString("0.##") ?? "";
                PointsInput.Text = editSetting.Points?.ToString("0.##") ?? "";
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            NameInput.IsEnabled = true;
            NameInput.Text = "";
            AmountInput.Text = "";
            PointsInput.Text = "";
            ErrorText.Text = "";
            NameInput.Focus();
        }

        private bool TryParseFields(out decimal amount, out decimal points)
        {
            amount = 0;
            points = 0;
            return decimal.TryParse(AmountInput.Text, out amount) && decimal.TryParse(PointsInput.Text, out points);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Rule Name is required.";
                return;
            }
            if (!TryParseFields(out var amount, out var points))
            {
                ErrorText.Text = "Amount and Points must both be valid numbers.";
                return;
            }

            var setting = new LoyaltySetting { LoyaltyName = name, Amount = amount, Points = points };

            try
            {
                await _loyaltyService.AddSettingAsync(setting);
                await _logService.LogAsync(CurrentSession.UserId, $"Added Loyalty Rule '{name}'");
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
                ErrorText.Text = "Use Get Data, pick a rule, then edit and Update.";
                return;
            }
            if (!TryParseFields(out var amount, out var points))
            {
                ErrorText.Text = "Amount and Points must both be valid numbers.";
                return;
            }

            try
            {
                _editing.Amount = amount;
                _editing.Points = points;

                await _loyaltyService.UpdateSettingAsync(_editing);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Loyalty Rule '{_editing.LoyaltyName.Trim()}'");
                ErrorText.Text = "Updated.";
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
                ErrorText.Text = "Use Get Data, pick a rule, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove loyalty rule '{_editing.LoyaltyName.Trim()}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var deletedName = _editing.LoyaltyName.Trim();
            await _loyaltyService.DeleteSettingAsync(deletedName);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Loyalty Rule '{deletedName}'");
            New_Click(sender, e);
            ErrorText.Text = "Removed.";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}