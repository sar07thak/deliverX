using System;

namespace DeliveryDost.Application.DTOs.ServiceArea;

/// <summary>
/// Request to create or update a service area
/// </summary>
public class SetServiceAreaRequest
{
    /// <summary>
    /// Center latitude (-90 to 90)
    /// </summary>
    public decimal CenterLat { get; set; }

    /// <summary>
    /// Center longitude (-180 to 180)
    /// </summary>
    public decimal CenterLng { get; set; }

    /// <summary>
    /// Radius in kilometers (1-50 km)
    /// </summary>
    public decimal RadiusKm { get; set; }

    /// <summary>
    /// Optional name for the service area (e.g., "Jaipur Central")
    /// </summary>
    public string? AreaName { get; set; }

    /// <summary>
    /// Whether DP is willing to drop outside the area
    /// </summary>
    public bool AllowDropOutsideArea { get; set; } = false;
}
