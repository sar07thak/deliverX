using System;

namespace DeliverX.Domain.Entities;

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

    // Pickup Location
    public decimal PickupLat { get; set; }
    public decimal PickupLng { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string? PickupContactName { get; set; }
    public string? PickupContactPhone { get; set; }
    public string? PickupInstructions { get; set; }

    // Drop Location
    public decimal DropLat { get; set; }
    public decimal DropLng { get; set; }
    public string DropAddress { get; set; } = string.Empty;
    public string? DropContactName { get; set; }
    public string? DropContactPhone { get; set; }
    public string? DropInstructions { get; set; }

    // Package Details
    public decimal WeightKg { get; set; }
    public string PackageType { get; set; } = "parcel"; // parcel, food, document, fragile
    public string? PackageDimensions { get; set; } // JSON: {length, width, height}
    public decimal? PackageValue { get; set; }
    public string? PackageDescription { get; set; }

    // Scheduling
    public string Priority { get; set; } = "ASAP"; // ASAP, SCHEDULED
    public DateTime? ScheduledAt { get; set; }

    // Status: CREATED, MATCHING, ASSIGNED, ACCEPTED, PICKED_UP, IN_TRANSIT, DELIVERED, CANCELLED, UNASSIGNABLE
    public string Status { get; set; } = "CREATED";

    // Pricing
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }

    // Special
    public string? SpecialInstructions { get; set; }
    public Guid? PreferredDPId { get; set; }

    // Metadata
    public decimal? DistanceKm { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public int MatchingAttempts { get; set; } = 0;

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Navigation
    public User? Requester { get; set; }
    public User? AssignedDP { get; set; }
}
