using System;

namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Delivery order entity
/// </summary>
public class Delivery
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public string RequesterType { get; set; } = "DBC"; // DBC, EC

    // Assigned DP
    public Guid? AssignedDPId { get; set; }
    public DateTime? AssignedAt { get; set; }

    // ====== PICKUP LOCATION ======
    // Address Name/Label (Group 3 Feature)
    public string? PickupAddressName { get; set; } // e.g., "Home", "Office"

    // Geolocation
    public decimal PickupLat { get; set; }
    public decimal PickupLng { get; set; }
    public string PickupAddress { get; set; } = string.Empty;

    // Contact Details
    public string? PickupContactName { get; set; }
    public string? PickupContactPhone { get; set; }
    public string? PickupAlternatePhone { get; set; } // Group 3 Feature
    public string? PickupContactEmail { get; set; } // Group 3 Feature
    public string? PickupWhatsAppNumber { get; set; } // Group 3 Feature
    public string? PickupInstructions { get; set; }

    // Saved Address Reference (Group 3 Feature)
    public Guid? PickupSavedAddressId { get; set; }

    // ====== DROP LOCATION ======
    // Address Name/Label (Group 3 Feature)
    public string? DropAddressName { get; set; } // e.g., "Home", "Office"

    // Geolocation
    public decimal DropLat { get; set; }
    public decimal DropLng { get; set; }
    public string DropAddress { get; set; } = string.Empty;

    // Contact Details
    public string? DropContactName { get; set; }
    public string? DropContactPhone { get; set; }
    public string? DropAlternatePhone { get; set; } // Group 3 Feature
    public string? DropContactEmail { get; set; } // Group 3 Feature
    public string? DropWhatsAppNumber { get; set; } // Group 3 Feature
    public string? DropInstructions { get; set; }

    // Saved Address Reference (Group 3 Feature)
    public Guid? DropSavedAddressId { get; set; }

    // ====== PACKAGE DETAILS ======
    public decimal WeightKg { get; set; }
    public string PackageType { get; set; } = "parcel"; // parcel, food, document, fragile
    public string? PackageDimensions { get; set; } // JSON: {length, width, height}
    public decimal? PackageValue { get; set; }
    public string? PackageDescription { get; set; }

    // Caution/Hazard Information (Group 3 Feature)
    public bool IsHazardous { get; set; } = false;
    public string? CautionType { get; set; } // FRAGILE, FLAMMABLE, PERISHABLE, LIQUID, GLASS, ELECTRONIC, CHEMICAL, NONE
    public string? CautionNotes { get; set; }
    public bool RequiresSpecialHandling { get; set; } = false;

    // ====== SCHEDULING ======
    public string Priority { get; set; } = "ASAP"; // ASAP, SCHEDULED
    public DateTime? ScheduledAt { get; set; }

    // Status: CREATED, MATCHING, ASSIGNED, ACCEPTED, PICKED_UP, IN_TRANSIT, DELIVERED, CANCELLED, UNASSIGNABLE
    public string Status { get; set; } = "CREATED";

    // ====== PRICING & DISTANCE ======
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }

    // Distance Calculation (Group 3 Feature - Enhanced)
    public decimal? DistanceKm { get; set; }
    public string? DistanceSource { get; set; } // GOOGLE_API, HAVERSINE, MANUAL
    public int? EstimatedDurationMinutes { get; set; }
    public string? RoutePolyline { get; set; } // Encoded polyline from Google Directions API

    // ====== SPECIAL INSTRUCTIONS ======
    public string? SpecialInstructions { get; set; }
    public Guid? PreferredDPId { get; set; }

    // ====== MATCHING ======
    public int MatchingAttempts { get; set; } = 0;

    // ====== TIMESTAMPS ======
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // ====== NAVIGATION ======
    public User? Requester { get; set; }
    public User? AssignedDP { get; set; }
    public SavedAddress? PickupSavedAddress { get; set; }
    public SavedAddress? DropSavedAddress { get; set; }
}
