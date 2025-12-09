using System;

namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Delivery state history / audit trail
/// </summary>
public class DeliveryEvent
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }

    // Event type: CREATED, MATCHED, ASSIGNED, ACCEPTED, REJECTED, PICKED_UP, IN_TRANSIT, DELIVERED, CANCELLED
    public string EventType { get; set; } = string.Empty;
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }

    public Guid? ActorId { get; set; }
    public string? ActorType { get; set; } // SYSTEM, DP, DBC, EC, ADMIN
    public string? Metadata { get; set; } // JSON: event-specific data

    public DateTime Timestamp { get; set; }

    // Navigation
    public Delivery? Delivery { get; set; }
    public User? Actor { get; set; }
}
