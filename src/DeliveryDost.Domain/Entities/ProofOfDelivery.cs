using System;

namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Proof of Delivery (POD) entity
/// Captures evidence and confirmation of successful delivery
/// </summary>
public class ProofOfDelivery
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }

    // Recipient Information
    public string? RecipientName { get; set; }
    public string? RecipientRelation { get; set; } // Self, Family, Security, Neighbor

    // OTP Verification
    public string? RecipientOTP { get; set; } // 4-digit OTP sent to recipient
    public bool OTPVerified { get; set; }
    public DateTime? OTPSentAt { get; set; }
    public DateTime? OTPVerifiedAt { get; set; }

    // Photo Evidence
    public string? PODPhotoUrl { get; set; }
    public string? PackagePhotoUrl { get; set; }

    // Signature (optional)
    public string? SignatureUrl { get; set; }

    // GPS Coordinates at Delivery
    public decimal? DeliveredLat { get; set; }
    public decimal? DeliveredLng { get; set; }
    public decimal? DistanceFromDropLocation { get; set; } // meters from original drop

    // Delivery Timestamps
    public DateTime? PickedUpAt { get; set; }
    public DateTime? InTransitAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Notes and Metadata
    public string? Notes { get; set; }
    public string? DeliveryCondition { get; set; } // Good, Damaged, Partial

    // Verification
    public Guid? VerifiedBy { get; set; } // EC userId if they confirmed delivery
    public DateTime? VerifiedAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Delivery? Delivery { get; set; }
    public User? Verifier { get; set; }
}
