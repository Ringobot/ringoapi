using System.Security.Cryptography;
using System.Text;

namespace Ringo.Api.Services
{
    public static class CryptoHelper
    {
        public static string Sha256(string input)
        {
            using (var algorithm = SHA256.Create())
            {
                return GetStringFromHash(algorithm.ComputeHash(Encoding.UTF8.GetBytes(input)));
            }
        }

        private static string GetStringFromHash(byte[] hash)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString();
        }
    }
}
