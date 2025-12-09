namespace DeliveryDost.Application.DTOs.Auth;

public class OtpSendRequest
{
    public string Phone { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "+91";
    /// <summary>
    /// User role for new registrations: EC (End Consumer), BC (Business Consumer), DP (Delivery Partner)
    /// </summary>
    public string? Role { get; set; }
}
