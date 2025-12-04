using System;

namespace DeliverX.Application.DTOs.Pricing;

public class CalculatePricingRequest
{
    public decimal PickupLat { get; set; }
    public decimal PickupLng { get; set; }
    public decimal DropLat { get; set; }
    public decimal DropLng { get; set; }
    public decimal WeightKg { get; set; } = 1;
    public string PackageType { get; set; } = "parcel";
    public string Priority { get; set; } = "SCHEDULED"; // ASAP or SCHEDULED
    public DateTime? ScheduledAt { get; set; }
    public Guid? PreferredDPId { get; set; }
}
