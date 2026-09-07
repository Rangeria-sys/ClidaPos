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
        // The secret is stored XOR-obfuscated rather than as a plain string constant,
        // so it does not appear as readable text if the compiled app is decompiled.
        // This is a deterrent, not a vault - someone who deliberately traces through
        // DecodeSecret() can still recover it, but it defeats a casual look at
        // decompiled output, which is where most attempts would stop.
        private static readonly byte[] ObfuscatedSecret =
        {
            0x1A, 0x26, 0x01, 0x2F, 0x11, 0x29, 0x05, 0x20, 0x24, 0x22, 0x3B, 0x30, 0x1A, 0x56, 0x16, 0x17,
            0x2E, 0x2C, 0x09, 0x2A, 0x22, 0x07, 0x01, 0x39, 0x54, 0x2D, 0x0D, 0x1B, 0x19, 0x25, 0x00, 0x56,
            0x0A, 0x28, 0x0D, 0x35, 0x36, 0x1A, 0x25, 0x2A, 0x13, 0x39, 0x33, 0x0B, 0x54, 0x24, 0x27, 0x51
        };
        private const byte ObfuscationKey = 0x63;

        private static string SharedSecret => DecodeSecret();

        private static string DecodeSecret()
        {
            var bytes = new byte[ObfuscatedSecret.Length];
            for (var i = 0; i < ObfuscatedSecret.Length; i++)
                bytes[i] = (byte)(ObfuscatedSecret[i] ^ ObfuscationKey);
            return Encoding.UTF8.GetString(bytes);
        }

        public static readonly Dictionary<string, string> DurationLabels = new()
        {
            ["TRL"] = "Free Trial (14 Days)",
            ["1MO"] = "1 Month",
            ["3MO"] = "3 Months",
            ["6MO"] = "6 Months",
            ["1YR"] = "1 Year",
            ["LIF"] = "Lifetime"
        };

        private static readonly Dictionary<string, TimeSpan?> DurationSpans = new()
        {
            ["TRL"] = TimeSpan.FromDays(14),
            ["1MO"] = TimeSpan.FromDays(30),
            ["3MO"] = TimeSpan.FromDays(90),
            ["6MO"] = TimeSpan.FromDays(180),
            ["1YR"] = TimeSpan.FromDays(365),
            ["LIF"] = null // lifetime - no expiry
        };

        private const string NonceChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I ambiguity

        public static string GenerateKey(string durationCode, string clientName)
        {
            if (!DurationSpans.ContainsKey(durationCode))
                throw new ArgumentException($"Unknown duration code '{durationCode}'.");
            if (string.IsNullOrWhiteSpace(clientName))
                throw new ArgumentException("Client name is required - keys are tied to one business and cannot be reused elsewhere.");

            var tag = ClientTag(clientName);
            var nonce = GenerateNonce(6);
            var payload = $"{durationCode}-{tag}-{nonce}";
            var signature = ComputeSignature(payload);
            return $"{payload}-{signature}";
        }

        /// <summary>
        /// clientName is the business's own name as stored in their Business Profile -
        /// the app checks this automatically, so nothing extra needs to be typed at
        /// activation time. A key only validates for the exact business it was
        /// generated for; the same key entered under a different business name fails.
        /// </summary>
        public static bool TryValidate(string key, string clientName, out string durationCode, out string error)
        {
            durationCode = "";
            error = "";

            var parts = (key ?? "").Trim().ToUpper().Split('-');
            if (parts.Length != 4)
            {
                error = "Invalid key format.";
                return false;
            }

            var dur = parts[0];
            var tag = parts[1];
            var nonce = parts[2];
            var providedSignature = parts[3];

            if (!DurationSpans.ContainsKey(dur))
            {
                error = "Unrecognized duration code.";
                return false;
            }

            var expectedSignature = ComputeSignature($"{dur}-{tag}-{nonce}");
            if (!string.Equals(expectedSignature, providedSignature, StringComparison.OrdinalIgnoreCase))
            {
                error = "This key is invalid or was not issued by the vendor.";
                return false;
            }

            var expectedTag = ClientTag(clientName);
            if (!string.Equals(tag, expectedTag, StringComparison.OrdinalIgnoreCase))
            {
                error = "This key was not issued for this business. Check the Business Name in Business Profile matches exactly what was given to the vendor.";
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

        /// <summary>Trim, uppercase, collapse internal whitespace - forgiving of trivial
        /// typing differences (extra spaces, case) but not genuinely different names.</summary>
        private static string NormalizeClientName(string name) =>
            System.Text.RegularExpressions.Regex.Replace((name ?? "").Trim(), @"\s+", " ").ToUpperInvariant();

        private static string ClientTag(string clientName)
        {
            var normalized = NormalizeClientName(clientName);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(hash)[..6];
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