using System;
using System.Collections.Generic;

namespace DeliveryDost.Application.DTOs.Delivery;

public class DeliveryListResponse
{
    public List<DeliveryListItem> Deliveries { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class DeliveryListItem
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public string DropAddress { get; set; } = string.Empty;
    public decimal? EstimatedPrice { get; set; }
    public decimal? DistanceKm { get; set; }
    public string? AssignedDPName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Priority { get; set; } = string.Empty;
}

public class DeliveryListRequest
{
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
