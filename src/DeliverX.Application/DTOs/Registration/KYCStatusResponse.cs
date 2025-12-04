namespace DeliverX.Application.DTOs.Registration;

public class KYCStatusResponse
{
    public Guid UserId { get; set; }
    public string OverallStatus { get; set; } = string.Empty; // PENDING, PARTIALLY_VERIFIED, FULLY_VERIFIED, REJECTED
    public Dictionary<string, VerificationStatusDto> Verifications { get; set; } = new();
    public bool CanActivate { get; set; }
    public List<string> PendingVerifications { get; set; } = new();
    public string? NextStep { get; set; }
}

public class VerificationStatusDto
{
    public string Status { get; set; } = string.Empty;
    public DateTime? VerifiedAt { get; set; }
    public DateTime? InitiatedAt { get; set; }
    public string? ReferenceId { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
    public string? Message { get; set; }
}
