using System;
using System.Collections.Generic;

namespace DeliverX.Application.DTOs.Delivery;

public class DeliveryDetailsDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public AssignedDPInfo? AssignedDP { get; set; }
    public LocationInfo Pickup { get; set; } = new();
    public LocationInfo Drop { get; set; } = new();
    public PackageInfo Package { get; set; } = new();

    public DeliveryPricingInfo Pricing { get; set; } = new();
    public List<DeliveryTimelineItem> Timeline { get; set; } = new();
    public string TrackingUrl { get; set; } = string.Empty;
}

public class AssignedDPInfo
{
    public Guid DPId { get; set; }
    public string DPName { get; set; } = string.Empty;
    public string? DPPhone { get; set; }
    public string? DPPhoto { get; set; }
    public decimal Rating { get; set; }
}

public class DeliveryPricingInfo
{
    public decimal EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public Pricing.PricingBreakdown? Breakdown { get; set; }
}

public class DeliveryTimelineItem
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Guid? DPId { get; set; }
    public string? Description { get; set; }
}
