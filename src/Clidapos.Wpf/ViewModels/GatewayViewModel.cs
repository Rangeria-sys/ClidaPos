using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.ViewModels
{
    public class GatewayViewModel
    {
        public Registration CurrentUser { get; }

        public bool IsAdmin => PermissionService.CanAccessBackOffice(CurrentUser);

        public StoreMode Mode => AppSettings.Mode;

        public GatewayViewModel(Registration currentUser)
        {
            CurrentUser = currentUser;
        }
    }
}