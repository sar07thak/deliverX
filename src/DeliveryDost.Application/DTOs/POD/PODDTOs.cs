using System;

namespace DeliveryDost.Application.DTOs.POD;

/// <summary>
/// Request to mark delivery as picked up
/// </summary>
public class PickupRequest
{
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
    public string? PackagePhotoUrl { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request to mark delivery as in transit
/// </summary>
public class TransitRequest
{
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request to complete delivery with POD
/// </summary>
public class DeliverRequest
{
    public string RecipientName { get; set; } = string.Empty;
    public string? RecipientRelation { get; set; } // Self, Family, Security, Neighbor
    public string? OTP { get; set; } // 4-digit OTP for verification
    public string? PODPhotoUrl { get; set; }
    public string? SignatureUrl { get; set; }
    public decimal DeliveredLat { get; set; }
    public decimal DeliveredLng { get; set; }
    public string? DeliveryCondition { get; set; } // Good, Damaged, Partial
    public string? Notes { get; set; }
}

/// <summary>
/// Response for state transition operations
/// </summary>
public class StateTransitionResponse
{
    public bool IsSuccess { get; set; }
    public Guid DeliveryId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PreviousStatus { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public string? NextAction { get; set; } // Guidance for DP
}

/// <summary>
/// POD details response
/// </summary>
public class PODDetailsDto
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }

    // Recipient
    public string? RecipientName { get; set; }
    public string? RecipientRelation { get; set; }

    // OTP
    public bool OTPVerified { get; set; }
    public DateTime? OTPVerifiedAt { get; set; }

    // Evidence
    public string? PODPhotoUrl { get; set; }
    public string? PackagePhotoUrl { get; set; }
    public string? SignatureUrl { get; set; }

    // Location
    public decimal? DeliveredLat { get; set; }
    public decimal? DeliveredLng { get; set; }
    public decimal? DistanceFromDropLocation { get; set; }

    // Timestamps
    public DateTime? PickedUpAt { get; set; }
    public DateTime? InTransitAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Metadata
    public string? Notes { get; set; }
    public string? DeliveryCondition { get; set; }

    // Verification
    public bool IsVerified { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

/// <summary>
/// Request to send OTP to recipient
/// </summary>
public class SendDeliveryOTPRequest
{
    public Guid DeliveryId { get; set; }
}

/// <summary>
/// Response for OTP send
/// </summary>
public class SendDeliveryOTPResponse
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Request to verify delivery OTP
/// </summary>
public class VerifyDeliveryOTPRequest
{
    public string OTP { get; set; } = string.Empty;
}

/// <summary>
/// Response for OTP verification
/// </summary>
public class VerifyDeliveryOTPResponse
{
    public bool IsSuccess { get; set; }
    public bool IsVerified { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Request to close a delivered order
/// </summary>
public class CloseDeliveryRequest
{
    public string? Reason { get; set; } // Auto-close, Manual, No complaints
}

/// <summary>
/// Delivery state info for tracking
/// </summary>
public class DeliveryStateInfo
{
    public Guid DeliveryId { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public string[] AllowedTransitions { get; set; } = Array.Empty<string>();
    public bool CanMatch { get; set; }      // EC/BC can start matching
    public bool CanAccept { get; set; }     // DP can accept delivery
    public bool CanPickup { get; set; }
    public bool CanTransit { get; set; }
    public bool CanDeliver { get; set; }
    public bool CanCancel { get; set; }
    public bool CanClose { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}
