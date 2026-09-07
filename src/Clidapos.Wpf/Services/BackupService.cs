using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        /// <summary>
        /// Restores ClidaDB from a .bak file, completely REPLACING all current data.
        /// This has to run against the "master" database, not ClidaDB itself - SQL
        /// Server refuses to restore over a database anything is actively connected
        /// to, so this briefly forces ClidaDB into single-user mode (dropping every
        /// other connection, including this app's own) to make the restore possible,
        /// then returns it to normal multi-user mode. The app should be closed and
        /// reopened immediately after, since every connection any screen was holding
        /// is now stale.
        /// </summary>
        public async Task<BackupResult> RestoreBackupAsync(string backupFilePath)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                    return new BackupResult { Ok = false, Error = "That backup file no longer exists." };

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .Build();

                var appConnectionString = config.GetConnectionString("ClidaDB")
                    ?? throw new InvalidOperationException("No ClidaDB connection string configured.");

                var masterBuilder = new SqlConnectionStringBuilder(appConnectionString)
                {
                    InitialCatalog = "master"
                };

                await using var connection = new SqlConnection(masterBuilder.ConnectionString);
                await connection.OpenAsync();

                async Task RunAsync(string sql, SqlParameter? param = null)
                {
                    await using var cmd = new SqlCommand(sql, connection) { CommandTimeout = 300 };
                    if (param != null) cmd.Parameters.Add(param);
                    await cmd.ExecuteNonQueryAsync();
                }

                await RunAsync($"ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
                try
                {
                    await RunAsync(
                        $"RESTORE DATABASE [{DatabaseName}] FROM DISK = @path WITH REPLACE",
                        new SqlParameter("@path", backupFilePath));
                }
                finally
                {
                    // Always try to bring the database back to normal, even if the
                    // restore itself failed - leaving it stuck in single-user mode
                    // would lock everyone out entirely.
                    await RunAsync($"ALTER DATABASE [{DatabaseName}] SET MULTI_USER");
                }

                return new BackupResult { Ok = true, FilePath = backupFilePath };
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