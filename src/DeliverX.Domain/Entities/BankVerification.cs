namespace DeliverX.Domain.Entities;

public class BankVerification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AccountNumberEncrypted { get; set; } = string.Empty; // Encrypted
    public string AccountNumberHash { get; set; } = string.Empty; // For duplicate detection
    public string IFSCCode { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty; // Encrypted
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? VerificationMethod { get; set; } // PENNY_DROP, NPCI
    public string? TransactionId { get; set; } // Penny drop transaction ID
    public int? NameMatchScore { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
