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
        public string AccountType { get; set; } = "";
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal NetBalance => TotalDebit - TotalCredit;
    }

    public class TrialBalanceSummary
    {
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal TotalEquity { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }

        /// <summary>True when total debits equal total credits across every
        /// account - should always be true by construction, since every
        /// journal entry posts a balanced Debit/Credit pair, but is checked
        /// and shown explicitly rather than just assumed.</summary>
        public bool BooksBalanced { get; set; }

        public List<AccountBalanceRow> Accounts { get; set; } = new();
    }

    public class AccountingService
    {
        /// <summary>Posts one double-entry transaction: writes the Journal header, then
        /// two LedgerBook rows (a Debit row against the debited account, a Credit row
        /// against the credited account) - all in one transaction. Also records/updates
        /// each account's type in the Chart of Accounts (Asset/Liability/Equity/Income/
        /// Expense), needed for Trial Balance grouping and the financial statements.</summary>
        public async Task PostJournalEntryAsync(
            string debitAccount, string debitAccountType,
            string creditAccount, string creditAccountType,
            DateTime date, decimal amount, string? remarks)
        {
            using var db = new ClidaposDbContext();
            using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                await UpsertAccountTypeAsync(db, debitAccount, debitAccountType);
                await UpsertAccountTypeAsync(db, creditAccount, creditAccountType);

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

                // Debit row - the contra account (Name) shows what it was matched against.
                db.Set<LedgerBookEntry>().Add(new LedgerBookEntry
                {
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

        private static async Task UpsertAccountTypeAsync(ClidaposDbContext db, string accountName, string accountType)
        {
            var name = accountName.Trim();
            var existing = await db.Set<ChartOfAccount>().FirstOrDefaultAsync(a => a.AccountName == name);

            if (existing == null)
            {
                db.Set<ChartOfAccount>().Add(new ChartOfAccount { AccountName = name, AccountType = accountType.Trim() });
            }
            else if (!string.IsNullOrWhiteSpace(accountType) && existing.AccountType != accountType.Trim())
            {
                existing.AccountType = accountType.Trim();
            }
        }

        /// <summary>The known type for an account, or null if this account name
        /// has never been assigned one yet (a brand new account).</summary>
        public async Task<string?> GetAccountTypeAsync(string accountName)
        {
            using var db = new ClidaposDbContext();
            var name = accountName.Trim();
            var match = await db.Set<ChartOfAccount>().FirstOrDefaultAsync(a => a.AccountName == name);
            return match?.AccountType;
        }

        /// <summary>Every known account name mapped to its type, for the
        /// journal entry popup to auto-fill the type field when an existing
        /// account is picked.</summary>
        public async Task<Dictionary<string, string>> GetAllAccountTypesAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Set<ChartOfAccount>().ToDictionaryAsync(a => a.AccountName, a => a.AccountType);
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

        /// <summary>Every account that has activity, with real Debit/Credit totals from
        /// LedgerBook, and its type from the Chart of Accounts. Accounts with no
        /// recorded type yet (only possible for entries posted before this feature
        /// existed) show as "(unclassified)".</summary>
        public async Task<List<AccountBalanceRow>> GetAccountBalancesAsync()
        {
            using var db = new ClidaposDbContext();
            var entries = await db.Set<LedgerBookEntry>().ToListAsync();
            var types = await GetAllAccountTypesAsync();

            return entries
                .Where(e => !string.IsNullOrWhiteSpace(e.AccLedger))
                .GroupBy(e => e.AccLedger!.Trim())
                .Select(g => new AccountBalanceRow
                {
                    AccountName = g.Key,
                    AccountType = types.TryGetValue(g.Key, out var t) ? t : "(unclassified)",
                    TotalDebit = g.Sum(e => e.Debit ?? 0),
                    TotalCredit = g.Sum(e => e.Credit ?? 0)
                })
                .OrderBy(r => r.AccountName)
                .ToList();
        }

        /// <summary>The full Trial Balance: every account grouped by type, with
        /// section totals and a books-balanced check (total debits should
        /// always equal total credits across every account, by construction).</summary>
        public async Task<TrialBalanceSummary> GetTrialBalanceAsync()
        {
            var accounts = await GetAccountBalancesAsync();

            var summary = new TrialBalanceSummary { Accounts = accounts };

            summary.TotalAssets = accounts.Where(a => a.AccountType == "Asset").Sum(a => a.NetBalance);
            summary.TotalLiabilities = accounts.Where(a => a.AccountType == "Liability").Sum(a => a.NetBalance);
            summary.TotalEquity = accounts.Where(a => a.AccountType == "Equity").Sum(a => a.NetBalance);
            summary.TotalIncome = accounts.Where(a => a.AccountType == "Income").Sum(a => a.NetBalance);
            summary.TotalExpenses = accounts.Where(a => a.AccountType == "Expense").Sum(a => a.NetBalance);

            var totalDebit = accounts.Sum(a => a.TotalDebit);
            var totalCredit = accounts.Sum(a => a.TotalCredit);
            summary.BooksBalanced = Math.Abs(totalDebit - totalCredit) < 0.01m;

            return summary;
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