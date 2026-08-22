using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    /// <summary>Manages the single business-profile row in the Hotel table.</summary>
    public class HotelProfileService
    {
        public async Task<Hotel> GetOrCreateAsync()
        {
            using var db = new ClidaposDbContext();

            var hotel = await db.Set<Hotel>().FirstOrDefaultAsync();
            if (hotel != null) return hotel;

            // Id is a real IDENTITY column - the database assigns it, we don't set it here.
            hotel = new Hotel();
            db.Set<Hotel>().Add(hotel);
            await db.SaveChangesAsync();

            return hotel;
        }

        public async Task SaveAsync(Hotel hotel)
        {
            using var db = new ClidaposDbContext();

            var existing = await db.Set<Hotel>().FirstOrDefaultAsync(h => h.Id == hotel.Id);
            if (existing == null) return;

            existing.HotelName = hotel.HotelName;
            existing.AddressLine1 = hotel.AddressLine1;
            existing.AddressLine2 = hotel.AddressLine2;
            existing.AddressLine3 = hotel.AddressLine3;
            existing.ContactNo = hotel.ContactNo;
            existing.EmailID = hotel.EmailID;
            existing.TIN = hotel.TIN;
            existing.STNo = hotel.STNo;
            existing.CIN = hotel.CIN;
            existing.BaseCurrency = hotel.BaseCurrency;
            existing.CurrencyCode = hotel.CurrencyCode;
            existing.TicketFooterMessage = hotel.TicketFooterMessage;
            existing.ShowLogo = hotel.ShowLogo;
            existing.CapitalAccount = hotel.CapitalAccount;
            existing.Logo = hotel.Logo;

            await db.SaveChangesAsync();
        }
    }
}