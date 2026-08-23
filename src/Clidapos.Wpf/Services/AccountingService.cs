using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class AccountBalanceRow
    {
        public string AccountName { get; set; } = "";
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal NetBalance => TotalDebit - TotalCredit;
    }

    public class AccountingService
    {
        /// <summary>Posts one double-entry transaction: writes the Journal header, then
        /// two LedgerBook rows (a Debit row against the debited account, a Credit row
        /// against the credited account) - all in one transaction.</summary>
        public async Task PostJournalEntryAsync(string debitAccount, string creditAccount, DateTime date, decimal amount, string? remarks)
        {
            using var db = new ClidaposDbContext();
            using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                var maxJournalId = await db.Set<JournalEntry>().Select(j => (int?)j.ID).MaxAsync() ?? 0;
                var journal = new JournalEntry
                {
                    ID = maxJournalId + 1,
                    DebitAccount = debitAccount.Trim(),
                    CreditAccount = creditAccount.Trim(),
                    Date = date,
                    Amount = amount,
                    Remarks = remarks?.Trim()
                };
                db.Set<JournalEntry>().Add(journal);
                await db.SaveChangesAsync();

                var maxLedgerId = await db.Set<LedgerBookEntry>().Select(l => (int?)l.Id).MaxAsync() ?? 0;

                // Debit row - the contra account (Name) shows what it was matched against.
                db.Set<LedgerBookEntry>().Add(new LedgerBookEntry
                {
                    Id = maxLedgerId + 1,
                    Date = date,
                    Name = creditAccount.Trim(),
                    LedgerNo = $"JE-{journal.ID}",
                    Label = remarks?.Trim() ?? "",
                    AccLedger = debitAccount.Trim(),
                    Debit = amount,
                    Credit = 0
                });

                // Credit row - same journal entry, other side.
                db.Set<LedgerBookEntry>().Add(new LedgerBookEntry
                {
                    Id = maxLedgerId + 2,
                    Date = date,
                    Name = debitAccount.Trim(),
                    LedgerNo = $"JE-{journal.ID}",
                    Label = remarks?.Trim() ?? "",
                    AccLedger = creditAccount.Trim(),
                    Debit = 0,
                    Credit = amount
                });

                await db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<List<JournalEntry>> GetAllJournalEntriesAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Set<JournalEntry>().OrderByDescending(j => j.Date).ToListAsync();
        }

        /// <summary>Every account name that has ever appeared, for autocomplete consistency.</summary>
        public async Task<List<string>> GetDistinctAccountNamesAsync()
        {
            using var db = new ClidaposDbContext();
            var debits = await db.Set<JournalEntry>().Select(j => j.DebitAccount).ToListAsync();
            var credits = await db.Set<JournalEntry>().Select(j => j.CreditAccount).ToListAsync();

            return debits.Concat(credits)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!.Trim())
                .Distinct()
                .OrderBy(n => n)
                .ToList();
        }

        /// <summary>Every account that has activity, with real Debit/Credit totals from LedgerBook.</summary>
        public async Task<List<AccountBalanceRow>> GetAccountBalancesAsync()
        {
            using var db = new ClidaposDbContext();
            var entries = await db.Set<LedgerBookEntry>().ToListAsync();

            return entries
                .Where(e => !string.IsNullOrWhiteSpace(e.AccLedger))
                .GroupBy(e => e.AccLedger!.Trim())
                .Select(g => new AccountBalanceRow
                {
                    AccountName = g.Key,
                    TotalDebit = g.Sum(e => e.Debit ?? 0),
                    TotalCredit = g.Sum(e => e.Credit ?? 0)
                })
                .OrderBy(r => r.AccountName)
                .ToList();
        }

        public async Task<List<LedgerBookEntry>> GetLedgerForAccountAsync(string accountName)
        {
            using var db = new ClidaposDbContext();
            var name = accountName.Trim();

            return await db.Set<LedgerBookEntry>()
                .Where(e => e.AccLedger != null && e.AccLedger.Trim() == name)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }
    }
}