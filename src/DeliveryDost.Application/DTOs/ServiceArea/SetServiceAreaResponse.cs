using System;

namespace DeliveryDost.Application.DTOs.ServiceArea;

/// <summary>
/// Response after creating/updating a service area
/// </summary>
public class SetServiceAreaResponse
{
    public Guid ServiceAreaId { get; set; }
    public ServiceAreaCoverage Coverage { get; set; } = new();
    public string EstimatedCoverage { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ServiceAreaCoverage
{
    public string Type { get; set; } = "CIRCLE";
    public decimal CenterLat { get; set; }
    public decimal CenterLng { get; set; }
    public decimal RadiusKm { get; set; }
    public string? AreaName { get; set; }
}
