using System;
using System.Collections.Generic;

namespace DeliverX.Application.DTOs.ServiceArea;

/// <summary>
/// Response containing eligible delivery partners for a delivery
/// </summary>
public class FindEligibleDPsResponse
{
    public List<MatchedDP> MatchedDPs { get; set; } = new();
    public int TotalMatches { get; set; }
    public long QueryTimeMs { get; set; }
}

public class MatchedDP
{
    public Guid DPId { get; set; }
    public string DPName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid ServiceAreaId { get; set; }
    public decimal DistanceFromPickupKm { get; set; }
    public decimal DistanceFromDropKm { get; set; }
    public string CoverageType { get; set; } = "BOTH_ENDS"; // BOTH_ENDS or PICKUP_ONLY
    public DPPricingInfo? Pricing { get; set; }
    public decimal EstimatedPrice { get; set; }
    public decimal Rating { get; set; } = 5.0m;
}

public class DPPricingInfo
{
    public decimal PerKmRate { get; set; }
    public decimal PerKgRate { get; set; }
    public decimal MinCharge { get; set; }
}
