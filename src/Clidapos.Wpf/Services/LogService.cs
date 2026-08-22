using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class LogService
    {
        /// <summary>Records one action to the audit trail. Never throws - a logging failure should never break the action it's recording.</summary>
        public async Task LogAsync(string userId, string operation)
        {
            try
            {
                using var db = new ClidaposDbContext();

                // Id is a real IDENTITY column in the database - the DB assigns it,
                // we don't set it here.
                db.Logs.Add(new LogEntry
                {
                    UserID = userId,
                    Operation = operation,
                    Date = DateTime.Now
                });

                await db.SaveChangesAsync();
            }
            catch
            {
                // Logging is best-effort - swallow failures so a broken log write
                // never blocks or breaks the real action the user is trying to do.
            }
        }

        public async Task<List<LogEntry>> GetLogsAsync(DateTime from, DateTime to)
        {
            using var db = new ClidaposDbContext();
            return await db.Logs
                .Where(l => l.Date >= from && l.Date <= to)
                .OrderByDescending(l => l.Date)
                .ToListAsync();
        }
    }
}