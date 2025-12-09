using System;

namespace DeliveryDost.Application.DTOs.Pricing;

public class DPPricingConfigDto
{
    public Guid DPId { get; set; }
    public PricingConfigDetail PricingConfig { get; set; } = new();
    public CommissionInfo? DPCMCommission { get; set; }
    public CommissionInfo PlatformFee { get; set; } = new();
}

public class PricingConfigDetail
{
    public decimal PerKmRate { get; set; }
    public decimal PerKgRate { get; set; }
    public decimal MinCharge { get; set; }
    public decimal MaxDistanceKm { get; set; }
    public bool AcceptsPriorityDelivery { get; set; }
    public decimal PrioritySurcharge { get; set; }
    public decimal PeakHourSurcharge { get; set; }
    public string Currency { get; set; } = "INR";
}

public class CommissionInfo
{
    public string Type { get; set; } = "PERCENTAGE";
    public decimal Value { get; set; }
}
