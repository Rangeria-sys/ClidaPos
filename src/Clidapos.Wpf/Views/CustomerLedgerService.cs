using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class CustomerBalanceRow
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public decimal Balance { get; set; } // positive = this customer owes us
    }

    public class CustomerLedgerService
    {
        /// <summary>Every customer, with a running balance from real linked ledger entries.
        /// Debit increases what they owe, Credit (a payment) reduces it - standard
        /// accounts-receivable convention, the opposite of Supplier Ledger.</summary>
        public async Task<List<CustomerBalanceRow>> GetCustomerBalancesAsync()
        {
            using var db = new ClidaposDbContext();

            var customers = await db.Set<Customer>().ToListAsync();
            var entries = await db.Set<CustomerLedgerEntry>().ToListAsync();

            return customers.Select(c =>
            {
                var customerEntries = entries.Where(e => e.CreditCustomer_ID == c.ID).ToList();

                return new CustomerBalanceRow
                {
                    CustomerId = c.ID,
                    CustomerCode = c.CustomerID.Trim(),
                    CustomerName = c.Name.Trim(),
                    Balance = customerEntries.Sum(e => e.Debit ?? 0) - customerEntries.Sum(e => e.Credit ?? 0)
                };
            })
            .OrderByDescending(r => r.Balance)
            .ToList();
        }

        /// <summary>Full transaction history for one customer, most recent first.</summary>
        public async Task<List<CustomerLedgerEntry>> GetEntriesForCustomerAsync(int customerId)
        {
            using var db = new ClidaposDbContext();

            return await db.Set<CustomerLedgerEntry>()
                .Where(e => e.CreditCustomer_ID == customerId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        /// <summary>Records credit given to the customer (a Debit - increases what they owe).</summary>
        public async Task AddCreditGivenAsync(int customerId, string label, decimal amount)
        {
            await AddEntryAsync(customerId, label, debit: amount, credit: 0);
        }

        /// <summary>Records a payment received from the customer (a Credit - reduces what they owe).</summary>
        public async Task AddPaymentReceivedAsync(int customerId, string label, decimal amount)
        {
            await AddEntryAsync(customerId, label, debit: 0, credit: amount);
        }

        private async Task AddEntryAsync(int customerId, string label, decimal debit, decimal credit)
        {
            using var db = new ClidaposDbContext();

            var maxId = await db.Set<CustomerLedgerEntry>().Select(e => (int?)e.Id).MaxAsync() ?? 0;

            db.Set<CustomerLedgerEntry>().Add(new CustomerLedgerEntry
            {
                Id = maxId + 1,
                Date = DateTime.Now,
                LedgerNo = "",
                Label = label.Trim(),
                Debit = debit,
                Credit = credit,
                CreditCustomer_ID = customerId
            });

            await db.SaveChangesAsync();
        }
    }
}