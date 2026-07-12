using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class LoginService
    {
        public async Task<Registration?> ValidateByPinAsync(string pin)
        {
            using var db = new ClidaposDbContext();
            var user = await db.Registrations.FirstOrDefaultAsync(u =>
                    u.Password.Trim() == pin.Trim() &&
                    u.Active != null && u.Active.Trim() == "Y");
            return user;
        }
    }
}