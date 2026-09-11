using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public enum LogCategory { WorkPeriod, SalesVoids, Settings, Login, Other }

    public class LogRow
    {
        public DateTime Date { get; set; }
        public string UserID { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Operation { get; set; } = "";
        public LogCategory Category { get; set; }
        public bool IsAnomaly { get; set; }
    }

    public class SystemLogReportSummary
    {
        /// <summary>Every non-login event in the range.</summary>
        public List<LogRow> Events { get; set; } = new();

        /// <summary>Login events in the range - hidden from the main list by
        /// default since they're high-volume and rarely the thing being
        /// investigated, but available on request.</summary>
        public List<LogRow> LoginEvents { get; set; } = new();

        public List<LogRow> Anomalies { get; set; } = new();
    }

    public class LogService
    {
        private const decimal AnomalyMultiplier = 50m;

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

        private static readonly System.Text.RegularExpressions.Regex CountedCashPattern =
            new(@"Counted Cash ([\d,]+\.\d{2})", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static LogCategory Categorize(string operation)
        {
            var op = operation.ToLowerInvariant();

            if (op.Contains("work period")) return LogCategory.WorkPeriod;

            if (op.Contains("void") || op.Contains("sale") || op.Contains("refund") || op.Contains("hold") ||
                op.Contains("receipt") || op.Contains("payment for sale"))
                return LogCategory.SalesVoids;

            // Master-data / configuration management - covers every Added/Updated/
            // Deleted/Renamed action across every entity type in the app (suppliers,
            // items, employees, bank accounts, loyalty rules, etc.), plus explicit
            // settings/config changes and database backup/restore.
            if (op.StartsWith("added ") || op.StartsWith("updated ") || op.StartsWith("deleted ") ||
                op.StartsWith("renamed ") || op.Contains("setting") || op.Contains("config") ||
                op.Contains("backup") || op.Contains("restored database"))
                return LogCategory.Settings;

            if (op.Contains("logged in") || op.Contains("login") || op.Contains("logged out") || op.Contains("logout")) return LogCategory.Login;
            return LogCategory.Other;
        }

        /// <summary>The full System Logs report: every event in the range,
        /// categorized, with logins split out separately (hidden by default in
        /// the UI), and counted-cash anomalies flagged. The anomaly baseline
        /// (median counted-cash value) is computed from ALL logged counted-cash
        /// events ever recorded, not just those in the selected range, so
        /// there's enough history for a meaningful comparison even when
        /// viewing a single day.</summary>
        public async Task<SystemLogReportSummary> GetSystemLogReportAsync(DateTime from, DateTime to)
        {
            using var db = new ClidaposDbContext();

            var allLogs = await db.Logs.OrderByDescending(l => l.Date).ToListAsync();

            var userNameLookup = (await db.Registrations.ToListAsync())
                .ToDictionary(r => r.UserID.Trim(), r => r.Name.Trim(), StringComparer.OrdinalIgnoreCase);

            var allCountedCashValues = new List<decimal>();
            foreach (var log in allLogs)
            {
                var match = CountedCashPattern.Match(log.Operation);
                if (match.Success && decimal.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var val))
                    allCountedCashValues.Add(val);
            }

            decimal? median = null;
            if (allCountedCashValues.Count > 0)
            {
                var sorted = allCountedCashValues.OrderBy(v => v).ToList();
                var mid = sorted.Count / 2;
                median = sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2 : sorted[mid];
            }

            var toExclusive = to.Date.AddDays(1);
            var inRange = allLogs.Where(l => l.Date >= from.Date && l.Date < toExclusive).ToList();

            var rows = inRange.Select(l =>
            {
                var isAnomaly = false;
                if (median is > 0)
                {
                    var match = CountedCashPattern.Match(l.Operation);
                    if (match.Success && decimal.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var val))
                        isAnomaly = val > median.Value * AnomalyMultiplier;
                }

                return new LogRow
                {
                    Date = l.Date,
                    UserID = l.UserID,
                    UserName = userNameLookup.TryGetValue(l.UserID.Trim(), out var uname) ? uname : l.UserID,
                    Operation = l.Operation,
                    Category = Categorize(l.Operation),
                    IsAnomaly = isAnomaly
                };
            }).ToList();

            var summary = new SystemLogReportSummary
            {
                Events = rows.Where(r => r.Category != LogCategory.Login).ToList(),
                LoginEvents = rows.Where(r => r.Category == LogCategory.Login).ToList(),
                Anomalies = rows.Where(r => r.IsAnomaly).ToList()
            };

            return summary;
        }
    }
}