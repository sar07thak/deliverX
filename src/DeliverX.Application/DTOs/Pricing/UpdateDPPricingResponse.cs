using System;

namespace DeliverX.Application.DTOs.Pricing;

public class UpdateDPPricingResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
}
