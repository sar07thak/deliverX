using System;
using System.Threading;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs.ServiceArea;

namespace DeliveryDost.Application.Services;

/// <summary>
/// Service for managing delivery partner service areas and geospatial matching
/// </summary>
public interface IServiceAreaService
{
    /// <summary>
    /// Create or update a service area for a user (DP or DPCM)
    /// </summary>
    Task<SetServiceAreaResponse> SetServiceAreaAsync(
        Guid userId,
        SetServiceAreaRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Find eligible delivery partners whose service area covers both pickup and drop points
    /// </summary>
    Task<FindEligibleDPsResponse> FindEligibleDPsAsync(
        FindEligibleDPsRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get service area details for a specific user
    /// </summary>
    Task<GetServiceAreaResponse> GetServiceAreaAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a point is covered by a DP's service area
    /// </summary>
    Task<CheckCoverageResponse> CheckCoverageAsync(
        CheckCoverageRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Deactivate a service area
    /// </summary>
    Task<bool> DeactivateServiceAreaAsync(
        Guid userId,
        Guid serviceAreaId,
        CancellationToken ct = default);

    /// <summary>
    /// Calculate distance between two points using Haversine formula
    /// </summary>
    double CalculateDistanceKm(
        double lat1, double lng1,
        double lat2, double lng2);
}
