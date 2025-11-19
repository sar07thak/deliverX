namespace DeliverX.Application.DTOs.Auth;

public class OtpVerifyRequest
{
    public string Phone { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
}
