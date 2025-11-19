namespace DeliverX.Application.DTOs.Auth;

public class OtpSendResponse
{
    public string Status { get; set; } = "OTP_SENT";
    public int ExpiresIn { get; set; } = 300; // 5 minutes in seconds
    public string Message { get; set; } = string.Empty;
}
