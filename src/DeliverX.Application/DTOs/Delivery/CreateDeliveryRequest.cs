using System;

namespace DeliverX.Application.DTOs.Delivery;

public class CreateDeliveryRequest
{
    public Guid? RequesterId { get; set; }
    public string RequesterType { get; set; } = "DBC"; // DBC, EC

    public LocationInfo Pickup { get; set; } = new();
    public LocationInfo Drop { get; set; } = new();
    public PackageInfo Package { get; set; } = new();

    public string Priority { get; set; } = "ASAP"; // ASAP, SCHEDULED
    public DateTime? ScheduledAt { get; set; }
    public string? SpecialInstructions { get; set; }
    public Guid? PreferredDPId { get; set; }
}

public class LocationInfo
{
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? Instructions { get; set; }
}

public class PackageInfo
{
    public decimal WeightKg { get; set; } = 1;
    public string Type { get; set; } = "parcel"; // parcel, food, document, fragile
    public DimensionsInfo? Dimensions { get; set; }
    public decimal? Value { get; set; }
    public string? Description { get; set; }
}

public class DimensionsInfo
{
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
}
