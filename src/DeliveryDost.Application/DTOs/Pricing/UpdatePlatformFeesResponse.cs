using System;

namespace DeliveryDost.Application.DTOs.Pricing;

public class UpdatePlatformFeesResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
}
