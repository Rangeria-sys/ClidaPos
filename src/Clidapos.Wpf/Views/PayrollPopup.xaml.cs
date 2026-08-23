using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class PayrollPopup : Window
    {
        private readonly PayrollService _payrollService = new();
        private readonly EmployeeRegistrationService _employeeService = new();
        private readonly LogService _logService = new();
        private List<EmployeeRegistration> _allEmployees = new();
        private EmployeeRegistration? _selectedEmployee;

        public PayrollPopup()
        {
            InitializeComponent();
            MonthInput.Text = DateTime.Today.ToString("MMMM");
            YearInput.Text = DateTime.Today.Year.ToString();
            Loaded += async (s, e) => _allEmployees = await _employeeService.GetAllAsync();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            ResultsList.ItemsSource = string.IsNullOrEmpty(q)
                ? null
                : _allEmployees.Where(emp => emp.EmployeeName.Trim().ToLower().Contains(q)).ToList();
        }

        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsList.SelectedItem is EmployeeRegistration emp)
            {
                _selectedEmployee = emp;
                SelectedEmployeeText.Text = $"Selected: {emp.EmployeeName.Trim()}";
                SearchBox.Clear();
                ResultsList.ItemsSource = null;
            }
        }

        private void Recompute_Changed(object sender, TextChangedEventArgs e) => Recompute();

        private void Recompute()
        {
            if (NetPayText == null) return;

            var gross = ParseDecimal(GrossInput?.Text);
            var nssfPer = ParseDecimal(NssfPerInput?.Text);
            var shaPer = ParseDecimal(ShaPerInput?.Text);
            var housingPer = ParseDecimal(HousingPerInput?.Text);
            var payePer = ParseDecimal(PayePerInput?.Text);

            var nssf = Math.Round(gross * nssfPer / 100m, 2);
            var sha = Math.Round(gross * shaPer / 100m, 2);
            var housing = Math.Round(gross * housingPer / 100m, 2);
            var paye = Math.Round(gross * payePer / 100m, 2);
            var netPay = gross - nssf - sha - housing - paye;

            NssfAmountText.Text = nssf.ToString("N2");
            ShaAmountText.Text = sha.ToString("N2");
            HousingAmountText.Text = housing.ToString("N2");
            PayeAmountText.Text = paye.ToString("N2");
            NetPayText.Text = netPay.ToString("N2");
        }

        private static decimal ParseDecimal(string? text)
            => decimal.TryParse(text, out var value) ? value : 0;

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (_selectedEmployee == null)
            {
                ErrorText.Text = "Search and select an employee first.";
                return;
            }

            if (!decimal.TryParse(GrossInput.Text, out var gross) || gross <= 0)
            {
                ErrorText.Text = "Enter a valid Gross Salary greater than zero.";
                return;
            }

            var nssfPer = ParseDecimal(NssfPerInput.Text);
            var shaPer = ParseDecimal(ShaPerInput.Text);
            var housingPer = ParseDecimal(HousingPerInput.Text);
            var payePer = ParseDecimal(PayePerInput.Text);

            var nssf = Math.Round(gross * nssfPer / 100m, 2);
            var sha = Math.Round(gross * shaPer / 100m, 2);
            var housing = Math.Round(gross * housingPer / 100m, 2);
            var paye = Math.Round(gross * payePer / 100m, 2);
            var netPay = gross - nssf - sha - housing - paye;

            var newId = await _payrollService.GetNextIdAsync();

            var run = new PayrollRun
            {
                Id = newId,
                EmpId = _selectedEmployee.EmpId,
                PaymentDate = DateTime.Now,
                PayMonth = MonthInput.Text.Trim(),
                PayYear = int.TryParse(YearInput.Text, out var year) ? year : DateTime.Today.Year,
                GrossSalary = gross,
                NSSFPer = nssfPer,
                NSSF = nssf,
                SHAPer = shaPer,
                SHA = sha,
                HousingLevyPer = housingPer,
                HousingLevy = housing,
                PAYEPer = payePer,
                PAYE = paye,
                NetPay = netPay
            };

            try
            {
                await _payrollService.AddAsync(run);
                await _logService.LogAsync(CurrentSession.UserId,
                    $"Ran Payroll for '{_selectedEmployee.EmployeeName.Trim()}' - {MonthInput.Text.Trim()} {YearInput.Text.Trim()} - Net Pay {netPay:N2}");

                MessageBox.Show($"Payroll saved. Net Pay: {netPay:N2}", "Clidapos");
                Close();
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