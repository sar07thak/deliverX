namespace DeliverX.Application.DTOs.Auth;

public class OtpSendRequest
{
    public string Phone { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "+91";
}
