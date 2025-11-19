using System.Text;
using OtpNet;
using DeliverX.Application.Services;

namespace DeliverX.Infrastructure.Services;

public class TotpService : ITotpService
{
    public string GenerateSecret()
    {
        // Generate a 160-bit (20 byte) secret key
        var key = KeyGeneration.GenerateRandomKey(20);
        return Convert.ToBase64String(key);
    }

    public string GenerateQrCodeUri(string email, string secret, string issuer = "DeliverX")
    {
        var secretBytes = Convert.FromBase64String(secret);
        var base32Secret = Base32Encoding.ToString(secretBytes);

        // Format: otpauth://totp/Issuer:email?secret=SECRET&issuer=Issuer
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedIssuer = Uri.EscapeDataString(issuer);

        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={base32Secret}&issuer={encodedIssuer}";
    }

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code))
            return false;

        try
        {
            var secretBytes = Convert.FromBase64String(secret);
            var totp = new Totp(secretBytes);

            // Allow 2 time steps before and after (total window of 5 steps = 2.5 minutes)
            return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));
        }
        catch
        {
            return false;
        }
    }
}
