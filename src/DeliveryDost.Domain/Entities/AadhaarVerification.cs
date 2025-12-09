namespace DeliveryDost.Domain.Entities;

public class AadhaarVerification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AadhaarHash { get; set; } = string.Empty; // SHA-256 hash of Aadhaar number
    public string? AadhaarReferenceId { get; set; } // UIDAI reference ID
    public string NameAsPerAadhaar { get; set; } = string.Empty; // Encrypted
    public DateTime DOB { get; set; }
    public string? Gender { get; set; }
    public string? AddressEncrypted { get; set; } // Encrypted full address
    public string? VerificationMethod { get; set; } // DIGILOCKER, MANUAL, OFFLINE_EKYC
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
