namespace DeliveryDost.Domain.Entities;

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

    // Business Constitution (new)
    public string? BusinessConstitution { get; set; } // PROPRIETORSHIP, PARTNERSHIP, LLP, PRIVATE_LIMITED, PUBLIC_LIMITED, etc.

    // GST Registration Type (new)
    public string? GSTRegistrationType { get; set; } // REGULAR, COMPOSITION, UNREGISTERED

    // Bank Details (encrypted JSON)
    public string? BankAccountEncrypted { get; set; } // JSON: AccountNumber, IFSCCode, AccountHolderName, BankName

    // Subscription (mandatory for BC)
    public Guid? SubscriptionPlanId { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }

    // Pickup Locations (stored separately or as JSON)
    public string? PickupLocationsJson { get; set; } // JSON array of pickup addresses

    public bool IsActive { get; set; } = false;
    public DateTime? ActivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public SubscriptionPlan? SubscriptionPlan { get; set; }
}

/// <summary>
/// Pickup location for Business Consumer
/// </summary>
public class BCPickupLocation
{
    public Guid Id { get; set; }
    public Guid BusinessConsumerProfileId { get; set; }
    public string LocationName { get; set; } = string.Empty; // "Main Warehouse", "Branch Office", etc.
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public BusinessConsumerProfile BusinessConsumerProfile { get; set; } = null!;
}
