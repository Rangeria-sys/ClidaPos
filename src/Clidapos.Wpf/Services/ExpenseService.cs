using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class ExpenseService
    {
        public async Task<List<Expense>> GetAllAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Expenses.OrderBy(e => e.ExpenseName).ToListAsync();
        }

        public async Task AddAsync(Expense expense)
        {
            using var db = new ClidaposDbContext();

            var exists = await db.Expenses
                .AnyAsync(e => e.ExpenseName.Trim().ToUpper() == expense.ExpenseName.Trim().ToUpper());
            if (exists)
                throw new InvalidOperationException($"An expense named \"{expense.ExpenseName.Trim()}\" already exists.");

            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
        }

        // originalName is the name the record currently has in the database (before any edits in this save).
        // expense.ExpenseName may be different if the user renamed it.
        public async Task UpdateAsync(string originalName, Expense expense)
        {
            using var db = new ClidaposDbContext();

            var from = originalName.Trim();
            var to = expense.ExpenseName.Trim();

            if (!string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            {
                var clash = await db.Expenses.AnyAsync(e =>
                    e.ExpenseName.Trim().ToUpper() == to.ToUpper());
                if (clash)
                    throw new InvalidOperationException($"An expense named \"{to}\" already exists.");

                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE dbo.Expense SET ExpenseName = {to} WHERE LTRIM(RTRIM(ExpenseName)) = {from}");
            }

            var existing = await db.Expenses
                .FirstOrDefaultAsync(e => e.ExpenseName.Trim() == to);

            if (existing != null)
            {
                existing.ExpenseType = expense.ExpenseType;
                await db.SaveChangesAsync();
            }
        }

        public async Task RemoveAsync(string expenseName)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Expenses
                .FirstOrDefaultAsync(e => e.ExpenseName.Trim() == expenseName.Trim());

            if (existing != null)
            {
                db.Expenses.Remove(existing);
                await db.SaveChangesAsync();
            }
        }
    }
}