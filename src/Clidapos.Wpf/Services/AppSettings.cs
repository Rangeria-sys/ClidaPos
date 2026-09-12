using System;
using Microsoft.Extensions.Configuration;

namespace Clidapos.Wpf.Services
{
    public enum StoreMode
    {
        Supermarket,
        Restaurant
    }

    /// <summary>
    /// Per-installation settings read from appsettings.json.
    /// Set once when the shop is commissioned.
    /// </summary>
    public static class AppSettings
    {
        private static readonly IConfigurationRoot _config;
        private static string? _cachedStoreName;

        static AppSettings()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();
        }

        public static StoreMode Mode =>
            (_config["AppMode"] ?? "Supermarket").Trim()
                .Equals("Restaurant", StringComparison.OrdinalIgnoreCase)
                    ? StoreMode.Restaurant
                    : StoreMode.Supermarket;

        public static string ModeLabel =>
            Mode == StoreMode.Restaurant ? "RESTAURANT MODE" : "SUPERMARKET MODE";

        /// <summary>The actual store name shown throughout receipts, reports,
        /// and every other screen - sourced from the live Business Profile
        /// (Hotel.HotelName), which the user actually edits via the UI, not
        /// from the static appsettings.json file (a separate, install-time
        /// config that was never meant to be the customer-facing name).
        /// Falls back to appsettings.json only if Business Profile hasn't
        /// been loaded yet or has no name set - never "Clidapos" itself,
        /// which is the software's own product name, not any customer's
        /// store.</summary>
        public static string StoreName =>
            !string.IsNullOrWhiteSpace(_cachedStoreName)
                ? _cachedStoreName
                : (string.IsNullOrWhiteSpace(_config["StoreName"]) ? "My Store" : _config["StoreName"]!.Trim());

        /// <summary>Refreshes the cached store name from Business Profile -
        /// call once at app startup, and again immediately after Business
        /// Profile is saved, so a name change takes effect live everywhere
        /// without needing to restart the app.</summary>
        public static async System.Threading.Tasks.Task RefreshStoreNameAsync()
        {
            try
            {
                var hotel = await new HotelProfileService().GetOrCreateAsync();
                if (!string.IsNullOrWhiteSpace(hotel.HotelName))
                    _cachedStoreName = hotel.HotelName.Trim();
            }
            catch
            {
                // Best-effort - if this fails (e.g. no database connection yet),
                // StoreName simply falls back to the appsettings.json value
                // until the next successful refresh.
            }
        }

        public static decimal VatPercent =>
            decimal.TryParse(_config["VatPercent"], out var v) ? v : 16m;

        public static string CurrencySymbol =>
            string.IsNullOrWhiteSpace(_config["CurrencySymbol"]) ? "KSh" : _config["CurrencySymbol"]!.Trim();
    }
}