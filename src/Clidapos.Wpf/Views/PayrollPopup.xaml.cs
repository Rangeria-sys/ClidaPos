using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Media;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class PayrollPopup : Window
    {
        private readonly Registration _currentUser;
        private readonly EmployeeRegistration _employee;
        private readonly PayrollService _payrollService = new();
        private readonly LogService _logService = new();
        private PayrollRun? _editing;

        public PayrollPopup(Registration currentUser, EmployeeRegistration employee, PayrollRun? editingRun = null)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _employee = employee;

            SelectedEmployeeText.Text = employee.EmployeeName.Trim();

            if (editingRun != null)
            {
                LoadForEditing(editingRun);
                SetMode(isExisting: true);
            }
            else
            {
                MonthInput.Text = DateTime.Today.ToString("MMMM");
                YearInput.Text = DateTime.Today.Year.ToString();
                SetMode(isExisting: false);
            }
        }

        /// <summary>
        /// Run Payroll only ever creates a brand-new record for this employee.
        /// Once a run has been loaded via Get Data (double-click), only Update
        /// or Delete can change it - a payroll mistake should be corrected or
        /// removed, not silently duplicated by running it again.
        /// </summary>
        private void SetMode(bool isExisting)
        {
            SaveTileBtn.IsEnabled = !isExisting;
            UpdateTileBtn.IsEnabled = isExisting;
            DeleteTileBtn.IsEnabled = isExisting;
        }

        private void LoadForEditing(PayrollRun run)
        {
            _editing = run;
            MonthInput.Text = run.PayMonth ?? "";
            YearInput.Text = run.PayYear?.ToString() ?? DateTime.Today.Year.ToString();
            GrossInput.Text = run.GrossSalary.ToString("N2");

            var hadDeductions = (run.NSSFPer ?? 0) > 0 || (run.SHAPer ?? 0) > 0 || (run.HousingLevyPer ?? 0) > 0 || (run.PAYEPer ?? 0) > 0;
            DeductionsCheck.IsChecked = hadDeductions;
            DeductionsPanel.Visibility = hadDeductions ? Visibility.Visible : Visibility.Collapsed;

            NssfPerInput.Text = (run.NSSFPer ?? 6).ToString("N2");
            ShaPerInput.Text = (run.SHAPer ?? 2.75m).ToString("N2");
            HousingPerInput.Text = (run.HousingLevyPer ?? 1.5m).ToString("N2");
            PayePerInput.Text = (run.PAYEPer ?? 10).ToString("N2");
            Recompute();
        }

        private (decimal nssfPer, decimal nssf, decimal shaPer, decimal sha, decimal housingPer, decimal housing, decimal payePer, decimal paye, decimal netPay) ComputeDeductions(decimal gross)
        {
            if (DeductionsCheck?.IsChecked != true)
            {
                // Direct payment, no deductions - the amount entered is the full Net Pay.
                return (0, 0, 0, 0, 0, 0, 0, 0, gross);
            }

            var nssfPer = ParseDecimal(NssfPerInput?.Text);
            var shaPer = ParseDecimal(ShaPerInput?.Text);
            var housingPer = ParseDecimal(HousingPerInput?.Text);
            var payePer = ParseDecimal(PayePerInput?.Text);

            var nssf = Math.Round(gross * nssfPer / 100m, 2);
            var sha = Math.Round(gross * shaPer / 100m, 2);
            var housing = Math.Round(gross * housingPer / 100m, 2);
            var paye = Math.Round(gross * payePer / 100m, 2);
            var netPay = gross - nssf - sha - housing - paye;

            return (nssfPer, nssf, shaPer, sha, housingPer, housing, payePer, paye, netPay);
        }

        private void DeductionsCheck_Changed(object sender, RoutedEventArgs e)
        {
            DeductionsPanel.Visibility = DeductionsCheck.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            Recompute();
        }

        private void Recompute_Changed(object sender, TextChangedEventArgs e) => Recompute();

        private void Recompute()
        {
            if (NetPayText == null) return;

            var gross = ParseDecimal(GrossInput?.Text);
            var d = ComputeDeductions(gross);

            NssfAmountText.Text = d.nssf.ToString("N2");
            ShaAmountText.Text = d.sha.ToString("N2");
            HousingAmountText.Text = d.housing.ToString("N2");
            PayeAmountText.Text = d.paye.ToString("N2");
            NetPayText.Text = d.netPay.ToString("N2");
        }

        private static decimal ParseDecimal(string? text)
            => decimal.TryParse(text, out var value) ? value : 0;

        private bool ValidateGross(out decimal gross)
        {
            if (!decimal.TryParse(GrossInput.Text, out gross) || gross <= 0)
            {
                ErrorText.Text = "Enter a valid Gross Salary greater than zero.";
                return false;
            }
            return true;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            if (!ValidateGross(out var gross)) return;

            var d = ComputeDeductions(gross);

            var run = new PayrollRun
            {
                EmpId = _employee.EmpId,
                PaymentDate = DateTime.Now,
                PayMonth = MonthInput.Text.Trim(),
                PayYear = int.TryParse(YearInput.Text, out var year) ? year : DateTime.Today.Year,
                GrossSalary = gross,
                NSSFPer = d.nssfPer,
                NSSF = d.nssf,
                SHAPer = d.shaPer,
                SHA = d.sha,
                HousingLevyPer = d.housingPer,
                HousingLevy = d.housing,
                PAYEPer = d.payePer,
                PAYE = d.paye,
                NetPay = d.netPay
            };

            try
            {
                await _payrollService.AddAsync(run);
                await _logService.LogAsync(CurrentSession.UserId,
                    $"Ran Payroll for '{_employee.EmployeeName.Trim()}' - {MonthInput.Text.Trim()} {YearInput.Text.Trim()} - Net Pay {d.netPay:N2}");

                MessageBox.Show($"Payroll saved. Net Pay: {d.netPay:N2}", "Clidapos");

                var listView = new PayrollListView(_currentUser);
                listView.Show();
                Close();
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
                ErrorText.Text = "Use Get Data, pick a payroll run, then Update.";
                return;
            }
            if (!ValidateGross(out var gross)) return;

            var confirm = MessageBox.Show(
                $"Update this payroll run for '{_employee.EmployeeName.Trim()}'?",
                "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            var d = ComputeDeductions(gross);

            try
            {
                _editing.PayMonth = MonthInput.Text.Trim();
                _editing.PayYear = int.TryParse(YearInput.Text, out var year) ? year : DateTime.Today.Year;
                _editing.GrossSalary = gross;
                _editing.NSSFPer = d.nssfPer;
                _editing.NSSF = d.nssf;
                _editing.SHAPer = d.shaPer;
                _editing.SHA = d.sha;
                _editing.HousingLevyPer = d.housingPer;
                _editing.HousingLevy = d.housing;
                _editing.PAYEPer = d.payePer;
                _editing.PAYE = d.paye;
                _editing.NetPay = d.netPay;

                await _payrollService.UpdateAsync(_editing);
                await _logService.LogAsync(CurrentSession.UserId,
                    $"Updated Payroll for '{_employee.EmployeeName.Trim()}' - {MonthInput.Text.Trim()} {YearInput.Text.Trim()} - Net Pay {d.netPay:N2}");

                MessageBox.Show("Updated.", "Clidapos");

                var listView = new PayrollListView(_currentUser);
                listView.Show();
                Close();
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
                ErrorText.Text = "Use Get Data, pick a payroll run, then Delete.";
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete this payroll run for '{_employee.EmployeeName.Trim()}'? This cannot be undone.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _payrollService.DeleteAsync(_editing.Id);
            await _logService.LogAsync(CurrentSession.UserId,
                $"Deleted Payroll run for '{_employee.EmployeeName.Trim()}' - {MonthInput.Text.Trim()} {YearInput.Text.Trim()}");

            MessageBox.Show("Removed.", "Clidapos");

            var listView = new PayrollListView(_currentUser);
            listView.Show();
            Close();
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var picker = new PayrollEmployeePickerView(_currentUser);
            picker.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var picker = new PayrollEmployeePickerView(_currentUser);
            picker.Show();
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
