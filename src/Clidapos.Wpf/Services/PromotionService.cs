using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
	public class PromotionService
	{
		public async Task<List<Promotion>> GetAllAsync()
		{
			using var db = new ClidaposDbContext();
			return await db.Set<Promotion>().OrderBy(p => p.Dish).ToListAsync();
		}

		public async Task<int> GetNextIdAsync()
		{
			using var db = new ClidaposDbContext();
			var maxId = await db.Set<Promotion>().Select(p => (int?)p.Id).MaxAsync();
			return (maxId ?? 0) + 1;
		}

		public async Task AddAsync(Promotion promotion)
		{
			using var db = new ClidaposDbContext();
			db.Set<Promotion>().Add(promotion);
			await db.SaveChangesAsync();
		}

		public async Task UpdateAsync(Promotion promotion)
		{
			using var db = new ClidaposDbContext();
			var existing = await db.Set<Promotion>().FirstOrDefaultAsync(p => p.Id == promotion.Id);
			if (existing == null) return;

			existing.Dish = promotion.Dish;
			existing.Rate = promotion.Rate;
			existing.PDay = promotion.PDay;
			existing.TimeFrom = promotion.TimeFrom;
			existing.TimeTo = promotion.TimeTo;
			existing.Active = promotion.Active;

			await db.SaveChangesAsync();
		}

		public async Task DeleteAsync(int id)
		{
			using var db = new ClidaposDbContext();
			var existing = await db.Set<Promotion>().FirstOrDefaultAsync(p => p.Id == id);
			if (existing != null)
			{
				db.Set<Promotion>().Remove(existing);
				await db.SaveChangesAsync();
			}
		}
	}
}