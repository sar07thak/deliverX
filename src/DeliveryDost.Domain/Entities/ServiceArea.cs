using System;

namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Represents a delivery partner's service coverage area.
/// MVP uses circle-based areas (center point + radius).
/// </summary>
public class ServiceArea
{
    public Guid Id { get; set; }

    /// <summary>
    /// The user (DP or DPCM) who owns this service area
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Role of the user: DP or DPCM
    /// </summary>
    public string UserRole { get; set; } = "DP";

    /// <summary>
    /// Type of service area: CIRCLE (MVP) or POLYGON (future)
    /// </summary>
    public string AreaType { get; set; } = "CIRCLE";

    /// <summary>
    /// Center latitude for circle-based area (-90 to 90)
    /// </summary>
    public decimal CenterLat { get; set; }

    /// <summary>
    /// Center longitude for circle-based area (-180 to 180)
    /// </summary>
    public decimal CenterLng { get; set; }

    /// <summary>
    /// Radius in kilometers (1-50 km for MVP)
    /// </summary>
    public decimal RadiusKm { get; set; }

    /// <summary>
    /// Optional name/label for the service area
    /// </summary>
    public string? AreaName { get; set; }

    /// <summary>
    /// Whether this service area is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether DP is willing to deliver outside area (drop only)
    /// </summary>
    public bool AllowDropOutsideArea { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? User { get; set; }
}
