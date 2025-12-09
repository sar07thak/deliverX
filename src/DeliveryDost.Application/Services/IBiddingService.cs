using DeliveryDost.Application.Common;
using DeliveryDost.Application.DTOs.Bidding;

namespace DeliveryDost.Application.Services;

/// <summary>
/// Service for delivery bidding platform
/// </summary>
public interface IBiddingService
{
    // ====== BID OPERATIONS ======

    /// <summary>
    /// Place a bid on a delivery
    /// </summary>
    Task<Result<PlaceBidResponse>> PlaceBidAsync(Guid dpId, PlaceBidRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Withdraw a bid
    /// </summary>
    Task<Result<bool>> WithdrawBidAsync(Guid bidId, Guid dpId, CancellationToken cancellationToken);

    /// <summary>
    /// Accept a bid (by requester)
    /// </summary>
    Task<Result<bool>> AcceptBidAsync(Guid requesterId, AcceptBidRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Reject a bid
    /// </summary>
    Task<Result<bool>> RejectBidAsync(Guid requesterId, RejectBidRequest request, CancellationToken cancellationToken);

    // ====== BID QUERIES ======

    /// <summary>
    /// Get all bids for a delivery
    /// </summary>
    Task<Result<DeliveryBidsResponse>> GetDeliveryBidsAsync(Guid deliveryId, Guid requesterId, CancellationToken cancellationToken);

    /// <summary>
    /// Get DP's bid history
    /// </summary>
    Task<Result<DPBidHistoryDto>> GetDPBidHistoryAsync(Guid dpId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Get DP's active bids
    /// </summary>
    Task<Result<List<BidDto>>> GetDPActiveBidsAsync(Guid dpId, CancellationToken cancellationToken);

    // ====== AVAILABLE DELIVERIES FOR BIDDING ======

    /// <summary>
    /// Get deliveries available for bidding (based on DP's service area)
    /// </summary>
    Task<Result<List<AvailableDeliveryForBidDto>>> GetAvailableDeliveriesForBiddingAsync(
        Guid dpId,
        decimal? currentLat,
        decimal? currentLng,
        CancellationToken cancellationToken);

    // ====== SERVICE AREA OPERATIONS ======

    /// <summary>
    /// Set DP's service area (radius from center point)
    /// </summary>
    Task<Result<ServiceAreaDto>> SetServiceAreaAsync(Guid dpId, SetServiceAreaRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Get DP's service area
    /// </summary>
    Task<Result<ServiceAreaDto?>> GetServiceAreaAsync(Guid dpId, CancellationToken cancellationToken);

    // ====== DIRECTION VALIDATION ======

    /// <summary>
    /// Check if a delivery matches DP's preferred direction
    /// </summary>
    Task<Result<bool>> CheckDirectionMatchAsync(
        Guid dpId,
        decimal pickupLat, decimal pickupLng,
        decimal dropLat, decimal dropLng,
        CancellationToken cancellationToken);

    // ====== BID VALIDATION ======

    /// <summary>
    /// Validate if bid amount is within allowed range
    /// </summary>
    Task<Result<bool>> ValidateBidAmountAsync(
        Guid dpId,
        Guid deliveryId,
        decimal bidAmount,
        CancellationToken cancellationToken);
}
