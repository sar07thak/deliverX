using System;

namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Stores pricing details for each delivery after creation
/// </summary>
public class DeliveryPricing
{
    public Guid Id { get; set; }
    public Guid? DeliveryId { get; set; } // Nullable for price preview without delivery
    public Guid DPId { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal WeightKg { get; set; }
    public decimal PerKmRate { get; set; }
    public decimal PerKgRate { get; set; }
    public decimal MinCharge { get; set; }
    public string? Surcharges { get; set; } // JSON array: [{"type": "PEAK_HOUR", "amount": 5.0}]
    public decimal Subtotal { get; set; }
    public decimal GSTPercentage { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DPEarning { get; set; }
    public decimal DPCMCommission { get; set; }
    public decimal PlatformFee { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime CalculatedAt { get; set; }

    // Navigation - links to User
    public User? User { get; set; }
}
