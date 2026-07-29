using System;
using System.Security.Cryptography;

namespace Smash_IT.Security
{
    public static class PasswordHasher
    {
        private const int Iterations = 100000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("A password is required.", "password");

            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                byte[] hash = derive.GetBytes(HashSize);
                return "PBKDF2$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
            }
        }

        public static bool Verify(string password, string storedValue)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedValue)) return false;
            string[] parts = storedValue.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2") return false;

            int iterations;
            byte[] salt, expected;
            if (!int.TryParse(parts[1], out iterations) || iterations < 10000) return false;
            try { salt = Convert.FromBase64String(parts[2]); expected = Convert.FromBase64String(parts[3]); }
            catch (FormatException) { return false; }

            using (var derive = new Rfc2898DeriveBytes(password, salt, iterations))
                return FixedTimeEquals(expected, derive.GetBytes(expected.Length));
        }

        public static bool IsLegacyPlainText(string storedValue) { return !string.IsNullOrEmpty(storedValue) && !storedValue.StartsWith("PBKDF2$", StringComparison.Ordinal); }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int difference = 0;
            for (int i = 0; i < a.Length; i++) difference |= a[i] ^ b[i];
            return difference == 0;
        }
    }
}
