using System;
using System.Collections.Generic;
using System.Media;
using System.Windows;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class MpesaSettingPopup : Window
    {
        private readonly IntegrationSettingsService _settingsService = new();
        private readonly LogService _logService = new();
        private MpesaSetting? _setting;

        // Tracks which credential fields the user has actually typed into -
        // fields NOT in this set still show the permanent mask, and Save must
        // reuse the original stored value for them rather than reading the
        // mask text off the TextBox, which would corrupt the real credential.
        private readonly HashSet<string> _touchedFields = new();

        public MpesaSettingPopup()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                _setting = await _settingsService.GetOrCreateMpesaAsync();
                ConsumerKeyInput.Text = SecretEncryptionService.Mask(_setting.ConsumerKey);
                ConsumerSecretInput.Text = SecretEncryptionService.Mask(_setting.ConsumerSecret);
                ShortcodeInput.Text = _setting.Shortcode?.Trim() ?? "";
                PassKeyInput.Text = SecretEncryptionService.Mask(_setting.PassKey);
                AccountNumberInput.Text = _setting.AccountNumber?.Trim() ?? "";
                EnvironmentInput.Text = _setting.Environment?.Trim() ?? "Sandbox - Paybill";

                // A field that was empty to begin with has nothing to mask -
                // it's already safe to type into and save directly.
                if (string.IsNullOrEmpty(_setting.ConsumerKey)) _touchedFields.Add("ConsumerKey");
                if (string.IsNullOrEmpty(_setting.ConsumerSecret)) _touchedFields.Add("ConsumerSecret");
                if (string.IsNullOrEmpty(_setting.PassKey)) _touchedFields.Add("PassKey");

                // Attached only now, after the masked text above is already set -
                // attaching earlier would fire on that initial assignment itself
                // and incorrectly mark every field as user-edited from the start.
                ConsumerKeyInput.TextChanged += (s2, e2) => _touchedFields.Add("ConsumerKey");
                ConsumerSecretInput.TextChanged += (s2, e2) => _touchedFields.Add("ConsumerSecret");
                PassKeyInput.TextChanged += (s2, e2) => _touchedFields.Add("PassKey");
            };
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            if (_setting == null) { ErrorText.Text = "Settings haven't loaded yet."; return; }

            if (_touchedFields.Contains("ConsumerKey") && string.IsNullOrWhiteSpace(ConsumerKeyInput.Text))
            {
                ErrorText.Text = "Consumer Key is required.";
                return;
            }
            if (_touchedFields.Contains("ConsumerSecret") && string.IsNullOrWhiteSpace(ConsumerSecretInput.Text))
            {
                ErrorText.Text = "Consumer Secret is required.";
                return;
            }
            if (_touchedFields.Contains("PassKey") && string.IsNullOrWhiteSpace(PassKeyInput.Text))
            {
                ErrorText.Text = "Pass Key is required.";
                return;
            }

            // Only take the TextBox's text for fields the user actually typed
            // into - anything still showing the mask keeps its original value.
            _setting.ConsumerKey = _touchedFields.Contains("ConsumerKey") ? ConsumerKeyInput.Text.Trim() : _setting.ConsumerKey;
            _setting.ConsumerSecret = _touchedFields.Contains("ConsumerSecret") ? ConsumerSecretInput.Text.Trim() : _setting.ConsumerSecret;
            _setting.Shortcode = ShortcodeInput.Text.Trim();
            _setting.PassKey = _touchedFields.Contains("PassKey") ? PassKeyInput.Text.Trim() : _setting.PassKey;
            _setting.AccountNumber = AccountNumberInput.Text.Trim();
            _setting.Environment = EnvironmentInput.Text.Trim();

            var changedCredentials = new List<string>();
            if (_touchedFields.Contains("ConsumerKey")) changedCredentials.Add("Consumer Key");
            if (_touchedFields.Contains("ConsumerSecret")) changedCredentials.Add("Consumer Secret");
            if (_touchedFields.Contains("PassKey")) changedCredentials.Add("Pass Key");

            var changeNote = changedCredentials.Count > 0
                ? $"\n\nChanging: {string.Join(", ", changedCredentials)}"
                : "";

            var confirm = MessageBox.Show(
                $"Update the live M-Pesa API settings?{changeNote}\n\nIncorrect values will cause customer payments to fail. This overwrites the settings currently in use.",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _settingsService.SaveMpesaAsync(_setting);
                await _logService.LogAsync(CurrentSession.UserId, "Updated M-Pesa API Settings");
                MessageBox.Show("M-Pesa settings saved.", "Clidapos");
                Close();
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.InnerException?.Message ?? ex.Message;
            }
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
