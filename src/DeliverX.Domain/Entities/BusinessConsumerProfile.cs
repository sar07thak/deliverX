namespace DeliverX.Domain.Entities;

public class BusinessConsumerProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string? GSTIN { get; set; }
    public string PAN { get; set; } = string.Empty;
    public string? BusinessCategory { get; set; } // E-commerce, Food, Pharma, etc.
    public string? BusinessAddress { get; set; } // JSON
    public string? BankAccountEncrypted { get; set; } // JSON, encrypted
    public Guid? SubscriptionPlanId { get; set; }
    public bool IsActive { get; set; } = false;
    public DateTime? ActivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
