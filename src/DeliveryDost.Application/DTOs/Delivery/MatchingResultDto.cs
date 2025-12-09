using System;
using System.Collections.Generic;

namespace DeliveryDost.Application.DTOs.Delivery;

public class MatchingResultDto
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid DeliveryId { get; set; }
    public List<MatchedDPInfo> MatchedDPs { get; set; } = new();
    public int TotalMatches { get; set; }
    public int NotificationsSent { get; set; }
    public string Status { get; set; } = "MATCHING_IN_PROGRESS";
}

public class MatchedDPInfo
{
    public Guid DPId { get; set; }
    public string DPName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal Rating { get; set; }
    public decimal EstimatedPrice { get; set; }
    public decimal DistanceFromPickupKm { get; set; }
    public decimal MatchScore { get; set; }
}
