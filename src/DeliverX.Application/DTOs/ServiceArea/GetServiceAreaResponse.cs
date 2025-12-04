using System;
using System.Collections.Generic;

namespace DeliverX.Application.DTOs.ServiceArea;

/// <summary>
/// Response containing service area details for a user
/// </summary>
public class GetServiceAreaResponse
{
    public List<ServiceAreaDetail> ServiceAreas { get; set; } = new();
}

public class ServiceAreaDetail
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "CIRCLE";
    public decimal CenterLat { get; set; }
    public decimal CenterLng { get; set; }
    public decimal RadiusKm { get; set; }
    public string? AreaName { get; set; }
    public bool IsActive { get; set; }
    public bool AllowDropOutsideArea { get; set; }
    public string EstimatedCoverage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
