using System;

namespace DeliverX.Domain.Entities;

/// <summary>
/// Delivery Partner pricing configuration
/// </summary>
public class DPPricingConfig
{
    public Guid Id { get; set; }
    public Guid DPId { get; set; }
    public decimal PerKmRate { get; set; }
    public decimal PerKgRate { get; set; }
    public decimal MinCharge { get; set; }
    public decimal MaxDistanceKm { get; set; } = 20;
    public bool AcceptsPriorityDelivery { get; set; } = true;
    public decimal PrioritySurcharge { get; set; } = 0;
    public decimal PeakHourSurcharge { get; set; } = 0;
    public string Currency { get; set; } = "INR";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation - links to User, not DeliveryPartnerProfile (user may not have profile yet)
    public User? User { get; set; }
}
