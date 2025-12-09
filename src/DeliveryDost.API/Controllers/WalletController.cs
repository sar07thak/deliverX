using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryDost.Application.DTOs.Wallet;
using DeliveryDost.Application.Services;

namespace DeliveryDost.API.Controllers;

[ApiController]
[Route("api/v1/wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
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
    /// Get current user's wallet
    /// GET /api/v1/wallet
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWallet(CancellationToken ct)
    {
        var userId = GetUserId();
        var wallet = await _walletService.GetOrCreateWalletAsync(userId, GetUserRole(), ct);
        return Ok(wallet);
    }

    /// <summary>
    /// Get wallet transactions
    /// GET /api/v1/wallet/transactions
    /// </summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] GetTransactionsRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _walletService.GetTransactionsAsync(userId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Recharge wallet
    /// POST /api/v1/wallet/recharge
    /// </summary>
    [HttpPost("recharge")]
    public async Task<IActionResult> RechargeWallet(
        [FromBody] RechargeWalletRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _walletService.InitiateRechargeAsync(userId, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { code = result.ErrorCode, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Confirm payment (webhook or client callback)
    /// POST /api/v1/wallet/confirm-payment
    /// </summary>
    [HttpPost("confirm-payment")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmPayment(
        [FromBody] ConfirmPaymentRequest request,
        CancellationToken ct)
    {
        var success = await _walletService.ConfirmPaymentAsync(request, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to confirm payment" });
        }

        return Ok(new { message = "Payment confirmed" });
    }

    /// <summary>
    /// Get earnings summary (for DPs)
    /// GET /api/v1/wallet/earnings
    /// </summary>
    [HttpGet("earnings")]
    public async Task<IActionResult> GetEarnings(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _walletService.GetEarningsSummaryAsync(userId, ct);
        return Ok(result);
    }
}

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IWalletService _walletService;

    public PaymentsController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Initiate delivery payment
    /// POST /api/v1/payments/delivery
    /// </summary>
    [HttpPost("delivery")]
    public async Task<IActionResult> InitiateDeliveryPayment(
        [FromBody] InitiateDeliveryPaymentRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _walletService.InitiateDeliveryPaymentAsync(userId, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { code = result.ErrorCode, message = result.Message });
        }

        return Ok(result);
    }
}

[ApiController]
[Route("api/v1/settlements")]
[Authorize]
public class SettlementsController : ControllerBase
{
    private readonly IWalletService _walletService;

    public SettlementsController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get settlements for current user
    /// GET /api/v1/settlements
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSettlements(
        [FromQuery] GetSettlementsRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _walletService.GetSettlementsAsync(userId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get settlement details
    /// GET /api/v1/settlements/{settlementId}
    /// </summary>
    [HttpGet("{settlementId}")]
    public async Task<IActionResult> GetSettlement(Guid settlementId, CancellationToken ct)
    {
        var result = await _walletService.GetSettlementDetailAsync(settlementId, ct);

        if (result == null)
        {
            return NotFound(new { error = "Settlement not found" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Create settlement (admin)
    /// POST /api/v1/settlements
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SA,DPCM")]
    public async Task<IActionResult> CreateSettlement(
        [FromBody] CreateSettlementRequest request,
        CancellationToken ct)
    {
        var result = await _walletService.CreateSettlementAsync(request, ct);

        if (result == null)
        {
            return BadRequest(new { error = "No pending earnings to settle" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Process settlement (admin)
    /// POST /api/v1/settlements/{settlementId}/process
    /// </summary>
    [HttpPost("{settlementId}/process")]
    [Authorize(Roles = "SA")]
    public async Task<IActionResult> ProcessSettlement(Guid settlementId, CancellationToken ct)
    {
        var success = await _walletService.ProcessSettlementAsync(settlementId, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to process settlement" });
        }

        return Ok(new { message = "Settlement processed successfully" });
    }
}
