using System;
using System.IO;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class HotelProfilePopup : Window
    {
        private readonly HotelProfileService _hotelService = new();
        private readonly LogService _logService = new();
        private Hotel? _hotel;

        // True once a real (non-blank) profile has already been saved before -
        // GetOrCreateAsync() auto-creates a blank row on first-ever run, so a
        // blank HotelName is what distinguishes "genuinely new" from "editing
        // an existing profile" (which needs the confirm-before-overwrite prompt).
        private bool _isExistingProfile;

        public HotelProfilePopup()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadProfile();
        }

        private async System.Threading.Tasks.Task LoadProfile()
        {
            try
            {
                _hotel = await _hotelService.GetOrCreateAsync();

                NameInput.Text = _hotel.HotelName?.Trim() ?? "";
                Address1Input.Text = _hotel.AddressLine1?.Trim() ?? "";
                Address2Input.Text = _hotel.AddressLine2?.Trim() ?? "";
                Address3Input.Text = _hotel.AddressLine3?.Trim() ?? "";
                ContactInput.Text = _hotel.ContactNo?.Trim() ?? "";
                EmailInput.Text = _hotel.EmailID?.Trim() ?? "";
                ShowLogoInput.SelectedIndex = (_hotel.ShowLogo?.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) ?? false) ? 0 : 1;
                TinInput.Text = _hotel.TIN?.Trim() ?? "";
                FooterInput.Text = _hotel.TicketFooterMessage?.Trim() ?? "";

                ShowLogoPreview(_hotel.Logo);

                _isExistingProfile = !string.IsNullOrWhiteSpace(_hotel.HotelName);
                SaveButtonLabel.Text = _isExistingProfile ? "Update" : "Save";

                // Only now is it safe to save - prevents the race where a fast click
                // on Save fires before the profile has actually finished loading.
                SaveButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                ErrorText.Text = $"Could not load the business profile: {detail}";
            }
        }

        private void ShowLogoInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var isYes = ((ShowLogoInput.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "")
                .Equals("Y", StringComparison.OrdinalIgnoreCase);
            LogoPanel.Visibility = isYes ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowLogoPreview(byte[]? logoBytes)
        {
            if (logoBytes == null || logoBytes.Length == 0)
            {
                LogoPreview.Source = null;
                LogoStatusText.Text = "No logo set";
                return;
            }

            try
            {
                using var stream = new MemoryStream(logoBytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();

                LogoPreview.Source = bitmap;
                LogoStatusText.Text = $"Logo set ({logoBytes.Length / 1024} KB)";
            }
            catch
            {
                LogoPreview.Source = null;
                LogoStatusText.Text = "Logo file could not be displayed.";
            }
        }

        private void BrowseLogo_Click(object sender, RoutedEventArgs e)
        {
            if (_hotel == null) return;

            var dialog = new OpenFileDialog
            {
                Title = "Choose a Business Logo",
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var bytes = File.ReadAllBytes(dialog.FileName);
                    _hotel.Logo = bytes;
                    ShowLogoPreview(bytes);
                    ErrorText.Text = "";
                }
                catch (Exception ex)
                {
                    ErrorText.Text = $"Could not read that image file: {ex.Message}";
                }
            }
        }

        private void RemoveLogo_Click(object sender, RoutedEventArgs e)
        {
            if (_hotel == null) return;
            _hotel.Logo = null;
            ShowLogoPreview(null);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (_hotel == null)
            {
                // Profile hadn't finished loading before Save was reached (shouldn't
                // normally happen now that the button starts disabled, but this is a
                // safety net) - fetch it now without touching anything already typed.
                try
                {
                    _hotel = await _hotelService.GetOrCreateAsync();
                }
                catch (Exception ex)
                {
                    var detail = ex.InnerException?.Message ?? ex.Message;
                    ErrorText.Text = $"Could not load the business profile: {detail}";
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                ErrorText.Text = "Business Name is required.";
                return;
            }

            var contact = ContactInput.Text.Trim();
            if (!Regex.IsMatch(contact, @"^07\d{8}$"))
            {
                ErrorText.Text = "Contact No must start with 07 and be exactly 10 digits (e.g. 0712345678).";
                return;
            }

            var email = EmailInput.Text.Trim();
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorText.Text = "Enter a valid email address.";
                return;
            }

            if (_isExistingProfile)
            {
                var confirm = MessageBox.Show(
                    "This will update your saved business profile. Are you sure you want to update?",
                    "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes)
                    return;
            }

            _hotel.HotelName = NameInput.Text.Trim();
            _hotel.AddressLine1 = Address1Input.Text.Trim();
            _hotel.AddressLine2 = Address2Input.Text.Trim();
            _hotel.AddressLine3 = Address3Input.Text.Trim();
            _hotel.ContactNo = contact;
            _hotel.EmailID = email;
            _hotel.ShowLogo = (ShowLogoInput.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "N";
            _hotel.TIN = TinInput.Text.Trim();
            _hotel.TicketFooterMessage = FooterInput.Text.Trim();
            // ST No, CIN, Base Currency, Currency Code, and Capital Account aren't
            // shown on this form (unused elsewhere in the app) - left untouched here
            // rather than cleared, in case they're populated from an earlier import.
            // _hotel.Logo is already set directly by BrowseLogo_Click / RemoveLogo_Click.

            try
            {
                await _hotelService.SaveAsync(_hotel);
                await AppSettings.RefreshStoreNameAsync();
                await _logService.LogAsync(CurrentSession.UserId,
                    _isExistingProfile ? "Updated Business Profile" : "Set up Business Profile");
                MessageBox.Show(_isExistingProfile ? "Business profile updated." : "Business profile saved.", "Clidapos");

                // From here on, further clicks are updates to a real profile,
                // not the initial first-time setup.
                _isExistingProfile = true;
                SaveButtonLabel.Text = "Update";
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                ErrorText.Text = detail;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// The root Grid covers the whole dimmed screen behind the card. A click
        /// that lands directly on it (not on the card or any control inside it)
        /// means the person clicked outside the popup - rather than silently
        /// ignoring that, play a warning sound as a nudge to close the card
        /// properly (via the X or Save) instead of clicking around it.
        /// </summary>
        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                SystemSounds.Exclamation.Play();
            }
        }
    }
}