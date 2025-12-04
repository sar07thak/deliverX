namespace DeliverX.Application.DTOs.Auth;

public class OtpVerifyRequest
{
    public string Phone { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    /// <summary>
    /// User role for new registrations: EC (End Consumer), BC (Business Consumer), DP (Delivery Partner)
    /// </summary>
    public string? Role { get; set; }
}
