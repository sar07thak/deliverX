using System;

namespace DeliverX.Application.DTOs.Delivery;

public class CreateDeliveryResponse
{
    public Guid DeliveryId { get; set; }
    public string Status { get; set; } = "CREATED";
    public decimal EstimatedPrice { get; set; }
    public decimal EstimatedDistance { get; set; }
    public string EstimatedTime { get; set; } = "15-20 mins";
    public string TrackingUrl { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
