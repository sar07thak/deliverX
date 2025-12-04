using System;
using System.Collections.Generic;

namespace DeliverX.Application.DTOs.Pricing;

public class CalculatePricingResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public PricingBreakdown? PricingBreakdown { get; set; }
    public List<DPPricingEstimate> AvailableDPs { get; set; } = new();
    public decimal CheapestPrice { get; set; }
    public string EstimatedDeliveryTime { get; set; } = "15-20 mins";
}

public class PricingBreakdown
{
    public decimal DistanceKm { get; set; }
    public decimal DistanceCost { get; set; }
    public decimal WeightCost { get; set; }
    public decimal MinCharge { get; set; }
    public List<SurchargeItem> Surcharges { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal GSTPercentage { get; set; }
    public decimal GST { get; set; }
    public decimal Total { get; set; }
}

public class SurchargeItem
{
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class DPPricingEstimate
{
    public Guid DPId { get; set; }
    public string DPName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal Rating { get; set; } = 5.0m;
    public decimal EstimatedPrice { get; set; }
    public string ETA { get; set; } = "15-20 mins";
    public PricingBreakdown? Breakdown { get; set; }
}
