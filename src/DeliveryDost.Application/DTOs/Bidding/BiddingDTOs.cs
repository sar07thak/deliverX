using System;
using System.ComponentModel.DataAnnotations;

namespace DeliveryDost.Application.DTOs.Bidding;

/// <summary>
/// Request to place a bid on a delivery
/// </summary>
public class PlaceBidRequest
{
    [Required]
    public Guid DeliveryId { get; set; }

    [Required]
    [Range(1, 100000, ErrorMessage = "Bid amount must be between 1 and 100000")]
    public decimal BidAmount { get; set; }

    [StringLength(500)]
    public string? BidNotes { get; set; }

    // DP's current location
    public decimal? CurrentLat { get; set; }
    public decimal? CurrentLng { get; set; }

    // Estimated times (optional - DP can provide estimates)
    public int? EstimatedPickupMinutes { get; set; }
    public int? EstimatedDeliveryMinutes { get; set; }
}

/// <summary>
/// Response after placing a bid
/// </summary>
public class PlaceBidResponse
{
    public Guid BidId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool ExceedsMaxRate { get; set; }
    public string? Message { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Request to accept a bid (by requester)
/// </summary>
public class AcceptBidRequest
{
    [Required]
    public Guid BidId { get; set; }

    [Required]
    public Guid DeliveryId { get; set; }
}

/// <summary>
/// Request to reject a bid
/// </summary>
public class RejectBidRequest
{
    [Required]
    public Guid BidId { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }
}

/// <summary>
/// Bid details DTO
/// </summary>
public class BidDto
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public Guid DPId { get; set; }
    public string DPName { get; set; } = string.Empty;
    public string? DPPhone { get; set; }
    public string? DPPhotoUrl { get; set; }
    public decimal DPRating { get; set; }
    public int DPCompletedDeliveries { get; set; }

    public decimal BidAmount { get; set; }
    public string? BidNotes { get; set; }
    public decimal? DistanceToPickupKm { get; set; }
    public int? EstimatedPickupMinutes { get; set; }
    public int? EstimatedDeliveryMinutes { get; set; }

    public string Status { get; set; } = string.Empty;
    public bool ExceedsMaxRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Calculated
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public int MinutesRemaining => Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalMinutes);
}

/// <summary>
/// List of bids for a delivery
/// </summary>
public class DeliveryBidsResponse
{
    public Guid DeliveryId { get; set; }
    public List<BidDto> Bids { get; set; } = new();
    public int TotalBids { get; set; }
    public decimal? LowestBid { get; set; }
    public decimal? HighestBid { get; set; }
    public decimal? AverageBid { get; set; }
    public decimal EstimatedPrice { get; set; }
    public bool BiddingOpen { get; set; }
    public DateTime? BiddingClosesAt { get; set; }
}

/// <summary>
/// Available delivery for bidding (DP view)
/// </summary>
public class AvailableDeliveryForBidDto
{
    public Guid Id { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string DropAddress { get; set; } = string.Empty;
    public decimal PickupLat { get; set; }
    public decimal PickupLng { get; set; }
    public decimal DropLat { get; set; }
    public decimal DropLng { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal WeightKg { get; set; }
    public string PackageType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public bool IsHazardous { get; set; }
    public string? CautionType { get; set; }
    public DateTime CreatedAt { get; set; }

    // Pricing info
    public decimal EstimatedPrice { get; set; }
    public decimal MinBidAllowed { get; set; }
    public decimal MaxBidAllowed { get; set; }

    // Current bidding status
    public int CurrentBidCount { get; set; }
    public decimal? LowestCurrentBid { get; set; }
    public bool HasAlreadyBid { get; set; }
    public decimal? MyBidAmount { get; set; }
    public string? MyBidStatus { get; set; }

    // Distance from DP's current location
    public decimal? DistanceFromDPKm { get; set; }

    // Direction check
    public bool MatchesPreferredDirection { get; set; } = true;

    // Time remaining
    public int MinutesRemainingTosBid { get; set; }
}

/// <summary>
/// DP's bid history
/// </summary>
public class DPBidHistoryDto
{
    public List<BidHistoryItemDto> Bids { get; set; } = new();
    public int TotalBids { get; set; }
    public int AcceptedBids { get; set; }
    public int RejectedBids { get; set; }
    public decimal AverageBidAmount { get; set; }
    public decimal WinRate { get; set; }
}

public class BidHistoryItemDto
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string DropAddress { get; set; } = string.Empty;
    public decimal BidAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

/// <summary>
/// Service area configuration request
/// </summary>
public class SetServiceAreaRequest
{
    [Required]
    [Range(-90, 90)]
    public decimal CenterLat { get; set; }

    [Required]
    [Range(-180, 180)]
    public decimal CenterLng { get; set; }

    [Required]
    [Range(1, 100, ErrorMessage = "Radius must be between 1 and 100 km")]
    public decimal RadiusKm { get; set; }

    // One-direction preference
    public bool OneDirectionOnly { get; set; } = false;
    public string? PreferredDirection { get; set; } // NORTH, SOUTH, EAST, WEST, ANY
}

/// <summary>
/// Service area response
/// </summary>
public class ServiceAreaDto
{
    public decimal CenterLat { get; set; }
    public decimal CenterLng { get; set; }
    public decimal RadiusKm { get; set; }
    public bool OneDirectionOnly { get; set; }
    public string? PreferredDirection { get; set; }

    // For map display
    public List<LatLngDto> BoundaryPoints { get; set; } = new();
}

public class LatLngDto
{
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
}
