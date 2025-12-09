namespace DeliveryDost.Domain.Entities;

public class PoliceVerification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? VerificationAgency { get; set; } // Local police / Third-party agency name
    public string? AddressForVerification { get; set; } // JSON
    public string? RequestDocumentUrl { get; set; }
    public string? ClearanceDocumentUrl { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, IN_PROGRESS, CLEARED, REJECTED
    public DateTime InitiatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
