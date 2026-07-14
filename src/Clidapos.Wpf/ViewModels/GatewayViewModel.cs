using System.ComponentModel;
using System.Runtime.CompilerServices;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.ViewModels
{
    public class GatewayViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly ShiftService _shiftService = new();

        public Registration CurrentUser { get; }

        private bool _isShiftOpen;
        public bool IsShiftOpen
        {
            get => _isShiftOpen;
            set { _isShiftOpen = value; OnPropertyChanged(); }
        }

        public bool IsAdmin => CurrentUser.UserType.Trim().Equals("Admin", System.StringComparison.OrdinalIgnoreCase);

        public GatewayViewModel(Registration currentUser)
        {
            CurrentUser = currentUser;
            _ = RefreshShiftStatusAsync();
        }

        public async System.Threading.Tasks.Task RefreshShiftStatusAsync()
        {
            IsShiftOpen = await _shiftService.IsShiftOpenAsync();
        }

        public async System.Threading.Tasks.Task StartPeriodAsync()
        {
            await _shiftService.StartPeriodAsync();
            await RefreshShiftStatusAsync();
        }
    }
}