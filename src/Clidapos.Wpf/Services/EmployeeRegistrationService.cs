using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class EmployeeRegistrationService
    {
        public async Task<List<EmployeeRegistration>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Set<EmployeeRegistration>().OrderBy(e => e.EmployeeName).ToListAsync();
        }

        public async Task<int> GetNextIdAsync()
        {
            using var db = new ClidaposDbContext();
            var maxId = await db.Set<EmployeeRegistration>().Select(e => (int?)e.EmpId).MaxAsync();
            return (maxId ?? 0) + 1;
        }

        public async Task AddAsync(EmployeeRegistration employee)
        {
            using var db = new ClidaposDbContext();

            // Photo is NOT NULL in the real database - an empty placeholder satisfies
            // that constraint until a real photo-upload feature is built.
            employee.Photo ??= System.Array.Empty<byte>();

            db.Set<EmployeeRegistration>().Add(employee);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAsync(EmployeeRegistration employee)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<EmployeeRegistration>().FirstOrDefaultAsync(e => e.EmpId == employee.EmpId);
            if (existing == null) return;

            existing.EmployeeID = employee.EmployeeID;
            existing.EmployeeName = employee.EmployeeName;
            existing.Address = employee.Address;
            existing.City = employee.City;
            existing.ContactNo = employee.ContactNo;
            existing.Email = employee.Email;
            existing.DateOfJoining = employee.DateOfJoining;
            existing.Active = employee.Active;

            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int empId)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<EmployeeRegistration>().FirstOrDefaultAsync(e => e.EmpId == empId);
            if (existing != null)
            {
                db.Set<EmployeeRegistration>().Remove(existing);
                await db.SaveChangesAsync();
            }
        }
    }
}