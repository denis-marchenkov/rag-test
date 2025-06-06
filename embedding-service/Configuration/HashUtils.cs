using System.Security.Cryptography;
using System.Text;

namespace embedding_service.Configuration
{
    public static class HashUtils
    {
        public static string HashToHex(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        public static string HashToShortId(string input, int length = 12)
        {
            string hash = HashToHex(input);
            return hash.Substring(0, Math.Min(length, hash.Length));
        }
    }
}
