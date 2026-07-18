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

        public static string StoreName =>
            string.IsNullOrWhiteSpace(_config["StoreName"]) ? "CLIDA POS" : _config["StoreName"]!.Trim();

        public static decimal VatPercent =>
            decimal.TryParse(_config["VatPercent"], out var v) ? v : 16m;

        public static string CurrencySymbol =>
            string.IsNullOrWhiteSpace(_config["CurrencySymbol"]) ? "KSh" : _config["CurrencySymbol"]!.Trim();
    }
}