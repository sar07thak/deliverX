using System;

namespace DeliveryDost.Application.DTOs.Delivery;

public class AcceptDeliveryResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid DeliveryId { get; set; }
    public string Status { get; set; } = string.Empty;
    public LocationInfo? Pickup { get; set; }
    public string? NavigationUrl { get; set; }
    public decimal EstimatedEarning { get; set; }
}
