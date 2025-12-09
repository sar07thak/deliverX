using System;

namespace DeliveryDost.Application.DTOs.Delivery;

public class UpdateDPAvailabilityRequest
{
    // OFFLINE, AVAILABLE, BUSY, BREAK
    public string Status { get; set; } = "AVAILABLE";
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
}

public class DPAvailabilityDto
{
    public Guid DPId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? CurrentDeliveryId { get; set; }
    public decimal? LastLocationLat { get; set; }
    public decimal? LastLocationLng { get; set; }
    public DateTime? LastLocationUpdatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
