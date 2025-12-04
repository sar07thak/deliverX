using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliverX.Application.DTOs.Rating;
using DeliverX.Application.Services;

namespace DeliverX.API.Controllers;

[ApiController]
[Route("api/v1/ratings")]
[Authorize]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingsController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private string GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? "EC";
    }

    /// <summary>
    /// Submit a rating after delivery
    /// POST /api/v1/ratings
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRating(
        [FromBody] CreateRatingRequest request,
        CancellationToken ct)
    {
        var raterId = GetUserId();
        var raterType = GetUserRole();

        var result = await _ratingService.CreateRatingAsync(raterId, raterType, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { code = result.ErrorCode, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get rating summary for a user
    /// GET /api/v1/ratings/summary/{targetId}
    /// </summary>
    [HttpGet("summary/{targetId}")]
    public async Task<IActionResult> GetRatingSummary(Guid targetId, CancellationToken ct)
    {
        var summary = await _ratingService.GetRatingSummaryAsync(targetId, ct);
        return Ok(summary);
    }

    /// <summary>
    /// Get ratings list for a user (paginated)
    /// GET /api/v1/ratings/{targetId}
    /// </summary>
    [HttpGet("{targetId}")]
    public async Task<IActionResult> GetRatings(
        Guid targetId,
        [FromQuery] string? targetType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var request = new GetRatingsRequest
        {
            TargetId = targetId,
            TargetType = targetType,
            Page = page,
            PageSize = pageSize
        };

        var result = await _ratingService.GetRatingsAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get behavior index for current user
    /// GET /api/v1/ratings/behavior-index
    /// </summary>
    [HttpGet("behavior-index")]
    public async Task<IActionResult> GetMyBehaviorIndex(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _ratingService.GetBehaviorIndexAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get behavior index for a specific user
    /// GET /api/v1/ratings/behavior-index/{userId}
    /// </summary>
    [HttpGet("behavior-index/{userId}")]
    public async Task<IActionResult> GetBehaviorIndex(Guid userId, CancellationToken ct)
    {
        var result = await _ratingService.GetBehaviorIndexAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Check if user has already rated a delivery
    /// GET /api/v1/ratings/check/{deliveryId}/{targetId}
    /// </summary>
    [HttpGet("check/{deliveryId}/{targetId}")]
    public async Task<IActionResult> CheckRating(Guid deliveryId, Guid targetId, CancellationToken ct)
    {
        var raterId = GetUserId();
        var hasRated = await _ratingService.HasRatedDeliveryAsync(raterId, deliveryId, targetId, ct);
        return Ok(new { hasRated });
    }

    /// <summary>
    /// Recalculate behavior index for a user (admin only)
    /// POST /api/v1/ratings/behavior-index/{userId}/recalculate
    /// </summary>
    [HttpPost("behavior-index/{userId}/recalculate")]
    [Authorize(Roles = "SA,DPCM")]
    public async Task<IActionResult> RecalculateBehaviorIndex(Guid userId, CancellationToken ct)
    {
        var result = await _ratingService.RecalculateBehaviorIndexAsync(userId, ct);
        return Ok(result);
    }
}
