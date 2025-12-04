namespace DeliverX.Domain.Entities;

public class DPCManager
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string PAN { get; set; } = string.Empty;
    public string? RegistrationCertificateUrl { get; set; }
    public string? ServiceRegions { get; set; } // JSON array: ["Jaipur", "Delhi"]
    public string? CommissionType { get; set; } // PERCENTAGE, FLAT
    public decimal? CommissionValue { get; set; }
    public string? BankAccountEncrypted { get; set; } // JSON, encrypted
    public bool IsActive { get; set; } = false;
    public DateTime? ActivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<DeliveryPartnerProfile> DeliveryPartners { get; set; } = new List<DeliveryPartnerProfile>();
}
