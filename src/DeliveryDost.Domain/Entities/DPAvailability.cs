using System;

namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Tracks DP availability status and current location
/// </summary>
public class DPAvailability
{
    public Guid Id { get; set; }
    public Guid DPId { get; set; }

    // Status: OFFLINE, AVAILABLE, BUSY (on delivery), BREAK
    public string Status { get; set; } = "OFFLINE";

    public Guid? CurrentDeliveryId { get; set; }

    // Last known location
    public decimal? LastLocationLat { get; set; }
    public decimal? LastLocationLng { get; set; }
    public DateTime? LastLocationUpdatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User? DP { get; set; }
    public Delivery? CurrentDelivery { get; set; }
}
