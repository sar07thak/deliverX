using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DeliverX.Application.DTOs.ServiceArea;
using DeliverX.Application.Services;

namespace DeliverX.API.Controllers;

/// <summary>
/// Controller for managing delivery partner service areas and geospatial matching
/// </summary>
[ApiController]
[Route("api/v1/service-area")]
public class ServiceAreaController : ControllerBase
{
    private readonly IServiceAreaService _serviceAreaService;
    private readonly ILogger<ServiceAreaController> _logger;

    public ServiceAreaController(
        IServiceAreaService serviceAreaService,
        ILogger<ServiceAreaController> logger)
    {
        _serviceAreaService = serviceAreaService;
        _logger = logger;
    }

    /// <summary>
    /// Create or update a service area for the authenticated user (DP or DPCM)
    /// </summary>
    /// <remarks>
    /// Circle-based service area with center point and radius (1-50 km).
    /// Only one active service area per user is allowed in MVP.
    /// </remarks>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<SetServiceAreaResponse>> SetServiceArea(
        [FromBody] SetServiceAreaRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var response = await _serviceAreaService.SetServiceAreaAsync(userId.Value, request, ct);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error setting service area");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting service area");
            return StatusCode(500, new { error = "An error occurred while setting the service area" });
        }
    }

    /// <summary>
    /// Find eligible delivery partners whose service area covers both pickup and drop points
    /// </summary>
    /// <remarks>
    /// Returns DPs sorted by distance from pickup location.
    /// Both pickup and drop must be within the DP's service area (unless AllowDropOutsideArea is enabled).
    /// </remarks>
    [HttpPost("match")]
    [Authorize]
    public async Task<ActionResult<FindEligibleDPsResponse>> FindEligibleDPs(
        [FromBody] FindEligibleDPsRequest request,
        CancellationToken ct)
    {
        try
        {
            var response = await _serviceAreaService.FindEligibleDPsAsync(request, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding eligible DPs");
            return StatusCode(500, new { error = "An error occurred while finding eligible DPs" });
        }
    }

    /// <summary>
    /// Get service area details for a specific user
    /// </summary>
    /// <param name="dpId">The delivery partner's user ID (optional - defaults to current user)</param>
    [HttpGet("{dpId?}")]
    [Authorize]
    public async Task<ActionResult<GetServiceAreaResponse>> GetServiceArea(
        Guid? dpId,
        CancellationToken ct)
    {
        try
        {
            var userId = dpId ?? GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var response = await _serviceAreaService.GetServiceAreaAsync(userId.Value, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service area for user {UserId}", dpId);
            return StatusCode(500, new { error = "An error occurred while getting the service area" });
        }
    }

    /// <summary>
    /// Check if a specific point is covered by a DP's service area
    /// </summary>
    [HttpPost("check-coverage")]
    [Authorize]
    public async Task<ActionResult<CheckCoverageResponse>> CheckCoverage(
        [FromBody] CheckCoverageRequest request,
        CancellationToken ct)
    {
        try
        {
            var response = await _serviceAreaService.CheckCoverageAsync(request, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking coverage for DP {DPId}", request.DPId);
            return StatusCode(500, new { error = "An error occurred while checking coverage" });
        }
    }

    /// <summary>
    /// Deactivate a service area
    /// </summary>
    [HttpDelete("{serviceAreaId}")]
    [Authorize]
    public async Task<ActionResult> DeactivateServiceArea(
        Guid serviceAreaId,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var success = await _serviceAreaService.DeactivateServiceAreaAsync(userId.Value, serviceAreaId, ct);
            if (!success)
            {
                return NotFound(new { error = "Service area not found" });
            }

            return Ok(new { message = "Service area deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating service area {ServiceAreaId}", serviceAreaId);
            return StatusCode(500, new { error = "An error occurred while deactivating the service area" });
        }
    }

    /// <summary>
    /// Calculate distance between two points (utility endpoint)
    /// </summary>
    [HttpGet("distance")]
    public ActionResult<object> CalculateDistance(
        [FromQuery] decimal lat1,
        [FromQuery] decimal lng1,
        [FromQuery] decimal lat2,
        [FromQuery] decimal lng2)
    {
        var distance = _serviceAreaService.CalculateDistanceKm(
            (double)lat1, (double)lng1,
            (double)lat2, (double)lng2);

        return Ok(new
        {
            from = new { lat = lat1, lng = lng1 },
            to = new { lat = lat2, lng = lng2 },
            distanceKm = Math.Round(distance, 2),
            distanceM = Math.Round(distance * 1000, 0)
        });
    }

    private Guid? GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}
