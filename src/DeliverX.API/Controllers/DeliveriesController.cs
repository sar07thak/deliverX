using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliverX.Application.DTOs.Delivery;
using DeliverX.Application.DTOs.POD;
using DeliverX.Application.Services;

namespace DeliverX.API.Controllers;

[ApiController]
[Route("api/v1/deliveries")]
[Authorize]
public class DeliveriesController : ControllerBase
{
    private readonly IDeliveryService _deliveryService;
    private readonly IMatchingService _matchingService;
    private readonly IDeliveryStateService _stateService;

    public DeliveriesController(
        IDeliveryService deliveryService,
        IMatchingService matchingService,
        IDeliveryStateService stateService)
    {
        _deliveryService = deliveryService;
        _matchingService = matchingService;
        _stateService = stateService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Create a new delivery order
    /// POST /api/v1/deliveries
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDelivery(
        [FromBody] CreateDeliveryRequest request,
        CancellationToken ct)
    {
        try
        {
            var requesterId = request.RequesterId ?? GetUserId();
            var result = await _deliveryService.CreateDeliveryAsync(request, requesterId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get delivery details
    /// GET /api/v1/deliveries/{deliveryId}
    /// </summary>
    [HttpGet("{deliveryId}")]
    public async Task<IActionResult> GetDelivery(Guid deliveryId, CancellationToken ct)
    {
        var delivery = await _deliveryService.GetDeliveryAsync(deliveryId, ct);
        if (delivery == null)
        {
            return NotFound(new { error = "Delivery not found" });
        }
        return Ok(delivery);
    }

    /// <summary>
    /// Get list of deliveries (for requester or DP)
    /// GET /api/v1/deliveries
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDeliveries(
        [FromQuery] DeliveryListRequest request,
        [FromQuery] string? role,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        Guid? requesterId = null;
        Guid? dpId = null;

        if (role == "dp" || userRole == "DP")
        {
            dpId = userId;
        }
        else
        {
            requesterId = userId;
        }

        var result = await _deliveryService.GetDeliveriesAsync(requesterId, dpId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Trigger matching for a delivery
    /// POST /api/v1/deliveries/{deliveryId}/match
    /// </summary>
    [HttpPost("{deliveryId}/match")]
    public async Task<IActionResult> MatchDelivery(Guid deliveryId, CancellationToken ct)
    {
        try
        {
            var result = await _matchingService.MatchDeliveryAsync(deliveryId, 1, ct);
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
    /// DP accepts a delivery
    /// POST /api/v1/deliveries/{deliveryId}/accept
    /// </summary>
    [HttpPost("{deliveryId}/accept")]
    public async Task<IActionResult> AcceptDelivery(Guid deliveryId, CancellationToken ct)
    {
        var dpId = GetUserId();
        var result = await _matchingService.AcceptDeliveryAsync(deliveryId, dpId, ct);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "ALREADY_ASSIGNED")
            {
                return Conflict(new { code = result.ErrorCode, message = result.Message });
            }
            return BadRequest(new { code = result.ErrorCode, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// DP rejects a delivery
    /// POST /api/v1/deliveries/{deliveryId}/reject
    /// </summary>
    [HttpPost("{deliveryId}/reject")]
    public async Task<IActionResult> RejectDelivery(
        Guid deliveryId,
        [FromBody] RejectDeliveryRequest request,
        CancellationToken ct)
    {
        var dpId = GetUserId();
        await _matchingService.RejectDeliveryAsync(deliveryId, dpId, request, ct);
        return Ok(new { message = "Delivery rejected. You will not receive further notifications for this delivery." });
    }

    /// <summary>
    /// Cancel a delivery
    /// POST /api/v1/deliveries/{deliveryId}/cancel
    /// </summary>
    [HttpPost("{deliveryId}/cancel")]
    public async Task<IActionResult> CancelDelivery(
        Guid deliveryId,
        [FromBody] CancelDeliveryRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var success = await _deliveryService.CancelDeliveryAsync(deliveryId, userId, request.Reason, ct);

        if (!success)
        {
            return BadRequest(new { error = "Cannot cancel this delivery" });
        }

        return Ok(new { message = "Delivery cancelled successfully" });
    }

    /// <summary>
    /// Get pending deliveries for DP (notifications)
    /// GET /api/v1/deliveries/pending
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingDeliveries(CancellationToken ct)
    {
        var dpId = GetUserId();
        var result = await _deliveryService.GetPendingDeliveriesForDPAsync(dpId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Update DP availability status
    /// PUT /api/v1/deliveries/availability
    /// </summary>
    [HttpPut("availability")]
    public async Task<IActionResult> UpdateAvailability(
        [FromBody] UpdateDPAvailabilityRequest request,
        CancellationToken ct)
    {
        var dpId = GetUserId();
        var result = await _matchingService.UpdateDPAvailabilityAsync(dpId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get DP availability status
    /// GET /api/v1/deliveries/availability
    /// </summary>
    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(CancellationToken ct)
    {
        var dpId = GetUserId();
        var result = await _matchingService.GetDPAvailabilityAsync(dpId, ct);

        if (result == null)
        {
            return Ok(new DPAvailabilityDto
            {
                DPId = dpId,
                Status = "OFFLINE",
                UpdatedAt = DateTime.UtcNow
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Update delivery status (for DP during delivery)
    /// PATCH /api/v1/deliveries/{deliveryId}/status
    /// </summary>
    [HttpPatch("{deliveryId}/status")]
    public async Task<IActionResult> UpdateDeliveryStatus(
        Guid deliveryId,
        [FromBody] UpdateStatusRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "DP";

        var success = await _deliveryService.UpdateDeliveryStatusAsync(
            deliveryId, request.Status, userId, userRole, request.Metadata, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to update status" });
        }

        return Ok(new { message = "Status updated", status = request.Status });
    }

    // =====================
    // F-06: State Machine & POD Endpoints
    // =====================

    /// <summary>
    /// Get delivery state info (allowed transitions)
    /// GET /api/v1/deliveries/{deliveryId}/state
    /// </summary>
    [HttpGet("{deliveryId}/state")]
    public async Task<IActionResult> GetDeliveryState(Guid deliveryId, CancellationToken ct)
    {
        var stateInfo = await _stateService.GetStateInfoAsync(deliveryId, ct);
        if (stateInfo == null)
        {
            return NotFound(new { error = "Delivery not found" });
        }
        return Ok(stateInfo);
    }

    /// <summary>
    /// Mark delivery as picked up
    /// POST /api/v1/deliveries/{deliveryId}/pickup
    /// </summary>
    [HttpPost("{deliveryId}/pickup")]
    public async Task<IActionResult> MarkAsPickedUp(
        Guid deliveryId,
        [FromBody] PickupRequest request,
        CancellationToken ct)
    {
        var dpId = GetUserId();
        var result = await _stateService.MarkAsPickedUpAsync(deliveryId, dpId, request, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "UNAUTHORIZED"
                ? Unauthorized(new { code = result.ErrorCode, message = result.Message })
                : BadRequest(new { code = result.ErrorCode, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Mark delivery as in transit
    /// POST /api/v1/deliveries/{deliveryId}/transit
    /// </summary>
    [HttpPost("{deliveryId}/transit")]
    public async Task<IActionResult> MarkAsInTransit(
        Guid deliveryId,
        [FromBody] TransitRequest request,
        CancellationToken ct)
    {
        var dpId = GetUserId();
        var result = await _stateService.MarkAsInTransitAsync(deliveryId, dpId, request, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "UNAUTHORIZED"
                ? Unauthorized(new { code = result.ErrorCode, message = result.Message })
                : BadRequest(new { code = result.ErrorCode, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Mark delivery as delivered with POD
    /// POST /api/v1/deliveries/{deliveryId}/deliver
    /// </summary>
    [HttpPost("{deliveryId}/deliver")]
    public async Task<IActionResult> MarkAsDelivered(
        Guid deliveryId,
        [FromBody] DeliverRequest request,
        CancellationToken ct)
    {
        var dpId = GetUserId();
        var result = await _stateService.MarkAsDeliveredAsync(deliveryId, dpId, request, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "UNAUTHORIZED"
                ? Unauthorized(new { code = result.ErrorCode, message = result.Message })
                : BadRequest(new { code = result.ErrorCode, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Close a delivered order
    /// POST /api/v1/deliveries/{deliveryId}/close
    /// </summary>
    [HttpPost("{deliveryId}/close")]
    public async Task<IActionResult> CloseDelivery(
        Guid deliveryId,
        [FromBody] CloseDeliveryRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _stateService.CloseDeliveryAsync(deliveryId, userId, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { code = result.ErrorCode, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get Proof of Delivery details
    /// GET /api/v1/deliveries/{deliveryId}/pod
    /// </summary>
    [HttpGet("{deliveryId}/pod")]
    public async Task<IActionResult> GetPOD(Guid deliveryId, CancellationToken ct)
    {
        var pod = await _stateService.GetPODAsync(deliveryId, ct);
        if (pod == null)
        {
            return NotFound(new { error = "POD not found for this delivery" });
        }
        return Ok(pod);
    }

    /// <summary>
    /// Send OTP to recipient before delivery
    /// POST /api/v1/deliveries/{deliveryId}/otp/send
    /// </summary>
    [HttpPost("{deliveryId}/otp/send")]
    public async Task<IActionResult> SendDeliveryOTP(Guid deliveryId, CancellationToken ct)
    {
        var result = await _stateService.SendDeliveryOTPAsync(deliveryId, ct);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Message });
        }
        return Ok(result);
    }

    /// <summary>
    /// Verify delivery OTP
    /// POST /api/v1/deliveries/{deliveryId}/otp/verify
    /// </summary>
    [HttpPost("{deliveryId}/otp/verify")]
    public async Task<IActionResult> VerifyDeliveryOTP(
        Guid deliveryId,
        [FromBody] VerifyDeliveryOTPRequest request,
        CancellationToken ct)
    {
        var result = await _stateService.VerifyDeliveryOTPAsync(deliveryId, request, ct);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Message });
        }
        return Ok(result);
    }
}

public class CancelDeliveryRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Metadata { get; set; }
}
