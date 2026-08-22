using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
	public class SupplierBalanceRow
	{
		public int SupplierId { get; set; }
		public string SupplierCode { get; set; } = "";
		public string SupplierName { get; set; } = "";
		public decimal Balance { get; set; } // positive = we owe this supplier
	}

	public class SupplierLedgerService
	{
		/// <summary>Every supplier, with a running balance computed from real linked ledger entries.</summary>
		public async Task<List<SupplierBalanceRow>> GetSupplierBalancesAsync()
		{
			using var db = new ClidaposDbContext();

			var suppliers = await db.Suppliers.ToListAsync();
			var entries = await db.Set<SupplierLedgerEntry>().ToListAsync();

			return suppliers.Select(s =>
			{
				var code = s.SupplierID.Trim();
				var supplierEntries = entries.Where(e => e.PartyID != null && e.PartyID.Trim() == code).ToList();

				return new SupplierBalanceRow
				{
					SupplierId = s.ID,
					SupplierCode = code,
					SupplierName = s.Name.Trim(),
					Balance = supplierEntries.Sum(e => e.Credit) - supplierEntries.Sum(e => e.Debit)
				};
			})
			.OrderByDescending(r => r.Balance)
			.ToList();
		}

		/// <summary>Full transaction history for one supplier, most recent first.</summary>
		public async Task<List<SupplierLedgerEntry>> GetEntriesForSupplierAsync(string supplierCode)
		{
			using var db = new ClidaposDbContext();
			var code = supplierCode.Trim();

			return await db.Set<SupplierLedgerEntry>()
				.Where(e => e.PartyID != null && e.PartyID.Trim() == code)
				.OrderByDescending(e => e.Date)
				.ToListAsync();
		}

		/// <summary>
		/// Called automatically after a Purchase Entry completes - posts a Credit
		/// (money now owed to the supplier) linked by the real SupplierID.
		/// </summary>
		public async Task PostPurchaseEntryAsync(string supplierCode, string supplierName, string invoiceNo, decimal amount)
		{
			using var db = new ClidaposDbContext();

			var maxId = await db.Set<SupplierLedgerEntry>().Select(e => (int?)e.Id).MaxAsync() ?? 0;

			db.Set<SupplierLedgerEntry>().Add(new SupplierLedgerEntry
			{
				Id = maxId + 1,
				Date = DateTime.Now,
				Name = supplierName.Trim(),
				LedgerNo = invoiceNo.Trim(),
				Label = $"Purchase {invoiceNo.Trim()}",
				Debit = 0,
				Credit = amount,
				PartyID = supplierCode.Trim()
			});

			await db.SaveChangesAsync();
		}

		/// <summary>Manual entry - typically recording a payment made to the supplier (a Debit, reducing balance owed).</summary>
		public async Task AddManualEntryAsync(string supplierCode, string supplierName, string label, decimal debit, decimal credit)
		{
			using var db = new ClidaposDbContext();

			var maxId = await db.Set<SupplierLedgerEntry>().Select(e => (int?)e.Id).MaxAsync() ?? 0;

			db.Set<SupplierLedgerEntry>().Add(new SupplierLedgerEntry
			{
				Id = maxId + 1,
				Date = DateTime.Now,
				Name = supplierName.Trim(),
				LedgerNo = "",
				Label = label.Trim(),
				Debit = debit,
				Credit = credit,
				PartyID = supplierCode.Trim()
			});

			await db.SaveChangesAsync();
		}
	}
}