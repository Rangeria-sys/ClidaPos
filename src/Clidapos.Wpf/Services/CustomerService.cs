using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class CustomerService
    {
        public async Task<List<Customer>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Set<Customer>().OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<int> GetNextIdAsync()
        {
            using var db = new ClidaposDbContext();
            var maxId = await db.Set<Customer>().Select(c => (int?)c.ID).MaxAsync();
            return (maxId ?? 0) + 1;
        }

        public async Task AddAsync(Customer customer)
        {
            using var db = new ClidaposDbContext();

            var exists = await db.Set<Customer>()
                .AnyAsync(c => c.CustomerID.Trim().ToUpper() == customer.CustomerID.Trim().ToUpper());
            if (exists)
                throw new InvalidOperationException($"A customer with code \"{customer.CustomerID.Trim()}\" already exists.");

            db.Set<Customer>().Add(customer);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Customer customer)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<Customer>().FirstOrDefaultAsync(c => c.ID == customer.ID);
            if (existing == null) return;

            existing.CustomerID = customer.CustomerID;
            existing.Name = customer.Name;
            existing.ContactNo = customer.ContactNo;
            existing.Email = customer.Email;

            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<Customer>().FirstOrDefaultAsync(c => c.ID == id);
            if (existing != null)
            {
                db.Set<Customer>().Remove(existing);
                await db.SaveChangesAsync();
            }
        }
    }
}