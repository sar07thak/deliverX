using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliverX.Application.DTOs.Referral;
using DeliverX.Application.Services;

namespace DeliverX.API.Controllers;

[ApiController]
[Route("api/v1/referrals")]
[Authorize]
public class ReferralsController : ControllerBase
{
    private readonly IReferralService _referralService;

    public ReferralsController(IReferralService referralService)
    {
        _referralService = referralService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get or create referral code for current user
    /// GET /api/v1/referrals/my-code
    /// </summary>
    [HttpGet("my-code")]
    public async Task<IActionResult> GetMyReferralCode(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _referralService.GetOrCreateReferralCodeAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Apply a referral code
    /// POST /api/v1/referrals/apply
    /// </summary>
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyReferralCode(
        [FromBody] ApplyReferralRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _referralService.ApplyReferralCodeAsync(userId, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { code = result.ErrorCode, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get referral statistics
    /// GET /api/v1/referrals/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetReferralStats(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _referralService.GetReferralStatsAsync(userId, ct);
        return Ok(result);
    }
}

[ApiController]
[Route("api/v1/donations")]
[Authorize]
public class DonationsController : ControllerBase
{
    private readonly IReferralService _referralService;

    public DonationsController(IReferralService referralService)
    {
        _referralService = referralService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get available charities
    /// GET /api/v1/donations/charities
    /// </summary>
    [HttpGet("charities")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCharities(CancellationToken ct)
    {
        var charities = await _referralService.GetCharitiesAsync(ct);
        return Ok(charities);
    }

    /// <summary>
    /// Make a donation
    /// POST /api/v1/donations
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> MakeDonation(
        [FromBody] MakeDonationRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _referralService.MakeDonationAsync(userId, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { code = result.ErrorCode, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get donation statistics
    /// GET /api/v1/donations/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetDonationStats(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _referralService.GetDonationStatsAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get donation preferences
    /// GET /api/v1/donations/preferences
    /// </summary>
    [HttpGet("preferences")]
    public async Task<IActionResult> GetDonationPreferences(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _referralService.GetDonationPreferenceAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Update donation preferences
    /// PUT /api/v1/donations/preferences
    /// </summary>
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdateDonationPreferences(
        [FromBody] UpdateDonationPreferenceRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var success = await _referralService.UpdateDonationPreferenceAsync(userId, request, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to update preferences" });
        }

        return Ok(new { message = "Preferences updated successfully" });
    }
}
