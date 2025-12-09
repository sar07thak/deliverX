namespace DeliveryDost.Application.DTOs.Registration;

public class PoliceVerificationRequest
{
    public Guid UserId { get; set; }
    public bool Consent { get; set; }
    public AddressDto AddressForVerification { get; set; } = new();
}

public class PoliceVerificationResponse
{
    public Guid KycId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int EstimatedCompletionDays { get; set; }
}
