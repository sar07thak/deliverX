namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Delivery bid from Delivery Partner
/// Implements the bidding platform feature for Available Jobs
/// </summary>
public class DeliveryBid
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public Guid DPId { get; set; }

    // Bid Amount
    public decimal BidAmount { get; set; }
    public string? BidNotes { get; set; }

    // DP Location at time of bid
    public decimal? DPLatitude { get; set; }
    public decimal? DPLongitude { get; set; }
    public decimal? DistanceToPickupKm { get; set; }

    // Estimated delivery time (in minutes)
    public int? EstimatedPickupMinutes { get; set; }
    public int? EstimatedDeliveryMinutes { get; set; }

    // Bid Status: PENDING, ACCEPTED, REJECTED, WITHDRAWN, EXPIRED
    public string Status { get; set; } = "PENDING";

    // If bid exceeds registration price, validation will fail
    public bool ExceedsMaxRate { get; set; } = false;

    // Response
    public DateTime? RespondedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    // Navigation
    public Delivery Delivery { get; set; } = null!;
    public User DP { get; set; } = null!;
}

/// <summary>
/// Configuration for delivery bidding rules
/// </summary>
public class BiddingConfig
{
    public Guid Id { get; set; }

    // Time limits
    public int BidExpiryMinutes { get; set; } = 15; // How long a bid is valid
    public int DeliveryBidWindowMinutes { get; set; } = 30; // How long delivery is open for bids

    // Bid limits
    public int MaxBidsPerDelivery { get; set; } = 10; // Max bids allowed per delivery
    public int MaxActiveBidsPerDP { get; set; } = 5; // Max pending bids a DP can have

    // Bid amount constraints
    public decimal MinBidPercentage { get; set; } = 0.5m; // Min bid as % of estimated price (50%)
    public decimal MaxBidPercentage { get; set; } = 1.5m; // Max bid as % of estimated price (150%)

    // Auto-selection rules
    public bool AutoSelectLowestBid { get; set; } = false;
    public int AutoSelectAfterMinutes { get; set; } = 10; // Auto-select after this time if not manual

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
