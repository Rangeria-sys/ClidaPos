using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
                ShowLogoInput.Text = _hotel.ShowLogo?.Trim() ?? "N";
                TinInput.Text = _hotel.TIN?.Trim() ?? "";
                StNoInput.Text = _hotel.STNo?.Trim() ?? "";
                CinInput.Text = _hotel.CIN?.Trim() ?? "";
                BaseCurrencyInput.Text = _hotel.BaseCurrency?.Trim() ?? "";
                CurrencyCodeInput.Text = _hotel.CurrencyCode?.Trim() ?? "";
                CapitalAccountInput.Text = _hotel.CapitalAccount?.ToString("0.00") ?? "";
                FooterInput.Text = _hotel.TicketFooterMessage?.Trim() ?? "";

                ShowLogoPreview(_hotel.Logo);

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

            decimal? capitalAccount = null;
            if (!string.IsNullOrWhiteSpace(CapitalAccountInput.Text))
            {
                if (!decimal.TryParse(CapitalAccountInput.Text, out var parsed))
                {
                    ErrorText.Text = "Capital Account must be a valid number.";
                    return;
                }
                capitalAccount = parsed;
            }

            _hotel.HotelName = NameInput.Text.Trim();
            _hotel.AddressLine1 = Address1Input.Text.Trim();
            _hotel.AddressLine2 = Address2Input.Text.Trim();
            _hotel.AddressLine3 = Address3Input.Text.Trim();
            _hotel.ContactNo = ContactInput.Text.Trim();
            _hotel.EmailID = EmailInput.Text.Trim();
            _hotel.ShowLogo = ShowLogoInput.Text.Trim();
            _hotel.TIN = TinInput.Text.Trim();
            _hotel.STNo = StNoInput.Text.Trim();
            _hotel.CIN = CinInput.Text.Trim();
            _hotel.BaseCurrency = BaseCurrencyInput.Text.Trim();
            _hotel.CurrencyCode = CurrencyCodeInput.Text.Trim();
            _hotel.CapitalAccount = capitalAccount;
            _hotel.TicketFooterMessage = FooterInput.Text.Trim();
            // _hotel.Logo is already set directly by BrowseLogo_Click / RemoveLogo_Click.

            try
            {
                await _hotelService.SaveAsync(_hotel);
                await _logService.LogAsync(CurrentSession.UserId, "Updated Business Profile");
                MessageBox.Show("Business profile saved.", "Clidapos");
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
    }
}