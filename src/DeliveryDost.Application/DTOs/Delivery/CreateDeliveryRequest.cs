using System;

namespace DeliveryDost.Application.DTOs.Delivery;

public class CreateDeliveryRequest
{
    public Guid? RequesterId { get; set; }
    public string RequesterType { get; set; } = "DBC"; // DBC, EC

    public LocationInfo Pickup { get; set; } = new();
    public LocationInfo Drop { get; set; } = new();
    public PackageInfo Package { get; set; } = new();

    public string Priority { get; set; } = "ASAP"; // ASAP, SCHEDULED
    public DateTime? ScheduledAt { get; set; }
    public string? SpecialInstructions { get; set; }
    public Guid? PreferredDPId { get; set; }
}

/// <summary>
/// Enhanced location info with address name, alternate contacts, and save option
/// </summary>
public class LocationInfo
{
    // Address Name/Label (Group 3 Feature)
    public string? AddressName { get; set; } // e.g., "Home", "Office", "Warehouse A"

    // Geolocation
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public string Address { get; set; } = string.Empty;

    // Primary Contact
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }

    // Alternate Contacts (Group 3 Feature)
    public string? AlternatePhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? WhatsAppNumber { get; set; }

    // Instructions
    public string? Instructions { get; set; }

    // Save Address Option (Group 3 Feature)
    public bool SaveAddress { get; set; } = false;
    public Guid? SavedAddressId { get; set; } // Reference to existing saved address
}

/// <summary>
/// Enhanced package info with caution/hazard information
/// </summary>
public class PackageInfo
{
    public decimal WeightKg { get; set; } = 1;
    public string Type { get; set; } = "parcel"; // parcel, food, document, fragile
    public DimensionsInfo? Dimensions { get; set; }
    public decimal? Value { get; set; }
    public string? Description { get; set; }

    // Caution/Hazard Information (Group 3 Feature)
    public bool IsHazardous { get; set; } = false;
    public string? CautionType { get; set; } // FRAGILE, FLAMMABLE, PERISHABLE, LIQUID, GLASS, ELECTRONIC, CHEMICAL, NONE
    public string? CautionNotes { get; set; }
    public bool RequiresSpecialHandling { get; set; } = false;
}

public class DimensionsInfo
{
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
}
