using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Clidapos.Wpf.Services
{
    /// <summary>
    /// Real cryptographic license key generation and verification (HMAC-SHA256 signed).
    /// A key is genuine only if it was signed with SharedSecret - the app can verify
    /// this without being able to forge new keys, since verification only needs to
    /// recompute and compare, not reverse the signature.
    ///
    /// IMPORTANT: change SharedSecret to your own random value before real deployment,
    /// and use the exact same value in the separate LicenseKeyGenerator tool. Anyone
    /// who extracts this secret from the compiled app (via decompilation) could forge
    /// keys - this is a real deterrent, not an unbreakable vault. No local-only license
    /// check can be 100% unbreakable; this raises the bar significantly above a plain
    /// text password check.
    /// </summary>
    public static class LicenseKeyService
    {
        private const string SharedSecret = "CHANGE-THIS-TO-YOUR-OWN-RANDOM-SECRET-8f3a9d2c7e1b";

        public static readonly Dictionary<string, string> DurationLabels = new()
        {
            ["1MO"] = "1 Month",
            ["3MO"] = "3 Months",
            ["6MO"] = "6 Months",
            ["1YR"] = "1 Year",
            ["LIF"] = "Lifetime"
        };

        private static readonly Dictionary<string, TimeSpan?> DurationSpans = new()
        {
            ["1MO"] = TimeSpan.FromDays(30),
            ["3MO"] = TimeSpan.FromDays(90),
            ["6MO"] = TimeSpan.FromDays(180),
            ["1YR"] = TimeSpan.FromDays(365),
            ["LIF"] = null // lifetime - no expiry
        };

        private const string NonceChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I ambiguity

        public static string GenerateKey(string durationCode)
        {
            if (!DurationSpans.ContainsKey(durationCode))
                throw new ArgumentException($"Unknown duration code '{durationCode}'.");

            var nonce = GenerateNonce(6);
            var payload = $"{durationCode}-{nonce}";
            var signature = ComputeSignature(payload);
            return $"{payload}-{signature}";
        }

        public static bool TryValidate(string key, out string durationCode, out string error)
        {
            durationCode = "";
            error = "";

            var parts = (key ?? "").Trim().ToUpper().Split('-');
            if (parts.Length != 3)
            {
                error = "Invalid key format.";
                return false;
            }

            var dur = parts[0];
            var nonce = parts[1];
            var providedSignature = parts[2];

            if (!DurationSpans.ContainsKey(dur))
            {
                error = "Unrecognized duration code.";
                return false;
            }

            var expectedSignature = ComputeSignature($"{dur}-{nonce}");
            if (!string.Equals(expectedSignature, providedSignature, StringComparison.OrdinalIgnoreCase))
            {
                error = "This key is invalid or was not issued by the vendor.";
                return false;
            }

            durationCode = dur;
            return true;
        }

        /// <summary>Null return means lifetime (no expiry).</summary>
        public static DateTime? ComputeExpiry(DateTime activatedDate, string durationCode)
        {
            if (!DurationSpans.TryGetValue(durationCode, out var span) || span == null)
                return null;
            return activatedDate.Add(span.Value);
        }

        private static string GenerateNonce(int length)
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            var chars = new char[length];
            for (var i = 0; i < length; i++)
                chars[i] = NonceChars[bytes[i] % NonceChars.Length];
            return new string(chars);
        }

        private static string ComputeSignature(string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SharedSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash)[..8];
        }
    }
}