using System.Security.Cryptography;
using System.Text;

namespace DeliverX.Infrastructure.Utilities;

public static class HashHelper
{
    /// <summary>
    /// Generate SHA-256 hash of input string
    /// Used for Aadhaar number and bank account number hashing
    /// </summary>
    public static string SHA256(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLower();
    }

    /// <summary>
    /// Generate SHA-256 hash with salt
    /// </summary>
    public static string SHA256WithSalt(string input, string salt)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return SHA256(input + salt);
    }
}
