namespace DeliveryDost.Domain.Entities;

public class KYCRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string VerificationType { get; set; } = string.Empty; // AADHAAR, PAN, BANK, POLICE, VEHICLE_LICENSE
    public string Status { get; set; } = "PENDING"; // PENDING, IN_PROGRESS, VERIFIED, REJECTED, EXPIRED
    public string? Method { get; set; } // MANUAL_UPLOAD, DIGILOCKER, API, PENNY_DROP
    public string? RequestData { get; set; } // JSON: submitted data
    public string? ResponseData { get; set; } // JSON: verification response
    public string? DocumentUrls { get; set; } // JSON array of uploaded documents
    public Guid? VerifiedBy { get; set; } // Admin who verified manually
    public string? RejectionReason { get; set; }
    public DateTime InitiatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public User? Verifier { get; set; }
}
