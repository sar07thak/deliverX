namespace DeliverX.Application.DTOs.Registration;

public class DPRegistrationRequest
{
    public string Phone { get; set; } = string.Empty;
    public string? ReferralCode { get; set; } // Optional: if onboarded by DPCM
}

public class DPRegistrationResponse
{
    public Guid UserId { get; set; }
    public Guid? RegistrationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? NextStep { get; set; }
}
