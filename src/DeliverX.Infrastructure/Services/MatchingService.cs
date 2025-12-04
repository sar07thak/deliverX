using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliverX.Application.DTOs.Delivery;
using DeliverX.Application.Services;
using DeliverX.Domain.Entities;
using DeliverX.Infrastructure.Data;

namespace DeliverX.Infrastructure.Services;

public class MatchingService : IMatchingService
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceAreaService _serviceAreaService;
    private readonly IPricingService _pricingService;
    private readonly ILogger<MatchingService> _logger;

    private const int MaxMatchingAttempts = 3;
    private const int DefaultTopCandidates = 5;

    public MatchingService(
        ApplicationDbContext context,
        IServiceAreaService serviceAreaService,
        IPricingService pricingService,
        ILogger<MatchingService> logger)
    {
        _context = context;
        _serviceAreaService = serviceAreaService;
        _pricingService = pricingService;
        _logger = logger;
    }

    public async Task<MatchingResultDto> MatchDeliveryAsync(
        Guid deliveryId,
        int attempt = 1,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Matching delivery {DeliveryId}, attempt {Attempt}", deliveryId, attempt);

        var delivery = await _context.Deliveries.FindAsync(new object[] { deliveryId }, ct);
        if (delivery == null)
        {
            return new MatchingResultDto
            {
                IsSuccess = false,
                ErrorMessage = "Delivery not found"
            };
        }

        // 1. Find eligible DPs by service area
        var eligibleDPsResult = await _serviceAreaService.FindEligibleDPsAsync(
            new Application.DTOs.ServiceArea.FindEligibleDPsRequest
            {
                PickupLat = delivery.PickupLat,
                PickupLng = delivery.PickupLng,
                DropLat = delivery.DropLat,
                DropLng = delivery.DropLng,
                MaxResults = 20
            }, ct);

        if (eligibleDPsResult.TotalMatches == 0)
        {
            _logger.LogWarning("No DPs found for delivery {DeliveryId}", deliveryId);

            if (attempt < MaxMatchingAttempts)
            {
                // Could implement wider search radius here
                return await MatchDeliveryAsync(deliveryId, attempt + 1, ct);
            }

            delivery.Status = "UNASSIGNABLE";
            delivery.MatchingAttempts = attempt;
            await _context.SaveChangesAsync(ct);

            return new MatchingResultDto
            {
                IsSuccess = false,
                DeliveryId = deliveryId,
                ErrorMessage = "No delivery partners available for this route",
                Status = "UNASSIGNABLE"
            };
        }

        var eligibleDPIds = eligibleDPsResult.MatchedDPs.Select(d => d.DPId).ToList();

        // 2. Filter by availability
        var availableDPIds = await _context.DPAvailabilities
            .Where(a => eligibleDPIds.Contains(a.DPId) &&
                       (a.Status == "AVAILABLE" || a.Status == "OFFLINE"))
            .Select(a => a.DPId)
            .ToListAsync(ct);

        // If no availability records, assume all eligible DPs are available
        if (!availableDPIds.Any())
        {
            availableDPIds = eligibleDPIds;
        }

        if (!availableDPIds.Any())
        {
            _logger.LogWarning("No available DPs for delivery {DeliveryId}", deliveryId);

            if (attempt < MaxMatchingAttempts)
            {
                return await MatchDeliveryAsync(deliveryId, attempt + 1, ct);
            }

            delivery.Status = "UNASSIGNABLE";
            await _context.SaveChangesAsync(ct);

            return new MatchingResultDto
            {
                IsSuccess = false,
                DeliveryId = deliveryId,
                ErrorMessage = "All delivery partners are currently busy",
                Status = "UNASSIGNABLE"
            };
        }

        // 3. Get pricing and calculate match scores
        var matchedDPs = new List<MatchedDPInfo>();

        foreach (var matchedDP in eligibleDPsResult.MatchedDPs.Where(d => availableDPIds.Contains(d.DPId)))
        {
            // Get DP's last location for proximity calculation
            var dpAvailability = await _context.DPAvailabilities
                .FirstOrDefaultAsync(a => a.DPId == matchedDP.DPId, ct);

            var distanceFromPickup = 0.0;
            if (dpAvailability?.LastLocationLat != null && dpAvailability?.LastLocationLng != null)
            {
                distanceFromPickup = _serviceAreaService.CalculateDistanceKm(
                    (double)dpAvailability.LastLocationLat.Value,
                    (double)dpAvailability.LastLocationLng.Value,
                    (double)delivery.PickupLat,
                    (double)delivery.PickupLng);
            }

            // Calculate price for this DP
            var pricingConfig = await _pricingService.GetDPPricingConfigAsync(matchedDP.DPId, ct);
            var estimatedPrice = matchedDP.EstimatedPrice > 0
                ? matchedDP.EstimatedPrice
                : delivery.EstimatedPrice ?? 50m;

            // Calculate match score (lower is better)
            // 40% price, 30% rating (inverted), 20% proximity, 10% completion rate
            var priceScore = (double)estimatedPrice / 100; // Normalize
            var ratingScore = 5.0 - (double)matchedDP.Rating; // Invert (5.0 rating = 0 score)
            var proximityScore = distanceFromPickup / 10; // Normalize

            var matchScore = (priceScore * 0.4) + (ratingScore * 0.3) + (proximityScore * 0.2);

            matchedDPs.Add(new MatchedDPInfo
            {
                DPId = matchedDP.DPId,
                DPName = matchedDP.DPName,
                Phone = matchedDP.Phone,
                Rating = matchedDP.Rating,
                EstimatedPrice = estimatedPrice,
                DistanceFromPickupKm = (decimal)distanceFromPickup,
                MatchScore = (decimal)matchScore
            });
        }

        // 4. Rank and take top candidates
        var rankedDPs = matchedDPs
            .OrderBy(d => d.MatchScore)
            .Take(DefaultTopCandidates)
            .ToList();

        // 5. Record matching history (simulate notifications)
        foreach (var dp in rankedDPs)
        {
            // Check if already notified in this attempt
            var existingMatch = await _context.DeliveryMatchingHistories
                .FirstOrDefaultAsync(m => m.DeliveryId == deliveryId &&
                                         m.DPId == dp.DPId &&
                                         m.MatchingAttempt == attempt, ct);

            if (existingMatch == null)
            {
                _context.DeliveryMatchingHistories.Add(new DeliveryMatchingHistory
                {
                    Id = Guid.NewGuid(),
                    DeliveryId = deliveryId,
                    DPId = dp.DPId,
                    MatchingAttempt = attempt,
                    NotifiedAt = DateTime.UtcNow
                });
            }
        }

        // 6. Update delivery status
        delivery.Status = "MATCHING";
        delivery.MatchingAttempts = attempt;
        delivery.UpdatedAt = DateTime.UtcNow;

        // Log event
        _context.DeliveryEvents.Add(new DeliveryEvent
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            EventType = "MATCHED",
            FromStatus = "CREATED",
            ToStatus = "MATCHING",
            ActorType = "SYSTEM",
            Metadata = JsonSerializer.Serialize(new
            {
                attempt,
                candidateCount = rankedDPs.Count,
                dpIds = rankedDPs.Select(d => d.DPId)
            }),
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Matched {Count} DPs for delivery {DeliveryId}",
            rankedDPs.Count, deliveryId);

        return new MatchingResultDto
        {
            IsSuccess = true,
            DeliveryId = deliveryId,
            MatchedDPs = rankedDPs,
            TotalMatches = rankedDPs.Count,
            NotificationsSent = rankedDPs.Count,
            Status = "MATCHING_IN_PROGRESS"
        };
    }

    public async Task<AcceptDeliveryResponse> AcceptDeliveryAsync(
        Guid deliveryId,
        Guid dpId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("DP {DPId} accepting delivery {DeliveryId}", dpId, deliveryId);

        var delivery = await _context.Deliveries.FindAsync(new object[] { deliveryId }, ct);
        if (delivery == null)
        {
            return new AcceptDeliveryResponse
            {
                IsSuccess = false,
                ErrorCode = "NOT_FOUND",
                Message = "Delivery not found"
            };
        }

        // Check if already assigned
        if (delivery.AssignedDPId.HasValue)
        {
            return new AcceptDeliveryResponse
            {
                IsSuccess = false,
                ErrorCode = "ALREADY_ASSIGNED",
                Message = "This delivery has already been accepted by another partner",
                DeliveryId = deliveryId
            };
        }

        // Assign and accept delivery
        delivery.AssignedDPId = dpId;
        delivery.AssignedAt = DateTime.UtcNow;
        delivery.Status = "ACCEPTED"; // DP has accepted the delivery
        delivery.UpdatedAt = DateTime.UtcNow;

        // Update DP availability
        var dpAvailability = await _context.DPAvailabilities
            .FirstOrDefaultAsync(a => a.DPId == dpId, ct);

        if (dpAvailability != null)
        {
            dpAvailability.Status = "BUSY";
            dpAvailability.CurrentDeliveryId = deliveryId;
            dpAvailability.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create availability record
            _context.DPAvailabilities.Add(new DPAvailability
            {
                Id = Guid.NewGuid(),
                DPId = dpId,
                Status = "BUSY",
                CurrentDeliveryId = deliveryId,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Update matching history
        var matchHistory = await _context.DeliveryMatchingHistories
            .FirstOrDefaultAsync(m => m.DeliveryId == deliveryId && m.DPId == dpId, ct);

        if (matchHistory != null)
        {
            matchHistory.ResponseType = "ACCEPTED";
            matchHistory.RespondedAt = DateTime.UtcNow;
        }

        // Log event
        _context.DeliveryEvents.Add(new DeliveryEvent
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            EventType = "ACCEPTED",
            FromStatus = "MATCHING",
            ToStatus = "ACCEPTED",
            ActorId = dpId,
            ActorType = "DP",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);

        // Calculate estimated earning
        var commissionBreakdown = await _pricingService.CalculateCommissionAsync(
            dpId, delivery.EstimatedPrice ?? 0, ct);

        _logger.LogInformation("Delivery {DeliveryId} assigned to DP {DPId}", deliveryId, dpId);

        return new AcceptDeliveryResponse
        {
            IsSuccess = true,
            DeliveryId = deliveryId,
            Status = "ACCEPTED",
            Message = "Delivery accepted. Proceed to pickup location.",
            Pickup = new LocationInfo
            {
                Lat = delivery.PickupLat,
                Lng = delivery.PickupLng,
                Address = delivery.PickupAddress,
                ContactName = delivery.PickupContactName,
                ContactPhone = delivery.PickupContactPhone,
                Instructions = delivery.PickupInstructions
            },
            NavigationUrl = $"https://maps.google.com/?daddr={delivery.PickupLat},{delivery.PickupLng}",
            EstimatedEarning = commissionBreakdown.DPEarning
        };
    }

    public async Task<bool> RejectDeliveryAsync(
        Guid deliveryId,
        Guid dpId,
        RejectDeliveryRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("DP {DPId} rejecting delivery {DeliveryId}", dpId, deliveryId);

        // Update matching history
        var matchHistory = await _context.DeliveryMatchingHistories
            .FirstOrDefaultAsync(m => m.DeliveryId == deliveryId && m.DPId == dpId, ct);

        if (matchHistory != null)
        {
            matchHistory.ResponseType = "REJECTED";
            matchHistory.RespondedAt = DateTime.UtcNow;
            matchHistory.RejectionReason = request.Reason;
        }

        // Log event
        _context.DeliveryEvents.Add(new DeliveryEvent
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            EventType = "REJECTED",
            ActorId = dpId,
            ActorType = "DP",
            Metadata = JsonSerializer.Serialize(new { reason = request.Reason }),
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);

        // Check if all matched DPs have rejected
        var delivery = await _context.Deliveries.FindAsync(new object[] { deliveryId }, ct);
        if (delivery != null && delivery.Status == "MATCHING")
        {
            var pendingResponses = await _context.DeliveryMatchingHistories
                .Where(m => m.DeliveryId == deliveryId &&
                           m.MatchingAttempt == delivery.MatchingAttempts &&
                           m.ResponseType == null)
                .CountAsync(ct);

            if (pendingResponses == 0)
            {
                // All DPs rejected, try re-matching
                _logger.LogInformation("All DPs rejected delivery {DeliveryId}, attempting re-match",
                    deliveryId);

                if (delivery.MatchingAttempts < MaxMatchingAttempts)
                {
                    await MatchDeliveryAsync(deliveryId, delivery.MatchingAttempts + 1, ct);
                }
                else
                {
                    delivery.Status = "UNASSIGNABLE";
                    delivery.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(ct);
                }
            }
        }

        return true;
    }

    public async Task<DPAvailabilityDto> UpdateDPAvailabilityAsync(
        Guid dpId,
        UpdateDPAvailabilityRequest request,
        CancellationToken ct = default)
    {
        var availability = await _context.DPAvailabilities
            .FirstOrDefaultAsync(a => a.DPId == dpId, ct);

        if (availability == null)
        {
            availability = new DPAvailability
            {
                Id = Guid.NewGuid(),
                DPId = dpId
            };
            _context.DPAvailabilities.Add(availability);
        }

        availability.Status = request.Status;
        if (request.Lat.HasValue && request.Lng.HasValue)
        {
            availability.LastLocationLat = request.Lat;
            availability.LastLocationLng = request.Lng;
            availability.LastLocationUpdatedAt = DateTime.UtcNow;
        }
        availability.UpdatedAt = DateTime.UtcNow;

        // If going offline or on break while on delivery, don't change current delivery
        if (request.Status == "AVAILABLE" && availability.CurrentDeliveryId.HasValue)
        {
            // Check if delivery is still active
            var delivery = await _context.Deliveries
                .FindAsync(new object[] { availability.CurrentDeliveryId.Value }, ct);

            if (delivery != null && (delivery.Status == "DELIVERED" || delivery.Status == "CANCELLED"))
            {
                availability.CurrentDeliveryId = null;
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("DP {DPId} availability updated to {Status}", dpId, request.Status);

        return new DPAvailabilityDto
        {
            DPId = availability.DPId,
            Status = availability.Status,
            CurrentDeliveryId = availability.CurrentDeliveryId,
            LastLocationLat = availability.LastLocationLat,
            LastLocationLng = availability.LastLocationLng,
            LastLocationUpdatedAt = availability.LastLocationUpdatedAt,
            UpdatedAt = availability.UpdatedAt
        };
    }

    public async Task<DPAvailabilityDto?> GetDPAvailabilityAsync(
        Guid dpId,
        CancellationToken ct = default)
    {
        var availability = await _context.DPAvailabilities
            .FirstOrDefaultAsync(a => a.DPId == dpId, ct);

        if (availability == null) return null;

        return new DPAvailabilityDto
        {
            DPId = availability.DPId,
            Status = availability.Status,
            CurrentDeliveryId = availability.CurrentDeliveryId,
            LastLocationLat = availability.LastLocationLat,
            LastLocationLng = availability.LastLocationLng,
            LastLocationUpdatedAt = availability.LastLocationUpdatedAt,
            UpdatedAt = availability.UpdatedAt
        };
    }
}
