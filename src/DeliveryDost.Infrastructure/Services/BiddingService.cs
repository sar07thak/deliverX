using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.Common;
using DeliveryDost.Application.DTOs.Bidding;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class BiddingService : IBiddingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BiddingService> _logger;
    private readonly IDistanceCalculatorService _distanceCalculator;

    private const int DEFAULT_BID_EXPIRY_MINUTES = 15;
    private const int DEFAULT_BID_WINDOW_MINUTES = 30;
    private const decimal DEFAULT_MIN_BID_PERCENTAGE = 0.5m;
    private const decimal DEFAULT_MAX_BID_PERCENTAGE = 1.5m;

    public BiddingService(
        ApplicationDbContext context,
        ILogger<BiddingService> logger,
        IDistanceCalculatorService distanceCalculator)
    {
        _context = context;
        _logger = logger;
        _distanceCalculator = distanceCalculator;
    }

    public async Task<Result<PlaceBidResponse>> PlaceBidAsync(Guid dpId, PlaceBidRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Get delivery
            var delivery = await _context.Deliveries
                .FirstOrDefaultAsync(d => d.Id == request.DeliveryId, cancellationToken);

            if (delivery == null)
                return Result<PlaceBidResponse>.Failure("Delivery not found");

            if (delivery.Status != "CREATED" && delivery.Status != "MATCHING")
                return Result<PlaceBidResponse>.Failure("Delivery is not open for bidding");

            // Check if DP already has a bid on this delivery
            var existingBid = await _context.DeliveryBids
                .FirstOrDefaultAsync(b => b.DeliveryId == request.DeliveryId && b.DPId == dpId && b.Status == "PENDING", cancellationToken);

            if (existingBid != null)
                return Result<PlaceBidResponse>.Failure("You already have a pending bid on this delivery");

            // Get DP profile
            var dpProfile = await _context.DeliveryPartnerProfiles
                .FirstOrDefaultAsync(p => p.UserId == dpId, cancellationToken);

            if (dpProfile == null)
                return Result<PlaceBidResponse>.Failure("Delivery Partner profile not found");

            // Calculate distance to pickup
            decimal? distanceToPickup = null;
            if (request.CurrentLat.HasValue && request.CurrentLng.HasValue)
            {
                var distResult = _distanceCalculator.CalculateHaversineDistance(
                    request.CurrentLat.Value, request.CurrentLng.Value,
                    delivery.PickupLat, delivery.PickupLng);
                distanceToPickup = distResult.DistanceKm;
            }

            // Check if bid exceeds max rate (from registration)
            bool exceedsMaxRate = dpProfile.MaxBidRate.HasValue && request.BidAmount > dpProfile.MaxBidRate.Value;

            // Create bid
            var bid = new DeliveryBid
            {
                Id = Guid.NewGuid(),
                DeliveryId = request.DeliveryId,
                DPId = dpId,
                BidAmount = request.BidAmount,
                BidNotes = request.BidNotes,
                DPLatitude = request.CurrentLat,
                DPLongitude = request.CurrentLng,
                DistanceToPickupKm = distanceToPickup,
                EstimatedPickupMinutes = request.EstimatedPickupMinutes,
                EstimatedDeliveryMinutes = request.EstimatedDeliveryMinutes,
                Status = "PENDING",
                ExceedsMaxRate = exceedsMaxRate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(DEFAULT_BID_EXPIRY_MINUTES)
            };

            _context.DeliveryBids.Add(bid);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("DP {DPId} placed bid {BidId} of {Amount} on delivery {DeliveryId}",
                dpId, bid.Id, bid.BidAmount, request.DeliveryId);

            return Result<PlaceBidResponse>.Success(new PlaceBidResponse
            {
                BidId = bid.Id,
                Status = bid.Status,
                ExceedsMaxRate = bid.ExceedsMaxRate,
                Message = exceedsMaxRate ? "Warning: Bid exceeds your registered max rate" : "Bid placed successfully",
                ExpiresAt = bid.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing bid for DP {DPId} on delivery {DeliveryId}", dpId, request.DeliveryId);
            return Result<PlaceBidResponse>.Failure("Failed to place bid");
        }
    }

    public async Task<Result<bool>> WithdrawBidAsync(Guid bidId, Guid dpId, CancellationToken cancellationToken)
    {
        try
        {
            var bid = await _context.DeliveryBids
                .FirstOrDefaultAsync(b => b.Id == bidId && b.DPId == dpId, cancellationToken);

            if (bid == null)
                return Result<bool>.Failure("Bid not found");

            if (bid.Status != "PENDING")
                return Result<bool>.Failure("Only pending bids can be withdrawn");

            bid.Status = "WITHDRAWN";
            bid.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("DP {DPId} withdrew bid {BidId}", dpId, bidId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error withdrawing bid {BidId} for DP {DPId}", bidId, dpId);
            return Result<bool>.Failure("Failed to withdraw bid");
        }
    }

    public async Task<Result<bool>> AcceptBidAsync(Guid requesterId, AcceptBidRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Get delivery
            var delivery = await _context.Deliveries
                .FirstOrDefaultAsync(d => d.Id == request.DeliveryId && d.RequesterId == requesterId, cancellationToken);

            if (delivery == null)
                return Result<bool>.Failure("Delivery not found or you don't have permission");

            // Get bid
            var bid = await _context.DeliveryBids
                .FirstOrDefaultAsync(b => b.Id == request.BidId && b.DeliveryId == request.DeliveryId, cancellationToken);

            if (bid == null)
                return Result<bool>.Failure("Bid not found");

            if (bid.Status != "PENDING")
                return Result<bool>.Failure("Bid is no longer available");

            if (DateTime.UtcNow > bid.ExpiresAt)
                return Result<bool>.Failure("Bid has expired");

            // Accept this bid
            bid.Status = "ACCEPTED";
            bid.RespondedAt = DateTime.UtcNow;
            bid.UpdatedAt = DateTime.UtcNow;

            // Reject all other pending bids for this delivery
            var otherBids = await _context.DeliveryBids
                .Where(b => b.DeliveryId == request.DeliveryId && b.Id != request.BidId && b.Status == "PENDING")
                .ToListAsync(cancellationToken);

            foreach (var otherBid in otherBids)
            {
                otherBid.Status = "REJECTED";
                otherBid.RejectionReason = "Another bid was accepted";
                otherBid.RespondedAt = DateTime.UtcNow;
                otherBid.UpdatedAt = DateTime.UtcNow;
            }

            // Update delivery with assigned DP and price
            delivery.AssignedDPId = bid.DPId;
            delivery.AssignedAt = DateTime.UtcNow;
            delivery.FinalPrice = bid.BidAmount;
            delivery.Status = "ASSIGNED";
            delivery.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Requester {RequesterId} accepted bid {BidId} for delivery {DeliveryId}",
                requesterId, request.BidId, request.DeliveryId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting bid {BidId} for delivery {DeliveryId}", request.BidId, request.DeliveryId);
            return Result<bool>.Failure("Failed to accept bid");
        }
    }

    public async Task<Result<bool>> RejectBidAsync(Guid requesterId, RejectBidRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var bid = await _context.DeliveryBids
                .Include(b => b.Delivery)
                .FirstOrDefaultAsync(b => b.Id == request.BidId && b.Delivery.RequesterId == requesterId, cancellationToken);

            if (bid == null)
                return Result<bool>.Failure("Bid not found or you don't have permission");

            if (bid.Status != "PENDING")
                return Result<bool>.Failure("Bid is no longer pending");

            bid.Status = "REJECTED";
            bid.RejectionReason = request.RejectionReason;
            bid.RespondedAt = DateTime.UtcNow;
            bid.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Requester {RequesterId} rejected bid {BidId}", requesterId, request.BidId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting bid {BidId}", request.BidId);
            return Result<bool>.Failure("Failed to reject bid");
        }
    }

    public async Task<Result<DeliveryBidsResponse>> GetDeliveryBidsAsync(Guid deliveryId, Guid requesterId, CancellationToken cancellationToken)
    {
        try
        {
            var delivery = await _context.Deliveries
                .FirstOrDefaultAsync(d => d.Id == deliveryId && d.RequesterId == requesterId, cancellationToken);

            if (delivery == null)
                return Result<DeliveryBidsResponse>.Failure("Delivery not found or you don't have permission");

            var bids = await _context.DeliveryBids
                .Include(b => b.DP)
                .Where(b => b.DeliveryId == deliveryId)
                .OrderBy(b => b.BidAmount)
                .Select(b => new BidDto
                {
                    Id = b.Id,
                    DeliveryId = b.DeliveryId,
                    DPId = b.DPId,
                    DPName = b.DP.FullName ?? "",
                    DPPhone = b.DP.Phone,
                    BidAmount = b.BidAmount,
                    BidNotes = b.BidNotes,
                    DistanceToPickupKm = b.DistanceToPickupKm,
                    EstimatedPickupMinutes = b.EstimatedPickupMinutes,
                    EstimatedDeliveryMinutes = b.EstimatedDeliveryMinutes,
                    Status = b.Status,
                    ExceedsMaxRate = b.ExceedsMaxRate,
                    CreatedAt = b.CreatedAt,
                    ExpiresAt = b.ExpiresAt
                })
                .ToListAsync(cancellationToken);

            var pendingBids = bids.Where(b => b.Status == "PENDING").ToList();

            return Result<DeliveryBidsResponse>.Success(new DeliveryBidsResponse
            {
                DeliveryId = deliveryId,
                Bids = bids,
                TotalBids = bids.Count,
                LowestBid = pendingBids.Any() ? pendingBids.Min(b => b.BidAmount) : null,
                HighestBid = pendingBids.Any() ? pendingBids.Max(b => b.BidAmount) : null,
                AverageBid = pendingBids.Any() ? pendingBids.Average(b => b.BidAmount) : null,
                EstimatedPrice = delivery.EstimatedPrice ?? 0,
                BiddingOpen = delivery.Status == "CREATED" || delivery.Status == "MATCHING",
                BiddingClosesAt = delivery.CreatedAt.AddMinutes(DEFAULT_BID_WINDOW_MINUTES)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bids for delivery {DeliveryId}", deliveryId);
            return Result<DeliveryBidsResponse>.Failure("Failed to get bids");
        }
    }

    public async Task<Result<DPBidHistoryDto>> GetDPBidHistoryAsync(Guid dpId, int page, int pageSize, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.DeliveryBids
                .Include(b => b.Delivery)
                .Where(b => b.DPId == dpId)
                .OrderByDescending(b => b.CreatedAt);

            var totalBids = await query.CountAsync(cancellationToken);
            var acceptedBids = await query.CountAsync(b => b.Status == "ACCEPTED", cancellationToken);
            var rejectedBids = await query.CountAsync(b => b.Status == "REJECTED", cancellationToken);
            var avgBidAmount = totalBids > 0 ? await query.AverageAsync(b => b.BidAmount, cancellationToken) : 0;

            var bids = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BidHistoryItemDto
                {
                    Id = b.Id,
                    DeliveryId = b.DeliveryId,
                    PickupAddress = b.Delivery.PickupAddress,
                    DropAddress = b.Delivery.DropAddress,
                    BidAmount = b.BidAmount,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    RespondedAt = b.RespondedAt
                })
                .ToListAsync(cancellationToken);

            return Result<DPBidHistoryDto>.Success(new DPBidHistoryDto
            {
                Bids = bids,
                TotalBids = totalBids,
                AcceptedBids = acceptedBids,
                RejectedBids = rejectedBids,
                AverageBidAmount = avgBidAmount,
                WinRate = totalBids > 0 ? (decimal)acceptedBids / totalBids * 100 : 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bid history for DP {DPId}", dpId);
            return Result<DPBidHistoryDto>.Failure("Failed to get bid history");
        }
    }

    public async Task<Result<List<BidDto>>> GetDPActiveBidsAsync(Guid dpId, CancellationToken cancellationToken)
    {
        try
        {
            var bids = await _context.DeliveryBids
                .Include(b => b.Delivery)
                .Where(b => b.DPId == dpId && b.Status == "PENDING" && b.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BidDto
                {
                    Id = b.Id,
                    DeliveryId = b.DeliveryId,
                    DPId = b.DPId,
                    BidAmount = b.BidAmount,
                    BidNotes = b.BidNotes,
                    DistanceToPickupKm = b.DistanceToPickupKm,
                    EstimatedPickupMinutes = b.EstimatedPickupMinutes,
                    EstimatedDeliveryMinutes = b.EstimatedDeliveryMinutes,
                    Status = b.Status,
                    ExceedsMaxRate = b.ExceedsMaxRate,
                    CreatedAt = b.CreatedAt,
                    ExpiresAt = b.ExpiresAt
                })
                .ToListAsync(cancellationToken);

            return Result<List<BidDto>>.Success(bids);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active bids for DP {DPId}", dpId);
            return Result<List<BidDto>>.Failure("Failed to get active bids");
        }
    }

    public async Task<Result<List<AvailableDeliveryForBidDto>>> GetAvailableDeliveriesForBiddingAsync(
        Guid dpId,
        decimal? currentLat,
        decimal? currentLng,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get DP profile with service area
            var dpProfile = await _context.DeliveryPartnerProfiles
                .FirstOrDefaultAsync(p => p.UserId == dpId, cancellationToken);

            if (dpProfile == null)
                return Result<List<AvailableDeliveryForBidDto>>.Failure("DP profile not found");

            // Get deliveries open for bidding
            var deliveriesQuery = _context.Deliveries
                .Where(d => d.Status == "CREATED" || d.Status == "MATCHING")
                .Where(d => d.CreatedAt > DateTime.UtcNow.AddMinutes(-DEFAULT_BID_WINDOW_MINUTES));

            var deliveries = await deliveriesQuery.ToListAsync(cancellationToken);

            // Filter by service area if configured
            if (dpProfile.ServiceAreaCenterLat.HasValue &&
                dpProfile.ServiceAreaCenterLng.HasValue &&
                dpProfile.ServiceAreaRadiusKm.HasValue)
            {
                deliveries = deliveries.Where(d =>
                {
                    var pickupDistance = _distanceCalculator.CalculateHaversineDistance(
                        dpProfile.ServiceAreaCenterLat.Value, dpProfile.ServiceAreaCenterLng.Value,
                        d.PickupLat, d.PickupLng).DistanceKm;
                    return pickupDistance <= dpProfile.ServiceAreaRadiusKm.Value;
                }).ToList();
            }

            // Filter by direction preference if one-direction only
            if (dpProfile.OneDirectionOnly && !string.IsNullOrEmpty(dpProfile.PreferredDirection))
            {
                deliveries = deliveries.Where(d =>
                    MatchesDirection(
                        dpProfile.ServiceAreaCenterLat ?? d.PickupLat,
                        dpProfile.ServiceAreaCenterLng ?? d.PickupLng,
                        d.DropLat, d.DropLng,
                        dpProfile.PreferredDirection)).ToList();
            }

            // Get existing bids by this DP
            var dpBids = await _context.DeliveryBids
                .Where(b => b.DPId == dpId && deliveries.Select(d => d.Id).Contains(b.DeliveryId))
                .ToListAsync(cancellationToken);

            // Get bid counts for each delivery
            var bidCounts = await _context.DeliveryBids
                .Where(b => deliveries.Select(d => d.Id).Contains(b.DeliveryId) && b.Status == "PENDING")
                .GroupBy(b => b.DeliveryId)
                .Select(g => new { DeliveryId = g.Key, Count = g.Count(), LowestBid = g.Min(b => b.BidAmount) })
                .ToListAsync(cancellationToken);

            var result = deliveries.Select(d =>
            {
                var myBid = dpBids.FirstOrDefault(b => b.DeliveryId == d.Id);
                var bidInfo = bidCounts.FirstOrDefault(b => b.DeliveryId == d.Id);
                var estimatedPrice = d.EstimatedPrice ?? 0;

                decimal? distanceFromDP = null;
                if (currentLat.HasValue && currentLng.HasValue)
                {
                    distanceFromDP = _distanceCalculator.CalculateHaversineDistance(
                        currentLat.Value, currentLng.Value,
                        d.PickupLat, d.PickupLng).DistanceKm;
                }

                return new AvailableDeliveryForBidDto
                {
                    Id = d.Id,
                    PickupAddress = d.PickupAddress,
                    DropAddress = d.DropAddress,
                    PickupLat = d.PickupLat,
                    PickupLng = d.PickupLng,
                    DropLat = d.DropLat,
                    DropLng = d.DropLng,
                    DistanceKm = d.DistanceKm ?? 0,
                    WeightKg = d.WeightKg,
                    PackageType = d.PackageType,
                    Priority = d.Priority,
                    IsHazardous = d.IsHazardous,
                    CautionType = d.CautionType,
                    CreatedAt = d.CreatedAt,
                    EstimatedPrice = estimatedPrice,
                    MinBidAllowed = estimatedPrice * DEFAULT_MIN_BID_PERCENTAGE,
                    MaxBidAllowed = Math.Min(estimatedPrice * DEFAULT_MAX_BID_PERCENTAGE, dpProfile.MaxBidRate ?? decimal.MaxValue),
                    CurrentBidCount = bidInfo?.Count ?? 0,
                    LowestCurrentBid = bidInfo?.LowestBid,
                    HasAlreadyBid = myBid != null,
                    MyBidAmount = myBid?.BidAmount,
                    MyBidStatus = myBid?.Status,
                    DistanceFromDPKm = distanceFromDP,
                    MatchesPreferredDirection = true, // Already filtered above
                    MinutesRemainingTosBid = Math.Max(0, (int)(d.CreatedAt.AddMinutes(DEFAULT_BID_WINDOW_MINUTES) - DateTime.UtcNow).TotalMinutes)
                };
            })
            .OrderBy(d => d.DistanceFromDPKm ?? decimal.MaxValue)
            .ToList();

            return Result<List<AvailableDeliveryForBidDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available deliveries for bidding for DP {DPId}", dpId);
            return Result<List<AvailableDeliveryForBidDto>>.Failure("Failed to get available deliveries");
        }
    }

    public async Task<Result<ServiceAreaDto>> SetServiceAreaAsync(Guid dpId, SetServiceAreaRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _context.DeliveryPartnerProfiles
                .FirstOrDefaultAsync(p => p.UserId == dpId, cancellationToken);

            if (profile == null)
                return Result<ServiceAreaDto>.Failure("DP profile not found");

            profile.ServiceAreaCenterLat = request.CenterLat;
            profile.ServiceAreaCenterLng = request.CenterLng;
            profile.ServiceAreaRadiusKm = request.RadiusKm;
            profile.OneDirectionOnly = request.OneDirectionOnly;
            profile.PreferredDirection = request.PreferredDirection;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("DP {DPId} updated service area: Center ({Lat}, {Lng}), Radius {Radius}km, Direction {Direction}",
                dpId, request.CenterLat, request.CenterLng, request.RadiusKm, request.PreferredDirection);

            return Result<ServiceAreaDto>.Success(new ServiceAreaDto
            {
                CenterLat = request.CenterLat,
                CenterLng = request.CenterLng,
                RadiusKm = request.RadiusKm,
                OneDirectionOnly = request.OneDirectionOnly,
                PreferredDirection = request.PreferredDirection,
                BoundaryPoints = GenerateCircleBoundary(request.CenterLat, request.CenterLng, request.RadiusKm)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting service area for DP {DPId}", dpId);
            return Result<ServiceAreaDto>.Failure("Failed to set service area");
        }
    }

    public async Task<Result<ServiceAreaDto?>> GetServiceAreaAsync(Guid dpId, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _context.DeliveryPartnerProfiles
                .FirstOrDefaultAsync(p => p.UserId == dpId, cancellationToken);

            if (profile == null)
                return Result<ServiceAreaDto?>.Failure("DP profile not found");

            if (!profile.ServiceAreaCenterLat.HasValue ||
                !profile.ServiceAreaCenterLng.HasValue ||
                !profile.ServiceAreaRadiusKm.HasValue)
            {
                return Result<ServiceAreaDto?>.Success(null);
            }

            return Result<ServiceAreaDto?>.Success(new ServiceAreaDto
            {
                CenterLat = profile.ServiceAreaCenterLat.Value,
                CenterLng = profile.ServiceAreaCenterLng.Value,
                RadiusKm = profile.ServiceAreaRadiusKm.Value,
                OneDirectionOnly = profile.OneDirectionOnly,
                PreferredDirection = profile.PreferredDirection,
                BoundaryPoints = GenerateCircleBoundary(
                    profile.ServiceAreaCenterLat.Value,
                    profile.ServiceAreaCenterLng.Value,
                    profile.ServiceAreaRadiusKm.Value)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service area for DP {DPId}", dpId);
            return Result<ServiceAreaDto?>.Failure("Failed to get service area");
        }
    }

    public async Task<Result<bool>> CheckDirectionMatchAsync(
        Guid dpId,
        decimal pickupLat, decimal pickupLng,
        decimal dropLat, decimal dropLng,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _context.DeliveryPartnerProfiles
                .FirstOrDefaultAsync(p => p.UserId == dpId, cancellationToken);

            if (profile == null)
                return Result<bool>.Failure("DP profile not found");

            if (!profile.OneDirectionOnly || string.IsNullOrEmpty(profile.PreferredDirection))
                return Result<bool>.Success(true); // No direction restriction

            var centerLat = profile.ServiceAreaCenterLat ?? pickupLat;
            var centerLng = profile.ServiceAreaCenterLng ?? pickupLng;

            var matches = MatchesDirection(centerLat, centerLng, dropLat, dropLng, profile.PreferredDirection);

            return Result<bool>.Success(matches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking direction match for DP {DPId}", dpId);
            return Result<bool>.Failure("Failed to check direction");
        }
    }

    public async Task<Result<bool>> ValidateBidAmountAsync(
        Guid dpId,
        Guid deliveryId,
        decimal bidAmount,
        CancellationToken cancellationToken)
    {
        try
        {
            var delivery = await _context.Deliveries
                .FirstOrDefaultAsync(d => d.Id == deliveryId, cancellationToken);

            if (delivery == null)
                return Result<bool>.Failure("Delivery not found");

            var dpProfile = await _context.DeliveryPartnerProfiles
                .FirstOrDefaultAsync(p => p.UserId == dpId, cancellationToken);

            if (dpProfile == null)
                return Result<bool>.Failure("DP profile not found");

            var estimatedPrice = delivery.EstimatedPrice ?? 0;
            var minAllowed = estimatedPrice * DEFAULT_MIN_BID_PERCENTAGE;
            var maxAllowed = Math.Min(estimatedPrice * DEFAULT_MAX_BID_PERCENTAGE, dpProfile.MaxBidRate ?? decimal.MaxValue);

            if (bidAmount < minAllowed)
                return Result<bool>.Failure($"Bid must be at least ₹{minAllowed:F2}");

            if (bidAmount > maxAllowed)
                return Result<bool>.Failure($"Bid cannot exceed ₹{maxAllowed:F2}");

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating bid amount for DP {DPId} on delivery {DeliveryId}", dpId, deliveryId);
            return Result<bool>.Failure("Failed to validate bid");
        }
    }

    #region Private Helpers

    private static bool MatchesDirection(decimal centerLat, decimal centerLng, decimal destLat, decimal destLng, string preferredDirection)
    {
        if (preferredDirection == "ANY")
            return true;

        var bearing = CalculateBearing((double)centerLat, (double)centerLng, (double)destLat, (double)destLng);

        return preferredDirection switch
        {
            "NORTH" => bearing >= 315 || bearing < 45,
            "EAST" => bearing >= 45 && bearing < 135,
            "SOUTH" => bearing >= 135 && bearing < 225,
            "WEST" => bearing >= 225 && bearing < 315,
            _ => true
        };
    }

    private static double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        var dLon = DegreesToRadians(lon2 - lon1);
        var lat1Rad = DegreesToRadians(lat1);
        var lat2Rad = DegreesToRadians(lat2);

        var y = Math.Sin(dLon) * Math.Cos(lat2Rad);
        var x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);

        var bearing = Math.Atan2(y, x);
        bearing = RadiansToDegrees(bearing);
        bearing = (bearing + 360) % 360;

        return bearing;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
    private static double RadiansToDegrees(double radians) => radians * 180 / Math.PI;

    private static List<LatLngDto> GenerateCircleBoundary(decimal centerLat, decimal centerLng, decimal radiusKm, int points = 36)
    {
        var result = new List<LatLngDto>();
        var lat = (double)centerLat;
        var lng = (double)centerLng;
        var radius = (double)radiusKm;

        for (int i = 0; i < points; i++)
        {
            var angle = (360.0 / points) * i;
            var bearing = DegreesToRadians(angle);

            var latRad = DegreesToRadians(lat);
            var lngRad = DegreesToRadians(lng);
            var angularDistance = radius / 6371.0; // Earth's radius in km

            var newLatRad = Math.Asin(Math.Sin(latRad) * Math.Cos(angularDistance) +
                                       Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearing));
            var newLngRad = lngRad + Math.Atan2(
                Math.Sin(bearing) * Math.Sin(angularDistance) * Math.Cos(latRad),
                Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(newLatRad));

            result.Add(new LatLngDto
            {
                Lat = (decimal)RadiansToDegrees(newLatRad),
                Lng = (decimal)RadiansToDegrees(newLngRad)
            });
        }

        return result;
    }

    #endregion
}
