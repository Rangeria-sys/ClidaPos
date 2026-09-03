using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class CreditCustomerService
    {
        public async Task<List<CreditCustomer>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Set<CreditCustomer>()
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<CreditCustomer?> GetByIdAsync(int ccId)
        {
            using var db = new ClidaposDbContext();
            return await db.Set<CreditCustomer>().FirstOrDefaultAsync(c => c.CC_ID == ccId);
        }

        public async Task<List<CreditCustomer>> SearchAsync(string term)
        {
            var t = (term ?? "").Trim();
            if (t.Length == 0) return await GetAllAsync();

            using var db = new ClidaposDbContext();
            return await db.Set<CreditCustomer>()
                .Where(c => c.Name!.StartsWith(t) || c.CreditCustomerID.StartsWith(t))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task AddAsync(CreditCustomer customer)
        {
            using var db = new ClidaposDbContext();

            var trimmedName = (customer.Name ?? "").Trim();

            var duplicateName = await db.Set<CreditCustomer>()
                .AnyAsync(c => c.Name != null && c.Name.Trim().ToUpper() == trimmedName.ToUpper());
            if (duplicateName)
                throw new InvalidOperationException($"A credit customer named \"{trimmedName}\" already exists. Use Get Data to find and edit them instead.");

            var maxId = await db.Set<CreditCustomer>().Select(c => (int?)c.CC_ID).MaxAsync() ?? 0;
            customer.CC_ID = maxId + 1;

            if (string.IsNullOrWhiteSpace(customer.CreditCustomerID))
            {
                customer.CreditCustomerID = $"CC-{customer.CC_ID}";
            }
            else
            {
                // A manually-typed code must also be genuinely unique - otherwise two
                // different people can end up displayed under the same label, which is
                // exactly the confusion that happened before this check existed.
                var trimmedCode = customer.CreditCustomerID.Trim();
                var duplicateCode = await db.Set<CreditCustomer>()
                    .AnyAsync(c => c.CreditCustomerID.Trim().ToUpper() == trimmedCode.ToUpper());
                if (duplicateCode)
                    throw new InvalidOperationException($"Customer code \"{trimmedCode}\" is already in use by someone else. Leave it blank to auto-generate a unique one, or choose a different code.");

                customer.CreditCustomerID = trimmedCode;
            }

            customer.RegistrationDate ??= DateTime.Now;
            customer.Active ??= "Y";

            db.Set<CreditCustomer>().Add(customer);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAsync(CreditCustomer customer)
        {
            using var db = new ClidaposDbContext();

            var trimmedName = (customer.Name ?? "").Trim();

            var duplicateName = await db.Set<CreditCustomer>()
                .AnyAsync(c => c.CC_ID != customer.CC_ID
                            && c.Name != null && c.Name.Trim().ToUpper() == trimmedName.ToUpper());
            if (duplicateName)
                throw new InvalidOperationException($"Another credit customer named \"{trimmedName}\" already exists.");

            if (!string.IsNullOrWhiteSpace(customer.CreditCustomerID))
            {
                var trimmedCode = customer.CreditCustomerID.Trim();
                var duplicateCode = await db.Set<CreditCustomer>()
                    .AnyAsync(c => c.CC_ID != customer.CC_ID
                                && c.CreditCustomerID.Trim().ToUpper() == trimmedCode.ToUpper());
                if (duplicateCode)
                    throw new InvalidOperationException($"Customer code \"{trimmedCode}\" is already in use by someone else.");
            }

            var existing = await db.Set<CreditCustomer>().FirstOrDefaultAsync(c => c.CC_ID == customer.CC_ID);
            if (existing == null) return;

            existing.CreditCustomerID = string.IsNullOrWhiteSpace(customer.CreditCustomerID)
                ? existing.CreditCustomerID
                : customer.CreditCustomerID.Trim();
            existing.Name = customer.Name;
            existing.ContactNo = customer.ContactNo;
            existing.Address = customer.Address;
            existing.OpeningBalance = customer.OpeningBalance;
            existing.OpeningBalanceType = customer.OpeningBalanceType;
            existing.Active = customer.Active;
            existing.EmailID = customer.EmailID;

            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int ccId)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<CreditCustomer>().FirstOrDefaultAsync(c => c.CC_ID == ccId);
            if (existing == null) return;

            db.Set<CreditCustomer>().Remove(existing);
            await db.SaveChangesAsync();
        }
    }
}