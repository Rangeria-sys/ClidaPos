using System;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class LoyaltySettingPopup : Window
    {
        private readonly Registration _currentUser;
        private readonly LoyaltyService _loyaltyService = new();
        private readonly LogService _logService = new();
        private LoyaltySetting? _editing;

        public LoyaltySettingPopup(Registration currentUser, LoyaltySetting? editSetting = null)
        {
            InitializeComponent();
            _currentUser = currentUser;

            if (editSetting != null)
            {
                _editing = editSetting;
                NameInput.Text = editSetting.LoyaltyName.Trim();
                NameInput.IsEnabled = false; // rule name is the key - not editable once created
                AmountInput.Text = editSetting.Amount?.ToString("0.##") ?? "";
                PointsInput.Text = editSetting.Points?.ToString("0.##") ?? "";
                SetMode(isExisting: true);
            }
            else
            {
                SetMode(isExisting: false);
            }
        }

        /// <summary>
        /// Save only ever creates a brand-new rule. Once something has been
        /// loaded via Get Data (double-click), only Update can change it.
        /// </summary>
        private void SetMode(bool isExisting)
        {
            SaveBtn.IsEnabled = !isExisting;
            UpdateBtn.IsEnabled = isExisting;
            DeleteBtn.IsEnabled = isExisting;
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            NameInput.IsEnabled = true;
            NameInput.Text = "";
            AmountInput.Text = "";
            PointsInput.Text = "";
            ErrorText.Text = "";
            SetMode(isExisting: false);
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
            ErrorText.Text = "";
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
                ErrorText.Text = "Use Get Data, pick a rule, then edit and Update.";
                return;
            }
            if (!TryParseFields(out var amount, out var points))
            {
                ErrorText.Text = "Amount and Points must both be valid numbers.";
                return;
            }

            var confirm = MessageBox.Show($"Update loyalty rule '{_editing.LoyaltyName.Trim()}'?",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                _editing.Amount = amount;
                _editing.Points = points;

                await _loyaltyService.UpdateSettingAsync(_editing);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Loyalty Rule '{_editing.LoyaltyName.Trim()}'");
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
                ErrorText.Text = "Use Get Data, pick a rule, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove loyalty rule '{_editing.LoyaltyName.Trim()}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var deletedName = _editing.LoyaltyName.Trim();
            await _loyaltyService.DeleteSettingAsync(deletedName);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Loyalty Rule '{deletedName}'");

            MessageBox.Show("Removed.", "Clidapos");
            New_Click(sender, e);
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new LoyaltySettingListView(_currentUser);
            listView.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var listView = new LoyaltySettingListView(_currentUser);
            listView.Show();
            Close();
        }

        private static readonly Regex NumericPattern = new(@"^[0-9]*\.?[0-9]*$");

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                e.Handled = true;
                return;
            }

            var proposedText = textBox.Text
                .Remove(textBox.SelectionStart, textBox.SelectionLength)
                .Insert(textBox.SelectionStart, e.Text);

            e.Handled = !NumericPattern.IsMatch(proposedText);
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
