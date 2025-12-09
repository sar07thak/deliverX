using System;

namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Platform fee configuration for different fee types
/// </summary>
public class PlatformFeeConfig
{
    public Guid Id { get; set; }
    public string FeeType { get; set; } = string.Empty; // PLATFORM_COMMISSION, GST, PEAK_SURCHARGE
    public string FeeCalculationType { get; set; } = "PERCENTAGE"; // PERCENTAGE, FLAT
    public decimal FeeValue { get; set; }
    public string? ApplicableRoles { get; set; } // JSON: ["DP", "DPCM", "DBC"]
    public string? Conditions { get; set; } // JSON: {"timeRange": "18:00-21:00"}
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
}
