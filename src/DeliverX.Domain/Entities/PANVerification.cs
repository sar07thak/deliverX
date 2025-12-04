namespace DeliverX.Domain.Entities;

public class PANVerification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PAN { get; set; } = string.Empty;
    public string NameAsPerPAN { get; set; } = string.Empty; // Encrypted
    public DateTime? DOB { get; set; }
    public string? PANStatus { get; set; } // ACTIVE, INACTIVE
    public int? NameMatchScore { get; set; } // % match with Aadhaar name
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
