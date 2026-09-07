using System;
using System.IO;
using System.Security.Cryptography;

namespace Clidapos.Wpf.Services
{
    /// <summary>
    /// Encrypts sensitive credential values (M-Pesa API keys, SMTP passwords, etc.)
    /// before they're written to the database, and decrypts them when the app
    /// actually needs to use them - so a raw copy of the database (a stolen
    /// backup, a copied .mdf/.bak file) never exposes these values in plain text.
    ///
    /// IMPORTANT: change EncryptionKey to your own random 32-byte value before
    /// real deployment (see the comment below for how). Like the License key
    /// secret, the key is stored XOR-obfuscated so it doesn't appear as
    /// readable text in decompiled output - a deterrent against a casual look,
    /// not a vault. Someone who deliberately extracts this key from the
    /// compiled app could decrypt stored values, same limitation the License
    /// system already has and documents.
    ///
    /// Uses AES-256-CBC with a random IV generated fresh for every value
    /// encrypted (prepended to the ciphertext, since an IV doesn't need to be
    /// secret - only unique per encryption). This means encrypting the same
    /// plaintext twice produces different ciphertext each time, which is the
    /// correct, stronger choice here since none of these fields are ever used
    /// to look up or match rows - they're purely stored and retrieved.
    /// </summary>
    public static class SecretEncryptionService
    {
        // Generate your own with: python3 -c "import os; k=os.urandom(32); x=0x7E;
        // print(', '.join(f'0x{b^x:02X}' for b in k))" and replace both this array
        // and ObfuscationKey below.
        private static readonly byte[] ObfuscatedKey =
        {
            0x1B, 0x57, 0x71, 0xD0, 0xE9, 0x5F, 0x0F, 0x10, 0x41, 0xBF, 0xC8, 0xBC, 0x30, 0x8B, 0x9E, 0xD5,
            0x51, 0xC6, 0xE9, 0xD0, 0x09, 0x12, 0xCA, 0xE9, 0x13, 0x64, 0x4E, 0x48, 0x17, 0x82, 0x8E, 0xA0
        };
        private const byte ObfuscationKey = 0x7E;

        private static byte[] DecodeKey()
        {
            var bytes = new byte[ObfuscatedKey.Length];
            for (var i = 0; i < ObfuscatedKey.Length; i++)
                bytes[i] = (byte)(ObfuscatedKey[i] ^ ObfuscationKey);
            return bytes;
        }

        /// <summary>Encrypts plaintext for storage. Returns "" for null/empty input
        /// (so optional settings fields don't need special-casing by callers).</summary>
        public static string Encrypt(string? plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) return "";

            using var aes = Aes.Create();
            aes.Key = DecodeKey();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // IV + ciphertext together, so decrypt can pull the IV back out.
            var combined = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, combined, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(combined);
        }

        /// <summary>Decrypts a value previously produced by Encrypt. Returns "" for
        /// null/empty input, and for anything that fails to decrypt (e.g. a value
        /// that was stored before encryption was added, or genuinely corrupted data)
        /// rather than throwing - callers treat a blank credential as "not configured".</summary>
        public static string Decrypt(string? ciphertext)
        {
            if (string.IsNullOrEmpty(ciphertext)) return "";

            try
            {
                // ConsumerKey/ConsumerSecret/PassKey are stored in nchar columns,
                // which SQL Server pads with trailing spaces to the fixed width -
                // without trimming, that padding breaks Base64 decoding below and
                // silently produces "" on every read, even for a value that was
                // correctly encrypted.
                var combined = Convert.FromBase64String(ciphertext.Trim());

                using var aes = Aes.Create();
                aes.Key = DecodeKey();

                var iv = new byte[16];
                Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var cipherBytes = new byte[combined.Length - iv.Length];
                Buffer.BlockCopy(combined, iv.Length, cipherBytes, 0, cipherBytes.Length);

                var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return System.Text.Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>For display only - shows the last few characters, masking the rest,
        /// so a glance at the screen never exposes a full secret. Matches the same
        /// masking convention used for bank account numbers.</summary>
        /// <summary>For display only - masks everything except the last 4 characters,
        /// permanently (there's no reveal action - clearing and retyping the field is
        /// how a credential gets changed).</summary>
        public static string Mask(string? value)
        {
            var v = (value ?? "").Trim();
            if (v.Length == 0) return "";
            if (v.Length <= 4) return new string('•', v.Length);
            return new string('•', v.Length - 4) + v[^4..];
        }
    }
}
