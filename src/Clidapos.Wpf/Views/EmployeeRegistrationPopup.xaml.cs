using System;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class EmployeeRegistrationPopup : Window
    {
        private readonly EmployeeRegistrationService _employeeService = new();
        private readonly LogService _logService = new();
        private EmployeeRegistration? _editing;
        private byte[]? _photoBytes;

        public EmployeeRegistrationPopup(EmployeeRegistration? editEmployee = null)
        {
            InitializeComponent();

            if (editEmployee != null)
            {
                LoadForEditing(editEmployee);
                SetMode(isExisting: true);
            }
            else
            {
                JoiningDateInput.SelectedDate = DateTime.Today;
                ActiveInput.SelectedIndex = 0;
                SetMode(isExisting: false);
                ShowPhotoPreview(null);
            }
        }

        /// <summary>
        /// Save only ever creates a brand-new employee. Once something has been
        /// loaded via Get Data (double-click), only Update can change it.
        /// </summary>
        private void SetMode(bool isExisting)
        {
            SaveBtn.IsEnabled = !isExisting;
            UpdateBtn.IsEnabled = isExisting;
            DeleteBtn.IsEnabled = isExisting;
        }

        private void LoadForEditing(EmployeeRegistration employee)
        {
            _editing = employee;
            EmployeeIdInput.Text = employee.EmployeeID.Trim();
            NationalIdInput.Text = employee.NationalID?.Trim() ?? "";
            NameInput.Text = employee.EmployeeName.Trim();
            AddressInput.Text = employee.Address.Trim();
            CityInput.Text = employee.City.Trim();
            ContactInput.Text = employee.ContactNo.Trim();
            EmailInput.Text = employee.Email.Trim();
            JoiningDateInput.SelectedDate = employee.DateOfJoining;
            ActiveInput.Text = employee.Active?.Trim() ?? "Y";
            _photoBytes = employee.Photo;
            ShowPhotoPreview(_photoBytes);
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            EmployeeIdInput.Text = "";
            NationalIdInput.Text = "";
            NameInput.Text = "";
            AddressInput.Text = "";
            CityInput.Text = "";
            ContactInput.Text = "";
            EmailInput.Text = "";
            JoiningDateInput.SelectedDate = DateTime.Today;
            ActiveInput.SelectedIndex = 0;
            ErrorText.Text = "";
            _photoBytes = null;
            ShowPhotoPreview(null);
            SetMode(isExisting: false);
            NameInput.Focus();
        }

        /// <summary>Validates Name/National ID/Contact/Email. Returns the values on success, or null (with ErrorText already set) if something's invalid.</summary>
        private (string name, string nationalId, string contact, string email)? ValidateRequired()
        {
            var name = NameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Full Name is required.";
                return null;
            }

            var nationalId = NationalIdInput.Text.Trim();
            if (!Regex.IsMatch(nationalId, @"^\d{6,10}$"))
            {
                ErrorText.Text = "National ID must be 6 to 10 digits, numbers only.";
                return null;
            }

            var contact = ContactInput.Text.Trim();
            if (!Regex.IsMatch(contact, @"^07\d{8}$"))
            {
                ErrorText.Text = "Contact No must start with 07 and be exactly 10 digits (e.g. 0712345678).";
                return null;
            }

            var email = EmailInput.Text.Trim();
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorText.Text = "Enter a valid email address.";
                return null;
            }

            return (name, nationalId, contact, email);
        }

        private void ShowPhotoPreview(byte[]? photoBytes)
        {
            if (photoBytes == null || photoBytes.Length == 0)
            {
                PhotoPreview.Source = null;
                PhotoStatusText.Text = "No photo set (optional)";
                return;
            }

            try
            {
                using var stream = new MemoryStream(photoBytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();

                PhotoPreview.Source = bitmap;
                PhotoStatusText.Text = $"Photo set ({photoBytes.Length / 1024} KB)";
            }
            catch
            {
                PhotoPreview.Source = null;
                PhotoStatusText.Text = "Photo file could not be displayed.";
            }
        }

        private void BrowsePhoto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose an Employee Photo (optional)",
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _photoBytes = File.ReadAllBytes(dialog.FileName);
                    ShowPhotoPreview(_photoBytes);
                    ErrorText.Text = "";
                }
                catch (Exception ex)
                {
                    ErrorText.Text = $"Could not read that image file: {ex.Message}";
                }
            }
        }

        private void RemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            _photoBytes = null;
            ShowPhotoPreview(null);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            var validated = ValidateRequired();
            if (validated == null) return;
            var (name, nationalId, contact, email) = validated.Value;

            var existing = await _employeeService.GetAllAsync();
            if (existing.Any(x => x.EmployeeName.Trim().Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorText.Text = $"An employee named '{name}' already exists.";
                return;
            }
            if (existing.Any(x => (x.NationalID ?? "").Trim() == nationalId))
            {
                ErrorText.Text = $"National ID '{nationalId}' is already registered to another employee.";
                return;
            }

            try
            {
                var newId = await _employeeService.GetNextIdAsync();
                var code = string.IsNullOrWhiteSpace(EmployeeIdInput.Text)
                    ? $"EMP-{newId}"
                    : EmployeeIdInput.Text.Trim();

                var employee = new EmployeeRegistration
                {
                    EmpId = newId,
                    EmployeeID = code,
                    NationalID = nationalId,
                    EmployeeName = name,
                    Address = AddressInput.Text.Trim(),
                    City = CityInput.Text.Trim(),
                    ContactNo = contact,
                    Email = email,
                    DateOfJoining = JoiningDateInput.SelectedDate ?? DateTime.Today,
                    Active = ActiveInput.Text.Trim(),
                    Photo = _photoBytes
                };

                await _employeeService.AddAsync(employee);
                await _logService.LogAsync(CurrentSession.UserId, $"Added Employee '{name}' ({code})");

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
                ErrorText.Text = "Use Get Data, pick an employee, then edit and Update.";
                return;
            }

            var validated = ValidateRequired();
            if (validated == null) return;
            var (name, nationalId, contact, email) = validated.Value;

            var existing = await _employeeService.GetAllAsync();
            var nameTaken = existing.Any(x =>
                x.EmpId != _editing.EmpId &&
                x.EmployeeName.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
            if (nameTaken)
            {
                ErrorText.Text = $"Another employee is already named '{name}'.";
                return;
            }
            var idTaken = existing.Any(x => x.EmpId != _editing.EmpId && (x.NationalID ?? "").Trim() == nationalId);
            if (idTaken)
            {
                ErrorText.Text = $"National ID '{nationalId}' is already registered to another employee.";
                return;
            }

            var confirm = MessageBox.Show($"Update employee '{_editing.EmployeeName.Trim()}'?",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                _editing.EmployeeID = string.IsNullOrWhiteSpace(EmployeeIdInput.Text)
                    ? _editing.EmployeeID
                    : EmployeeIdInput.Text.Trim();
                _editing.NationalID = nationalId;
                _editing.EmployeeName = name;
                _editing.Address = AddressInput.Text.Trim();
                _editing.City = CityInput.Text.Trim();
                _editing.ContactNo = contact;
                _editing.Email = email;
                _editing.DateOfJoining = JoiningDateInput.SelectedDate ?? DateTime.Today;
                _editing.Active = ActiveInput.Text.Trim();
                _editing.Photo = _photoBytes;

                await _employeeService.UpdateAsync(_editing);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Employee '{name}'");

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
                ErrorText.Text = "Use Get Data, pick an employee, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove employee '{_editing.EmployeeName.Trim()}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var deletedName = _editing.EmployeeName.Trim();
            await _employeeService.DeleteAsync(_editing.EmpId);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Employee '{deletedName}'");

            MessageBox.Show("Removed.", "Clidapos");
            New_Click(sender, e);
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var listView = new EmployeeRegistrationListView();
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
