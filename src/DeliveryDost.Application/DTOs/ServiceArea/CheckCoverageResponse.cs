using System;

namespace DeliveryDost.Application.DTOs.ServiceArea;

/// <summary>
/// Response indicating whether a point is covered by a service area
/// </summary>
public class CheckCoverageResponse
{
    public bool IsCovered { get; set; }
    public Guid? ServiceAreaId { get; set; }
    public decimal? DistanceFromCenterKm { get; set; }
    public string? AreaName { get; set; }
}
