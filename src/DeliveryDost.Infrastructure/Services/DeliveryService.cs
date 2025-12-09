using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.DTOs.Delivery;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class DeliveryService : IDeliveryService
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceAreaService _serviceAreaService;
    private readonly IPricingService _pricingService;
    private readonly ILogger<DeliveryService> _logger;

    public DeliveryService(
        ApplicationDbContext context,
        IServiceAreaService serviceAreaService,
        IPricingService pricingService,
        ILogger<DeliveryService> logger)
    {
        _context = context;
        _serviceAreaService = serviceAreaService;
        _pricingService = pricingService;
        _logger = logger;
    }

    public async Task<CreateDeliveryResponse> CreateDeliveryAsync(
        CreateDeliveryRequest request,
        Guid requesterId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating delivery for requester {RequesterId}", requesterId);

        // Calculate distance
        var distanceKm = _serviceAreaService.CalculateDistanceKm(
            (double)request.Pickup.Lat, (double)request.Pickup.Lng,
            (double)request.Drop.Lat, (double)request.Drop.Lng);

        // Calculate estimated price
        var pricingResult = await _pricingService.CalculatePricingAsync(
            new Application.DTOs.Pricing.CalculatePricingRequest
            {
                PickupLat = request.Pickup.Lat,
                PickupLng = request.Pickup.Lng,
                DropLat = request.Drop.Lat,
                DropLng = request.Drop.Lng,
                WeightKg = request.Package.WeightKg,
                Priority = request.Priority,
                PreferredDPId = request.PreferredDPId
            }, ct);

        var estimatedPrice = pricingResult.IsSuccess ? pricingResult.CheapestPrice : 0;

        // Create delivery entity
        var delivery = new Delivery
        {
            Id = Guid.NewGuid(),
            RequesterId = requesterId,
            RequesterType = request.RequesterType,

            PickupLat = request.Pickup.Lat,
            PickupLng = request.Pickup.Lng,
            PickupAddress = request.Pickup.Address,
            PickupContactName = request.Pickup.ContactName,
            PickupContactPhone = request.Pickup.ContactPhone,
            PickupInstructions = request.Pickup.Instructions,

            DropLat = request.Drop.Lat,
            DropLng = request.Drop.Lng,
            DropAddress = request.Drop.Address,
            DropContactName = request.Drop.ContactName,
            DropContactPhone = request.Drop.ContactPhone,
            DropInstructions = request.Drop.Instructions,

            WeightKg = request.Package.WeightKg,
            PackageType = request.Package.Type,
            PackageDimensions = request.Package.Dimensions != null
                ? JsonSerializer.Serialize(request.Package.Dimensions)
                : null,
            PackageValue = request.Package.Value,
            PackageDescription = request.Package.Description,

            Priority = request.Priority,
            ScheduledAt = request.ScheduledAt,
            SpecialInstructions = request.SpecialInstructions,
            PreferredDPId = request.PreferredDPId,

            Status = "CREATED",
            EstimatedPrice = estimatedPrice,
            DistanceKm = (decimal)distanceKm,
            EstimatedDurationMinutes = EstimateDuration(distanceKm),

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Deliveries.Add(delivery);

        // Create initial event
        _context.DeliveryEvents.Add(new DeliveryEvent
        {
            Id = Guid.NewGuid(),
            DeliveryId = delivery.Id,
            EventType = "CREATED",
            ToStatus = "CREATED",
            ActorId = requesterId,
            ActorType = request.RequesterType,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Delivery {DeliveryId} created successfully", delivery.Id);

        return new CreateDeliveryResponse
        {
            DeliveryId = delivery.Id,
            Status = "CREATED",
            EstimatedPrice = estimatedPrice,
            EstimatedDistance = (decimal)distanceKm,
            EstimatedTime = EstimateTimeString(distanceKm),
            TrackingUrl = $"https://deliverx.com/track/{delivery.Id}",
            Message = "Delivery created. Finding delivery partners..."
        };
    }

    public async Task<DeliveryDetailsDto?> GetDeliveryAsync(
        Guid deliveryId,
        CancellationToken ct = default)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.Requester)
            .Include(d => d.AssignedDP)
            .FirstOrDefaultAsync(d => d.Id == deliveryId, ct);

        if (delivery == null) return null;

        // Get events/timeline
        var events = await _context.DeliveryEvents
            .Where(e => e.DeliveryId == deliveryId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(ct);

        // Get assigned DP details
        AssignedDPInfo? assignedDPInfo = null;
        if (delivery.AssignedDPId.HasValue)
        {
            var dpProfile = await _context.DeliveryPartnerProfiles
                .FirstOrDefaultAsync(p => p.UserId == delivery.AssignedDPId.Value, ct);

            if (dpProfile != null)
            {
                assignedDPInfo = new AssignedDPInfo
                {
                    DPId = delivery.AssignedDPId.Value,
                    DPName = dpProfile.FullName,
                    DPPhone = delivery.AssignedDP?.Phone,
                    DPPhoto = dpProfile.ProfilePhotoUrl,
                    Rating = 5.0m // TODO: Get actual rating
                };
            }
        }

        return new DeliveryDetailsDto
        {
            Id = delivery.Id,
            Status = delivery.Status,
            CreatedAt = delivery.CreatedAt,
            AssignedDP = assignedDPInfo,
            Pickup = new LocationInfo
            {
                Lat = delivery.PickupLat,
                Lng = delivery.PickupLng,
                Address = delivery.PickupAddress,
                ContactName = delivery.PickupContactName,
                ContactPhone = delivery.PickupContactPhone,
                Instructions = delivery.PickupInstructions
            },
            Drop = new LocationInfo
            {
                Lat = delivery.DropLat,
                Lng = delivery.DropLng,
                Address = delivery.DropAddress,
                ContactName = delivery.DropContactName,
                ContactPhone = delivery.DropContactPhone,
                Instructions = delivery.DropInstructions
            },
            Package = new PackageInfo
            {
                WeightKg = delivery.WeightKg,
                Type = delivery.PackageType,
                Value = delivery.PackageValue,
                Description = delivery.PackageDescription
            },
            Pricing = new DeliveryPricingInfo
            {
                EstimatedPrice = delivery.EstimatedPrice ?? 0,
                FinalPrice = delivery.FinalPrice
            },
            Timeline = events.Select(e => new DeliveryTimelineItem
            {
                Status = e.ToStatus ?? e.EventType,
                Timestamp = e.Timestamp,
                DPId = e.ActorType == "DP" ? e.ActorId : null,
                Description = e.EventType
            }).ToList(),
            TrackingUrl = $"https://deliverx.com/track/{delivery.Id}"
        };
    }

    public async Task<DeliveryListResponse> GetDeliveriesAsync(
        Guid? requesterId,
        Guid? dpId,
        DeliveryListRequest request,
        CancellationToken ct = default)
    {
        var query = _context.Deliveries.AsQueryable();

        if (requesterId.HasValue)
            query = query.Where(d => d.RequesterId == requesterId.Value);

        if (dpId.HasValue)
            query = query.Where(d => d.AssignedDPId == dpId.Value);

        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(d => d.Status == request.Status);

        if (request.FromDate.HasValue)
            query = query.Where(d => d.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(d => d.CreatedAt <= request.ToDate.Value);

        var totalCount = await query.CountAsync(ct);

        var deliveries = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DeliveryListItem
            {
                Id = d.Id,
                Status = d.Status,
                PickupAddress = d.PickupAddress,
                DropAddress = d.DropAddress,
                EstimatedPrice = d.EstimatedPrice,
                DistanceKm = d.DistanceKm,
                CreatedAt = d.CreatedAt,
                Priority = d.Priority
            })
            .ToListAsync(ct);

        return new DeliveryListResponse
        {
            Deliveries = deliveries,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<bool> CancelDeliveryAsync(
        Guid deliveryId,
        Guid userId,
        string reason,
        CancellationToken ct = default)
    {
        var delivery = await _context.Deliveries.FindAsync(new object[] { deliveryId }, ct);
        if (delivery == null) return false;

        // Only allow cancellation in certain statuses
        var cancellableStatuses = new[] { "CREATED", "MATCHING", "ASSIGNED" };
        if (!cancellableStatuses.Contains(delivery.Status))
        {
            _logger.LogWarning("Cannot cancel delivery {DeliveryId} in status {Status}",
                deliveryId, delivery.Status);
            return false;
        }

        var previousStatus = delivery.Status;
        delivery.Status = "CANCELLED";
        delivery.CancelledAt = DateTime.UtcNow;
        delivery.CancellationReason = reason;
        delivery.UpdatedAt = DateTime.UtcNow;

        // Log event
        _context.DeliveryEvents.Add(new DeliveryEvent
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            EventType = "CANCELLED",
            FromStatus = previousStatus,
            ToStatus = "CANCELLED",
            ActorId = userId,
            ActorType = delivery.RequesterId == userId ? "REQUESTER" : "ADMIN",
            Metadata = JsonSerializer.Serialize(new { reason }),
            Timestamp = DateTime.UtcNow
        });

        // If DP was assigned, free them up
        if (delivery.AssignedDPId.HasValue)
        {
            var dpAvailability = await _context.DPAvailabilities
                .FirstOrDefaultAsync(a => a.DPId == delivery.AssignedDPId.Value, ct);

            if (dpAvailability != null && dpAvailability.CurrentDeliveryId == deliveryId)
            {
                dpAvailability.Status = "AVAILABLE";
                dpAvailability.CurrentDeliveryId = null;
                dpAvailability.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Delivery {DeliveryId} cancelled by {UserId}", deliveryId, userId);
        return true;
    }

    public async Task<bool> UpdateDeliveryStatusAsync(
        Guid deliveryId,
        string newStatus,
        Guid actorId,
        string actorType,
        string? metadata = null,
        CancellationToken ct = default)
    {
        var delivery = await _context.Deliveries.FindAsync(new object[] { deliveryId }, ct);
        if (delivery == null) return false;

        var previousStatus = delivery.Status;
        delivery.Status = newStatus;
        delivery.UpdatedAt = DateTime.UtcNow;

        _context.DeliveryEvents.Add(new DeliveryEvent
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            EventType = newStatus,
            FromStatus = previousStatus,
            ToStatus = newStatus,
            ActorId = actorId,
            ActorType = actorType,
            Metadata = metadata,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<DeliveryListResponse> GetPendingDeliveriesForDPAsync(
        Guid dpId,
        CancellationToken ct = default)
    {
        // Get deliveries where this DP was matched and hasn't responded yet
        var pendingMatches = await _context.DeliveryMatchingHistories
            .Where(m => m.DPId == dpId && m.ResponseType == null)
            .Select(m => m.DeliveryId)
            .ToListAsync(ct);

        var deliveries = await _context.Deliveries
            .Where(d => pendingMatches.Contains(d.Id) && d.Status == "MATCHING")
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DeliveryListItem
            {
                Id = d.Id,
                Status = d.Status,
                PickupAddress = d.PickupAddress,
                DropAddress = d.DropAddress,
                EstimatedPrice = d.EstimatedPrice,
                DistanceKm = d.DistanceKm,
                CreatedAt = d.CreatedAt,
                Priority = d.Priority
            })
            .ToListAsync(ct);

        return new DeliveryListResponse
        {
            Deliveries = deliveries,
            TotalCount = deliveries.Count,
            Page = 1,
            PageSize = deliveries.Count
        };
    }

    private int EstimateDuration(double distanceKm)
    {
        // Rough estimate: 20 km/h average speed
        return (int)(distanceKm / 20 * 60) + 10; // Add 10 mins for pickup/drop
    }

    private string EstimateTimeString(double distanceKm)
    {
        var minutes = EstimateDuration(distanceKm);
        if (minutes < 15) return "10-15 mins";
        if (minutes < 25) return "15-25 mins";
        if (minutes < 40) return "25-40 mins";
        if (minutes < 60) return "40-60 mins";
        return $"{minutes / 60}+ hours";
    }
}
