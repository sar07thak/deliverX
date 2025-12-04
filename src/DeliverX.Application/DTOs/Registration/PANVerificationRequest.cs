namespace DeliverX.Application.DTOs.Registration;

public class PANVerificationRequest
{
    public Guid UserId { get; set; }
    public string PAN { get; set; } = string.Empty;
    public string? NameAsPerPan { get; set; } // Optional, for cross-check
}

public class PANDetailsDto
{
    public string PAN { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? DOB { get; set; }
    public string Status { get; set; } = string.Empty; // ACTIVE, INACTIVE
}
