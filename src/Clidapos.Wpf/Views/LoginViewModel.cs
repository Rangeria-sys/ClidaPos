using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly LoginService _loginService = new();

        public const int PinLength = 4;

        private string _pin = string.Empty;
        public string Pin
        {
            get => _pin;
            set
            {
                _pin = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MaskedPin));
            }
        }

        public string MaskedPin => string.IsNullOrEmpty(_pin) ? "ENTER PIN" : new string('●', _pin.Length);

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public Clidapos.Wpf.Entities.Registration? LoggedInUser { get; private set; }

        public async System.Threading.Tasks.Task LoginAsync()
        {
            ErrorMessage = string.Empty;
            LoggedInUser = null;

            if (string.IsNullOrWhiteSpace(Pin))
            {
                ErrorMessage = "Enter your PIN.";
                return;
            }

            try
            {
                var user = await _loginService.ValidateByPinAsync(Pin);

                if (user == null)
                {
                    ErrorMessage = "Invalid PIN.";
                    Pin = string.Empty;
                    return;
                }

                Pin = string.Empty;
                LoggedInUser = user;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"ERROR: {ex.Message}", "Something went wrong");
            }
        }
    }
}