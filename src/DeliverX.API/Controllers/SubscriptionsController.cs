using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliverX.Application.DTOs.Subscription;
using DeliverX.Application.Services;

namespace DeliverX.API.Controllers;

[ApiController]
[Route("api/v1/subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get available subscription plans
    /// GET /api/v1/subscriptions/plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans([FromQuery] string? planType, CancellationToken ct)
    {
        var plans = await _subscriptionService.GetPlansAsync(planType, ct);
        return Ok(plans);
    }

    /// <summary>
    /// Get plan details
    /// GET /api/v1/subscriptions/plans/{planId}
    /// </summary>
    [HttpGet("plans/{planId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlan(Guid planId, CancellationToken ct)
    {
        var plan = await _subscriptionService.GetPlanAsync(planId, ct);
        if (plan == null)
        {
            return NotFound(new { error = "Plan not found" });
        }
        return Ok(plan);
    }

    /// <summary>
    /// Get current user's active subscription
    /// GET /api/v1/subscriptions/my
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMySubscription(CancellationToken ct)
    {
        var userId = GetUserId();
        var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId, ct);
        if (subscription == null)
        {
            return Ok(new { message = "No active subscription" });
        }
        return Ok(subscription);
    }

    /// <summary>
    /// Subscribe to a plan
    /// POST /api/v1/subscriptions
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Subscribe(
        [FromBody] SubscribeRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _subscriptionService.SubscribeAsync(userId, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { code = result.ErrorCode, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Cancel subscription
    /// POST /api/v1/subscriptions/cancel
    /// </summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelSubscription(
        [FromBody] CancelSubscriptionRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var success = await _subscriptionService.CancelSubscriptionAsync(userId, request, ct);

        if (!success)
        {
            return BadRequest(new { error = "No active subscription to cancel" });
        }

        return Ok(new { message = "Subscription cancelled successfully" });
    }

    /// <summary>
    /// Toggle auto-renewal
    /// PUT /api/v1/subscriptions/auto-renew
    /// </summary>
    [HttpPut("auto-renew")]
    public async Task<IActionResult> ToggleAutoRenew(
        [FromBody] ToggleAutoRenewRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var success = await _subscriptionService.ToggleAutoRenewAsync(userId, request.AutoRenew, ct);

        if (!success)
        {
            return BadRequest(new { error = "No active subscription" });
        }

        return Ok(new { message = $"Auto-renewal {(request.AutoRenew ? "enabled" : "disabled")}" });
    }

    /// <summary>
    /// Get invoices
    /// GET /api/v1/subscriptions/invoices
    /// </summary>
    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] GetInvoicesRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _subscriptionService.GetInvoicesAsync(userId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get invoice details
    /// GET /api/v1/subscriptions/invoices/{invoiceId}
    /// </summary>
    [HttpGet("invoices/{invoiceId}")]
    public async Task<IActionResult> GetInvoice(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await _subscriptionService.GetInvoiceAsync(invoiceId, ct);
        if (invoice == null)
        {
            return NotFound(new { error = "Invoice not found" });
        }
        return Ok(invoice);
    }

    /// <summary>
    /// Validate promo code
    /// POST /api/v1/subscriptions/promo/validate
    /// </summary>
    [HttpPost("promo/validate")]
    public async Task<IActionResult> ValidatePromoCode(
        [FromBody] ValidatePromoCodeRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _subscriptionService.ValidatePromoCodeAsync(userId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get promo codes (admin)
    /// GET /api/v1/subscriptions/promo
    /// </summary>
    [HttpGet("promo")]
    [Authorize(Roles = "SA")]
    public async Task<IActionResult> GetPromoCodes(CancellationToken ct)
    {
        var promoCodes = await _subscriptionService.GetPromoCodesAsync(ct);
        return Ok(promoCodes);
    }

    /// <summary>
    /// Create promo code (admin)
    /// POST /api/v1/subscriptions/promo
    /// </summary>
    [HttpPost("promo")]
    [Authorize(Roles = "SA")]
    public async Task<IActionResult> CreatePromoCode(
        [FromBody] CreatePromoCodeRequest request,
        CancellationToken ct)
    {
        var result = await _subscriptionService.CreatePromoCodeAsync(request, ct);

        if (result == null)
        {
            return BadRequest(new { error = "Promo code already exists" });
        }

        return Ok(result);
    }
}

public class ToggleAutoRenewRequest
{
    public bool AutoRenew { get; set; }
}
