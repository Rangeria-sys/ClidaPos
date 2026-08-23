using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class BankAccountRow
    {
        public string AccountNo { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string AccountType { get; set; } = "";
        public string BankName { get; set; } = "";
        public string BranchName { get; set; } = "";
        public string Active { get; set; } = "";
        public decimal RunningBalance { get; set; }
    }

    public class BankingService
    {
        public async Task EnsureBankAsync(string bankName)
        {
            using var db = new ClidaposDbContext();
            var name = bankName.Trim();

            var exists = await db.Set<Bank>().AnyAsync(b => b.BankName.Trim() == name);
            if (!exists)
            {
                db.Set<Bank>().Add(new Bank { BankName = name });
                await db.SaveChangesAsync();
            }
        }

        public async Task<List<string>> GetBankNamesAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Set<Bank>().Select(b => b.BankName.Trim()).OrderBy(n => n).ToListAsync();
        }

        /// <summary>Creates the branch if it doesn't already exist for this bank, returns its Id either way.</summary>
        public async Task<int> EnsureBranchAsync(string bankName, string branchName, string? address, string? contactNo, string? swiftCode, string? ifscCode)
        {
            using var db = new ClidaposDbContext();
            var bank = bankName.Trim();
            var branch = branchName.Trim();

            var existing = await db.Set<BankBranch>()
                .FirstOrDefaultAsync(b => b.BankName.Trim() == bank && b.BranchName != null && b.BranchName.Trim() == branch);

            if (existing != null)
                return existing.Id;

            var maxId = await db.Set<BankBranch>().Select(b => (int?)b.Id).MaxAsync() ?? 0;

            var newBranch = new BankBranch
            {
                Id = maxId + 1,
                BankName = bank,
                BranchName = branch,
                Address = address,
                ContactNo = contactNo,
                SwiftCode = swiftCode,
                IFSCCode = ifscCode
            };
            db.Set<BankBranch>().Add(newBranch);
            await db.SaveChangesAsync();

            return newBranch.Id;
        }

        public async Task<List<BankAccountRegistration>> GetAllAccountsAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Set<BankAccountRegistration>().OrderBy(a => a.AccountName).ToListAsync();
        }

        public async Task AddAccountAsync(BankAccountRegistration account)
        {
            using var db = new ClidaposDbContext();

            var exists = await db.Set<BankAccountRegistration>()
                .AnyAsync(a => a.AccountNo.Trim() == account.AccountNo.Trim());
            if (exists)
                throw new InvalidOperationException($"An account with number \"{account.AccountNo.Trim()}\" already exists.");

            db.Set<BankAccountRegistration>().Add(account);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAccountAsync(BankAccountRegistration account)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<BankAccountRegistration>()
                .FirstOrDefaultAsync(a => a.AccountNo.Trim() == account.AccountNo.Trim());
            if (existing == null) return;

            existing.AccountName = account.AccountName;
            existing.AccountType = account.AccountType;
            existing.OpeningDate = account.OpeningDate;
            existing.BalanceAmount = account.BalanceAmount;
            existing.Active = account.Active;
            existing.BranchID = account.BranchID;

            await db.SaveChangesAsync();
        }

        /// <summary>Every account with a real running balance: opening balance plus all ledger activity.</summary>
        public async Task<List<BankAccountRow>> GetAccountBalancesAsync()
        {
            using var db = new ClidaposDbContext();

            var accounts = await db.Set<BankAccountRegistration>().ToListAsync();
            var branches = await db.Set<BankBranch>().ToListAsync();
            var entries = await db.Set<BankAccountLedger>().ToListAsync();

            return accounts.Select(a =>
            {
                var accNo = a.AccountNo.Trim();
                var branch = a.BranchID.HasValue ? branches.FirstOrDefault(br => br.Id == a.BranchID.Value) : null;
                var accountEntries = entries.Where(e => e.AccNo != null && e.AccNo.Trim() == accNo).ToList();
                var activity = accountEntries.Sum(e => e.Credit ?? 0) - accountEntries.Sum(e => e.Debit ?? 0);

                return new BankAccountRow
                {
                    AccountNo = accNo,
                    AccountName = a.AccountName?.Trim() ?? "",
                    AccountType = a.AccountType?.Trim() ?? "",
                    BankName = branch?.BankName.Trim() ?? "",
                    BranchName = branch?.BranchName?.Trim() ?? "",
                    Active = a.Active?.Trim() ?? "",
                    RunningBalance = (a.BalanceAmount ?? 0) + activity
                };
            })
            .OrderBy(r => r.AccountName)
            .ToList();
        }

        public async Task<List<BankAccountLedger>> GetLedgerEntriesForAccountAsync(string accountNo)
        {
            using var db = new ClidaposDbContext();
            var accNo = accountNo.Trim();

            return await db.Set<BankAccountLedger>()
                .Where(e => e.AccNo != null && e.AccNo.Trim() == accNo)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        /// <summary>Records a deposit (Credit) or withdrawal (Debit) against an account.</summary>
        public async Task AddLedgerEntryAsync(string accountNo, string label, decimal debit, decimal credit)
        {
            using var db = new ClidaposDbContext();

            var maxId = await db.Set<BankAccountLedger>().Select(e => (int?)e.Id).MaxAsync() ?? 0;

            db.Set<BankAccountLedger>().Add(new BankAccountLedger
            {
                Id = maxId + 1,
                Date = DateTime.Now,
                AccNo = accountNo.Trim(),
                LedgerNo = "",
                Label = label.Trim(),
                Debit = debit,
                Credit = credit
            });

            await db.SaveChangesAsync();
        }
    }
}