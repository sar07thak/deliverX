namespace DeliverX.Application.DTOs.Auth;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? TotpCode { get; set; } // Optional, required if 2FA enabled
    public string? DeviceId { get; set; }
}
