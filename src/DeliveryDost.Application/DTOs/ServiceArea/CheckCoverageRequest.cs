using System;

namespace DeliveryDost.Application.DTOs.ServiceArea;

/// <summary>
/// Request to check if a point is covered by a DP's service area
/// </summary>
public class CheckCoverageRequest
{
    /// <summary>
    /// The delivery partner's user ID
    /// </summary>
    public Guid DPId { get; set; }

    /// <summary>
    /// Point latitude to check
    /// </summary>
    public decimal PointLat { get; set; }

    /// <summary>
    /// Point longitude to check
    /// </summary>
    public decimal PointLng { get; set; }
}
