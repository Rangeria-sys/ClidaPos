using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class LoyaltyMemberRow
    {
        public int MemberID { get; set; }
        public string Name { get; set; } = "";
        public string ContactNo { get; set; } = "";
        public string CardNo { get; set; } = "";
        public decimal PointsBalance { get; set; }
    }

    public class LoyaltyService
    {
        public async Task<List<LoyaltyMember>> GetAllMembersAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Set<LoyaltyMember>().OrderBy(m => m.Name).ToListAsync();
        }

        public async Task<int> GetNextMemberIdAsync()
        {
            using var db = new ClidaposDbContext();
            var maxId = await db.Set<LoyaltyMember>().Select(m => (int?)m.MemberID).MaxAsync();
            return (maxId ?? 0) + 1;
        }

        public async Task AddMemberAsync(LoyaltyMember member)
        {
            using var db = new ClidaposDbContext();
            db.Set<LoyaltyMember>().Add(member);
            await db.SaveChangesAsync();
        }

        public async Task UpdateMemberAsync(LoyaltyMember member)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<LoyaltyMember>().FirstOrDefaultAsync(m => m.MemberID == member.MemberID);
            if (existing == null) return;

            existing.Name = member.Name;
            existing.CardNo = member.CardNo;
            existing.ContactNo = member.ContactNo;
            existing.Address = member.Address;
            existing.Active = member.Active;

            await db.SaveChangesAsync();
        }

        /// <summary>Every member with a real points balance computed entirely from ledger activity
        /// (LoyaltyMember itself has no points column of its own).</summary>
        public async Task<List<LoyaltyMemberRow>> GetMemberBalancesAsync()
        {
            using var db = new ClidaposDbContext();

            var members = await db.Set<LoyaltyMember>().ToListAsync();
            var entries = await db.Set<LoyaltyMemberLedgerBook>().ToListAsync();

            return members.Select(m =>
            {
                var memberEntries = entries.Where(e => e.MemberID == m.MemberID).ToList();
                var balance = memberEntries.Sum(e => e.PointsEarned) - memberEntries.Sum(e => e.PointsRedeem);

                return new LoyaltyMemberRow
                {
                    MemberID = m.MemberID,
                    Name = m.Name?.Trim() ?? "",
                    ContactNo = m.ContactNo?.Trim() ?? "",
                    CardNo = m.CardNo?.Trim() ?? "",
                    PointsBalance = balance
                };
            })
            .OrderBy(r => r.Name)
            .ToList();
        }

        public async Task<List<LoyaltyMemberLedgerBook>> GetLedgerForMemberAsync(int memberId)
        {
            using var db = new ClidaposDbContext();

            return await db.Set<LoyaltyMemberLedgerBook>()
                .Where(e => e.MemberID == memberId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task AddPointsEarnedAsync(int memberId, string label, decimal points)
        {
            await AddLedgerEntryAsync(memberId, label, earned: points, redeemed: 0);
        }

        public async Task AddPointsRedeemedAsync(int memberId, string label, decimal points)
        {
            await AddLedgerEntryAsync(memberId, label, earned: 0, redeemed: points);
        }

        private async Task AddLedgerEntryAsync(int memberId, string label, decimal earned, decimal redeemed)
        {
            using var db = new ClidaposDbContext();

            var maxId = await db.Set<LoyaltyMemberLedgerBook>().Select(e => (int?)e.Id).MaxAsync() ?? 0;

            db.Set<LoyaltyMemberLedgerBook>().Add(new LoyaltyMemberLedgerBook
            {
                Id = maxId + 1,
                Date = DateTime.Now,
                LedgerNo = "",
                Label = label.Trim(),
                PointsEarned = earned,
                PointsRedeem = redeemed,
                MemberID = memberId
            });

            await db.SaveChangesAsync();
        }

        // ---------- Loyalty Setting: a named list of earning rules, not a singleton ----------

        public async Task<List<LoyaltySetting>> GetAllSettingsAsync()
        {
            using var db = new ClidaposDbContext();
            return await db.Set<LoyaltySetting>().OrderBy(s => s.LoyaltyName).ToListAsync();
        }

        public async Task AddSettingAsync(LoyaltySetting setting)
        {
            using var db = new ClidaposDbContext();

            var exists = await db.Set<LoyaltySetting>()
                .AnyAsync(s => s.LoyaltyName.Trim().ToUpper() == setting.LoyaltyName.Trim().ToUpper());
            if (exists)
                throw new InvalidOperationException($"A rule named \"{setting.LoyaltyName.Trim()}\" already exists.");

            db.Set<LoyaltySetting>().Add(setting);
            await db.SaveChangesAsync();
        }

        public async Task UpdateSettingAsync(LoyaltySetting setting)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<LoyaltySetting>()
                .FirstOrDefaultAsync(s => s.LoyaltyName.Trim() == setting.LoyaltyName.Trim());
            if (existing == null) return;

            existing.Amount = setting.Amount;
            existing.Points = setting.Points;

            await db.SaveChangesAsync();
        }

        public async Task DeleteSettingAsync(string loyaltyName)
        {
            using var db = new ClidaposDbContext();
            var existing = await db.Set<LoyaltySetting>()
                .FirstOrDefaultAsync(s => s.LoyaltyName.Trim() == loyaltyName.Trim());
            if (existing != null)
            {
                db.Set<LoyaltySetting>().Remove(existing);
                await db.SaveChangesAsync();
            }
        }
    }
}