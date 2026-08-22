using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;

namespace Clidapos.Wpf.Services
{
    public class BackupHistoryRow
    {
        public DateTime BackupDate { get; set; }
        public string PhysicalPath { get; set; } = "";
        public decimal SizeMb { get; set; }
    }

    public class BackupResult
    {
        public bool Ok { get; set; }
        public string FilePath { get; set; } = "";
        public string Error { get; set; } = "";
    }

    public class BackupService
    {
        // Hardcoded, not user input - confirmed as the real database name via sqlcmd all night.
        // Safe to embed directly; only the destination path (user-influenced) is parameterized.
        private const string DatabaseName = "ClidaDB";

        public async Task<BackupResult> RunBackupAsync(string folderPath)
        {
            try
            {
                Directory.CreateDirectory(folderPath);

                var fileName = $"ClidaDB_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                var fullPath = Path.Combine(folderPath, fileName);

                using var db = new ClidaposDbContext();

                var sql = $"BACKUP DATABASE [{DatabaseName}] TO DISK = @path WITH INIT, NAME = @name";
                await db.Database.ExecuteSqlRawAsync(sql,
                    new SqlParameter("@path", fullPath),
                    new SqlParameter("@name", "ClidaDB Backup"));

                return new BackupResult { Ok = true, FilePath = fullPath };
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return new BackupResult { Ok = false, Error = detail };
            }
        }

        /// <summary>Reads real backup history straight from SQL Server's own msdb tracking tables.</summary>
        public async Task<List<BackupHistoryRow>> GetBackupHistoryAsync()
        {
            var rows = new List<BackupHistoryRow>();

            using var db = new ClidaposDbContext();
            using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT TOP 20
                    bs.backup_finish_date AS BackupDate,
                    bmf.physical_device_name AS PhysicalPath,
                    bs.backup_size / 1048576.0 AS SizeMb
                FROM msdb.dbo.backupset bs
                JOIN msdb.dbo.backupmediafamily bmf ON bs.media_set_id = bmf.media_set_id
                WHERE bs.database_name = @dbName
                ORDER BY bs.backup_finish_date DESC";

            var param = command.CreateParameter();
            param.ParameterName = "@dbName";
            param.Value = DatabaseName;
            command.Parameters.Add(param);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new BackupHistoryRow
                {
                    BackupDate = reader.GetDateTime(0),
                    PhysicalPath = reader.GetString(1),
                    SizeMb = reader.IsDBNull(2) ? 0 : Convert.ToDecimal(reader.GetValue(2))
                });
            }

            return rows;
        }
    }
}