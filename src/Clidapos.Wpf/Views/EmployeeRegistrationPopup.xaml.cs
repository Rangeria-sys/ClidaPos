using System;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class EmployeeRegistrationPopup : Window
    {
        private readonly EmployeeRegistrationService _employeeService = new();
        private readonly LogService _logService = new();
        private EmployeeRegistration? _editing;

        public EmployeeRegistrationPopup(EmployeeRegistration? editEmployee = null)
        {
            InitializeComponent();

            if (editEmployee != null)
            {
                LoadForEditing(editEmployee);
            }
            else
            {
                JoiningDateInput.SelectedDate = DateTime.Today;
                ActiveInput.Text = "Y";
            }
        }

        private void LoadForEditing(EmployeeRegistration employee)
        {
            _editing = employee;
            EmployeeIdInput.Text = employee.EmployeeID.Trim();
            NameInput.Text = employee.EmployeeName.Trim();
            AddressInput.Text = employee.Address.Trim();
            CityInput.Text = employee.City.Trim();
            ContactInput.Text = employee.ContactNo.Trim();
            EmailInput.Text = employee.Email.Trim();
            JoiningDateInput.SelectedDate = employee.DateOfJoining;
            ActiveInput.Text = employee.Active?.Trim() ?? "Y";
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            EmployeeIdInput.Text = "";
            NameInput.Text = "";
            AddressInput.Text = "";
            CityInput.Text = "";
            ContactInput.Text = "";
            EmailInput.Text = "";
            JoiningDateInput.SelectedDate = DateTime.Today;
            ActiveInput.Text = "Y";
            ErrorText.Text = "";
            NameInput.Focus();
        }

        private bool ValidateRequired()
        {
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                ErrorText.Text = "Full Name is required.";
                return false;
            }
            return true;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            if (!ValidateRequired()) return;

            var newId = await _employeeService.GetNextIdAsync();
            var code = string.IsNullOrWhiteSpace(EmployeeIdInput.Text)
                ? $"EMP-{newId}"
                : EmployeeIdInput.Text.Trim();

            var employee = new EmployeeRegistration
            {
                EmpId = newId,
                EmployeeID = code,
                EmployeeName = NameInput.Text.Trim(),
                Address = AddressInput.Text.Trim(),
                City = CityInput.Text.Trim(),
                ContactNo = ContactInput.Text.Trim(),
                Email = EmailInput.Text.Trim(),
                DateOfJoining = JoiningDateInput.SelectedDate ?? DateTime.Today,
                Active = ActiveInput.Text.Trim()
            };

            try
            {
                await _employeeService.AddAsync(employee);
                await _logService.LogAsync(CurrentSession.UserId, $"Added Employee '{employee.EmployeeName}' ({code})");
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
            ErrorText.Text = "";

            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick an employee, then edit and Update.";
                return;
            }
            if (!ValidateRequired()) return;

            try
            {
                _editing.EmployeeID = string.IsNullOrWhiteSpace(EmployeeIdInput.Text)
                    ? _editing.EmployeeID
                    : EmployeeIdInput.Text.Trim();
                _editing.EmployeeName = NameInput.Text.Trim();
                _editing.Address = AddressInput.Text.Trim();
                _editing.City = CityInput.Text.Trim();
                _editing.ContactNo = ContactInput.Text.Trim();
                _editing.Email = EmailInput.Text.Trim();
                _editing.DateOfJoining = JoiningDateInput.SelectedDate ?? DateTime.Today;
                _editing.Active = ActiveInput.Text.Trim();

                await _employeeService.UpdateAsync(_editing);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Employee '{_editing.EmployeeName}'");
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
                ErrorText.Text = "Use Get Data, pick an employee, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove employee '{_editing.EmployeeName.Trim()}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var deletedName = _editing.EmployeeName.Trim();
            await _employeeService.DeleteAsync(_editing.EmpId);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Employee '{deletedName}'");
            New_Click(sender, e);
            ErrorText.Text = "Removed.";
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
    }
}