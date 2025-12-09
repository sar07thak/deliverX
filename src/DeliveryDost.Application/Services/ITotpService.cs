namespace DeliveryDost.Application.Services;

public interface ITotpService
{
    /// <summary>
    /// Generate a new TOTP secret for a user
    /// </summary>
    string GenerateSecret();

    /// <summary>
    /// Generate QR code URI for authenticator apps (Google Authenticator, Authy, etc.)
    /// </summary>
    string GenerateQrCodeUri(string email, string secret, string issuer = "DeliveryDost");

    /// <summary>
    /// Verify a TOTP code
    /// </summary>
    bool VerifyCode(string secret, string code);
}
