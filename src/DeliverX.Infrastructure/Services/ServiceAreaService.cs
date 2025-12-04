using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliverX.Application.DTOs.ServiceArea;
using DeliverX.Application.Services;
using DeliverX.Domain.Entities;
using DeliverX.Infrastructure.Data;

namespace DeliverX.Infrastructure.Services;

/// <summary>
/// Service for managing service areas with Haversine-based geospatial matching
/// </summary>
public class ServiceAreaService : IServiceAreaService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ServiceAreaService> _logger;
    private const double EarthRadiusKm = 6371.0;

    public ServiceAreaService(
        ApplicationDbContext context,
        ILogger<ServiceAreaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SetServiceAreaResponse> SetServiceAreaAsync(
        Guid userId,
        SetServiceAreaRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Setting service area for user {UserId}", userId);

        // Validate input
        if (request.RadiusKm < 1 || request.RadiusKm > 50)
        {
            throw new ArgumentException("Radius must be between 1 and 50 km");
        }

        if (request.CenterLat < -90 || request.CenterLat > 90)
        {
            throw new ArgumentException("Latitude must be between -90 and 90");
        }

        if (request.CenterLng < -180 || request.CenterLng > 180)
        {
            throw new ArgumentException("Longitude must be between -180 and 180");
        }

        // Check if user exists
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null)
        {
            throw new ArgumentException("User not found");
        }

        // Check if service area already exists for this user
        var existingArea = await _context.ServiceAreas
            .FirstOrDefaultAsync(sa => sa.UserId == userId && sa.IsActive, ct);

        ServiceArea serviceArea;

        if (existingArea != null)
        {
            // Update existing
            existingArea.CenterLat = request.CenterLat;
            existingArea.CenterLng = request.CenterLng;
            existingArea.RadiusKm = request.RadiusKm;
            existingArea.AreaName = request.AreaName;
            existingArea.AllowDropOutsideArea = request.AllowDropOutsideArea;
            existingArea.UpdatedAt = DateTime.UtcNow;
            serviceArea = existingArea;

            _logger.LogInformation("Updated existing service area {ServiceAreaId}", existingArea.Id);
        }
        else
        {
            // Create new
            serviceArea = new ServiceArea
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserRole = user.Role,
                AreaType = "CIRCLE",
                CenterLat = request.CenterLat,
                CenterLng = request.CenterLng,
                RadiusKm = request.RadiusKm,
                AreaName = request.AreaName,
                AllowDropOutsideArea = request.AllowDropOutsideArea,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ServiceAreas.Add(serviceArea);

            _logger.LogInformation("Created new service area {ServiceAreaId}", serviceArea.Id);
        }

        await _context.SaveChangesAsync(ct);

        // Calculate estimated coverage area (π * r²)
        var coverageAreaSqKm = Math.PI * (double)request.RadiusKm * (double)request.RadiusKm;

        return new SetServiceAreaResponse
        {
            ServiceAreaId = serviceArea.Id,
            Coverage = new ServiceAreaCoverage
            {
                Type = "CIRCLE",
                CenterLat = serviceArea.CenterLat,
                CenterLng = serviceArea.CenterLng,
                RadiusKm = serviceArea.RadiusKm,
                AreaName = serviceArea.AreaName
            },
            EstimatedCoverage = $"{coverageAreaSqKm:F1} sq km",
            Message = existingArea != null
                ? "Service area updated successfully"
                : "Service area created successfully"
        };
    }

    public async Task<FindEligibleDPsResponse> FindEligibleDPsAsync(
        FindEligibleDPsRequest request,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Finding eligible DPs for pickup ({PickupLat}, {PickupLng}) -> drop ({DropLat}, {DropLng})",
            request.PickupLat, request.PickupLng, request.DropLat, request.DropLng);

        // Get all active service areas for DP users only
        var activeAreas = await _context.ServiceAreas
            .Include(sa => sa.User)
            .Where(sa => sa.IsActive && sa.AreaType == "CIRCLE" && sa.User != null && sa.User.Role == "DP")
            .ToListAsync(ct);

        // Get DP profiles for pricing info
        var dpProfiles = await _context.DeliveryPartnerProfiles
            .Where(dp => dp.IsActive)
            .ToDictionaryAsync(dp => dp.UserId, ct);

        var matchedDPs = new List<MatchedDP>();

        foreach (var area in activeAreas)
        {
            var pickupDistance = CalculateDistanceKm(
                (double)area.CenterLat, (double)area.CenterLng,
                (double)request.PickupLat, (double)request.PickupLng);

            var dropDistance = CalculateDistanceKm(
                (double)area.CenterLat, (double)area.CenterLng,
                (double)request.DropLat, (double)request.DropLng);

            var radiusKm = (double)area.RadiusKm;

            // Check if both pickup and drop are within the service area
            bool pickupInArea = pickupDistance <= radiusKm;
            bool dropInArea = dropDistance <= radiusKm;

            string coverageType;
            bool isEligible = false;

            if (pickupInArea && dropInArea)
            {
                coverageType = "BOTH_ENDS";
                isEligible = true;
            }
            else if (pickupInArea && area.AllowDropOutsideArea)
            {
                coverageType = "PICKUP_ONLY";
                isEligible = true;
            }
            else
            {
                continue; // Not eligible
            }

            if (isEligible && area.User != null)
            {
                // Get pricing info and name from profile if available
                DPPricingInfo? pricing = null;
                string dpName = "Unknown";

                if (dpProfiles.TryGetValue(area.UserId, out var profile))
                {
                    dpName = profile.FullName;
                    pricing = new DPPricingInfo
                    {
                        PerKmRate = profile.PerKmRate ?? 0,
                        PerKgRate = profile.PerKgRate ?? 0,
                        MinCharge = profile.MinCharge ?? 0
                    };
                }

                matchedDPs.Add(new MatchedDP
                {
                    DPId = area.UserId,
                    DPName = dpName,
                    Phone = area.User.Phone,
                    ServiceAreaId = area.Id,
                    DistanceFromPickupKm = (decimal)pickupDistance,
                    DistanceFromDropKm = (decimal)dropDistance,
                    CoverageType = coverageType,
                    Pricing = pricing
                });
            }
        }

        // Sort by distance from pickup (closest first)
        matchedDPs = matchedDPs
            .OrderBy(dp => dp.DistanceFromPickupKm)
            .Take(request.MaxResults)
            .ToList();

        stopwatch.Stop();

        _logger.LogInformation(
            "Found {Count} eligible DPs in {ElapsedMs}ms",
            matchedDPs.Count, stopwatch.ElapsedMilliseconds);

        return new FindEligibleDPsResponse
        {
            MatchedDPs = matchedDPs,
            TotalMatches = matchedDPs.Count,
            QueryTimeMs = stopwatch.ElapsedMilliseconds
        };
    }

    public async Task<GetServiceAreaResponse> GetServiceAreaAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var serviceAreas = await _context.ServiceAreas
            .Where(sa => sa.UserId == userId)
            .OrderByDescending(sa => sa.IsActive)
            .ThenByDescending(sa => sa.UpdatedAt)
            .ToListAsync(ct);

        var response = new GetServiceAreaResponse
        {
            ServiceAreas = serviceAreas.Select(sa => new ServiceAreaDetail
            {
                Id = sa.Id,
                Type = sa.AreaType,
                CenterLat = sa.CenterLat,
                CenterLng = sa.CenterLng,
                RadiusKm = sa.RadiusKm,
                AreaName = sa.AreaName,
                IsActive = sa.IsActive,
                AllowDropOutsideArea = sa.AllowDropOutsideArea,
                EstimatedCoverage = $"{Math.PI * (double)sa.RadiusKm * (double)sa.RadiusKm:F1} sq km",
                CreatedAt = sa.CreatedAt,
                UpdatedAt = sa.UpdatedAt
            }).ToList()
        };

        return response;
    }

    public async Task<CheckCoverageResponse> CheckCoverageAsync(
        CheckCoverageRequest request,
        CancellationToken ct = default)
    {
        var serviceArea = await _context.ServiceAreas
            .FirstOrDefaultAsync(sa => sa.UserId == request.DPId && sa.IsActive, ct);

        if (serviceArea == null)
        {
            return new CheckCoverageResponse
            {
                IsCovered = false,
                ServiceAreaId = null,
                DistanceFromCenterKm = null,
                AreaName = null
            };
        }

        var distance = CalculateDistanceKm(
            (double)serviceArea.CenterLat, (double)serviceArea.CenterLng,
            (double)request.PointLat, (double)request.PointLng);

        var isCovered = distance <= (double)serviceArea.RadiusKm;

        return new CheckCoverageResponse
        {
            IsCovered = isCovered,
            ServiceAreaId = serviceArea.Id,
            DistanceFromCenterKm = (decimal)distance,
            AreaName = serviceArea.AreaName
        };
    }

    public async Task<bool> DeactivateServiceAreaAsync(
        Guid userId,
        Guid serviceAreaId,
        CancellationToken ct = default)
    {
        var serviceArea = await _context.ServiceAreas
            .FirstOrDefaultAsync(sa => sa.Id == serviceAreaId && sa.UserId == userId, ct);

        if (serviceArea == null)
        {
            return false;
        }

        serviceArea.IsActive = false;
        serviceArea.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Deactivated service area {ServiceAreaId}", serviceAreaId);
        return true;
    }

    /// <summary>
    /// Calculate distance between two points using Haversine formula
    /// </summary>
    public double CalculateDistanceKm(
        double lat1, double lng1,
        double lat2, double lng2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
