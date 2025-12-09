namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Saved addresses for End Consumers and Business Consumers
/// Allows users to permanently save pickup/drop locations for reuse
/// </summary>
public class SavedAddress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Address Name/Label (e.g., "Home", "Office", "Warehouse A")
    public string AddressName { get; set; } = string.Empty;

    // Full Address
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;

    // Geolocation
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    // Full formatted address (for display)
    public string FullAddress { get; set; } = string.Empty;

    // Contact Details
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? WhatsAppNumber { get; set; }

    // Address Type
    public string AddressType { get; set; } = "OTHER"; // HOME, OFFICE, WAREHOUSE, OTHER

    // Usage flags
    public bool IsDefault { get; set; } = false;
    public bool IsPickupAddress { get; set; } = true; // Can be used as pickup
    public bool IsDropAddress { get; set; } = true; // Can be used as drop

    // Special Instructions
    public string? DefaultInstructions { get; set; }
    public string? Landmark { get; set; }

    // Status
    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
