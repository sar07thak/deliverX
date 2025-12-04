using System;

namespace DeliverX.Domain.Entities;

/// <summary>
/// DPCM (Delivery Partner Cluster Manager) commission configuration
/// </summary>
public class DPCMCommissionConfig
{
    public Guid Id { get; set; }
    public Guid DPCMId { get; set; }
    public string CommissionType { get; set; } = "PERCENTAGE"; // PERCENTAGE, FLAT_PER_DELIVERY
    public decimal CommissionValue { get; set; }
    public decimal MinCommissionAmount { get; set; } = 0;
    public decimal? MaxCommissionAmount { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public DPCManager? DPCM { get; set; }
}
