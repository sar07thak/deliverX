namespace DeliveryDost.Domain.Entities;

public class DPCManager
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string PAN { get; set; } = string.Empty;
    public string? RegistrationCertificateUrl { get; set; }
    public string? ServiceRegions { get; set; } // JSON array: ["Jaipur", "Delhi"]
    public string? CommissionType { get; set; } // PERCENTAGE, FLAT, HYBRID (Amount or % whichever is higher)
    public decimal? CommissionValue { get; set; }
    public decimal? MinCommissionAmount { get; set; } // For HYBRID: minimum commission amount
    public string? BankAccountEncrypted { get; set; } // JSON, encrypted

    // Security Deposit (Manual registration)
    public decimal SecurityDeposit { get; set; } = 0;
    public string? SecurityDepositStatus { get; set; } // PENDING, RECEIVED, REFUNDED
    public DateTime? SecurityDepositReceivedAt { get; set; }
    public string? SecurityDepositTransactionRef { get; set; }

    // Agreement Document
    public string? AgreementDocumentUrl { get; set; }
    public DateTime? AgreementSignedAt { get; set; }
    public string? AgreementVersion { get; set; }

    public bool IsActive { get; set; } = false;
    public DateTime? ActivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<DeliveryPartnerProfile> DeliveryPartners { get; set; } = new List<DeliveryPartnerProfile>();
    public ICollection<PincodeDPCMMapping> PincodeMappings { get; set; } = new List<PincodeDPCMMapping>();
}

/// <summary>
/// Pincode-DPCM Mapping: One Pincode = One DPCM
/// </summary>
public class PincodeDPCMMapping
{
    public Guid Id { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public Guid DPCMId { get; set; }
    public string? StateName { get; set; }
    public string? DistrictName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public Guid? AssignedByUserId { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public string? DeactivationReason { get; set; }

    // Navigation
    public DPCManager DPCM { get; set; } = null!;
    public User? AssignedByUser { get; set; }
}
