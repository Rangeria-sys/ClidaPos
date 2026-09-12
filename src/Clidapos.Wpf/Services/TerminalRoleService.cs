using System;
using Microsoft.Extensions.Configuration;

namespace Clidapos.Wpf.Services
{
    /// <summary>Determines whether this specific terminal is "the server" -
    /// the one PC hosting SQL Server itself. Identified by this terminal's
    /// own appsettings.json connection string pointing at "localhost" or at
    /// its own machine name, rather than at some other PC's address - which
    /// is exactly how every client terminal's connection string is set up to
    /// point elsewhere.</summary>
    public static class TerminalRoleService
    {
        public static bool IsServerTerminal()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .Build();

                var connectionString = config.GetConnectionString("ClidaDB") ?? "";

                var serverValue = "";
                foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length == 2 && kv[0].Trim().Equals("Server", StringComparison.OrdinalIgnoreCase))
                    {
                        serverValue = kv[1].Trim();
                        break;
                    }
                }

                if (serverValue.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
                if (serverValue.Equals("127.0.0.1")) return true;
                if (serverValue.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)) return true;

                return false;
            }
            catch
            {
                // If this can't be determined for any reason, default to "not
                // the server" - the safer failure mode, since the only
                // consequence is the end-of-day close/report logic behaving
                // like a normal client terminal.
                return false;
            }
        }
    }
}
