using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliverX.Application.DTOs.Pricing;
using DeliverX.Application.Services;

namespace DeliverX.API.Controllers;

[ApiController]
[Route("api/v1/pricing")]
public class PricingController : ControllerBase
{
    private readonly IPricingService _pricingService;

    public PricingController(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    /// <summary>
    /// Calculate delivery pricing preview
    /// POST /api/v1/pricing/calculate
    /// </summary>
    [HttpPost("calculate")]
    [Authorize]
    public async Task<IActionResult> CalculatePricing(
        [FromBody] CalculatePricingRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _pricingService.CalculatePricingAsync(request, ct);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get DP pricing configuration
    /// GET /api/v1/pricing/dp/{dpId}
    /// </summary>
    [HttpGet("dp/{dpId}")]
    [Authorize]
    public async Task<IActionResult> GetDPPricing(
        Guid dpId,
        CancellationToken ct)
    {
        var config = await _pricingService.GetDPPricingConfigAsync(dpId, ct);

        if (config == null)
        {
            return NotFound(new { error = "No pricing configuration found for this DP" });
        }

        return Ok(config);
    }

    /// <summary>
    /// Update DP pricing configuration
    /// PATCH /api/v1/pricing/dp/{dpId}
    /// </summary>
    [HttpPatch("dp/{dpId}")]
    [Authorize]
    public async Task<IActionResult> UpdateDPPricing(
        Guid dpId,
        [FromBody] UpdateDPPricingRequest request,
        CancellationToken ct)
    {
        // Verify the user is the DP or admin
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role != "Admin" && userId != dpId.ToString())
        {
            return Forbid();
        }

        try
        {
            var result = await _pricingService.UpdateDPPricingAsync(dpId, request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update platform fee configuration (Admin only)
    /// PATCH /api/v1/pricing/platform/fees
    /// </summary>
    [HttpPatch("platform/fees")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePlatformFees(
        [FromBody] UpdatePlatformFeesRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _pricingService.UpdatePlatformFeesAsync(request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Calculate commission breakdown for a delivery amount
    /// POST /api/v1/pricing/commission
    /// </summary>
    [HttpPost("commission")]
    [Authorize]
    public async Task<IActionResult> CalculateCommission(
        [FromBody] CalculateCommissionRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _pricingService.CalculateCommissionAsync(
                request.DPId,
                request.TotalAmount,
                ct);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Initialize default pricing for a DP
    /// POST /api/v1/pricing/dp/{dpId}/initialize
    /// </summary>
    [HttpPost("dp/{dpId}/initialize")]
    [Authorize]
    public async Task<IActionResult> InitializePricing(
        Guid dpId,
        CancellationToken ct)
    {
        try
        {
            await _pricingService.InitializeDefaultPricingAsync(dpId, ct);
            return Ok(new { message = "Default pricing initialized successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class CalculateCommissionRequest
{
    public Guid DPId { get; set; }
    public decimal TotalAmount { get; set; }
}
