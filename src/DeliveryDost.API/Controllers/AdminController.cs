using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryDost.Application.DTOs.Dashboard;
using DeliveryDost.Application.Services;

namespace DeliveryDost.API.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "SuperAdmin,SA")]
public class AdminController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public AdminController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get admin dashboard
    /// GET /api/v1/admin/dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var dashboard = await _dashboardService.GetAdminDashboardAsync(ct);
        return Ok(dashboard);
    }

    /// <summary>
    /// Get platform statistics
    /// GET /api/v1/admin/stats/platform
    /// </summary>
    [HttpGet("stats/platform")]
    public async Task<IActionResult> GetPlatformStats(CancellationToken ct)
    {
        var stats = await _dashboardService.GetPlatformStatsAsync(ct);
        return Ok(stats);
    }

    /// <summary>
    /// Get revenue statistics
    /// GET /api/v1/admin/stats/revenue
    /// </summary>
    [HttpGet("stats/revenue")]
    public async Task<IActionResult> GetRevenueStats(CancellationToken ct)
    {
        var stats = await _dashboardService.GetRevenueStatsAsync(ct);
        return Ok(stats);
    }

    /// <summary>
    /// Generate report
    /// POST /api/v1/admin/reports
    /// </summary>
    [HttpPost("reports")]
    public async Task<IActionResult> GenerateReport(
        [FromBody] ReportRequest request,
        CancellationToken ct)
    {
        var report = await _dashboardService.GenerateReportAsync(request, ct);
        return Ok(report);
    }

    /// <summary>
    /// Get users list
    /// GET /api/v1/admin/users
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] UserListRequest request,
        CancellationToken ct)
    {
        var users = await _dashboardService.GetUsersAsync(request, ct);
        return Ok(users);
    }

    /// <summary>
    /// Get user details
    /// GET /api/v1/admin/users/{userId}
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken ct)
    {
        var user = await _dashboardService.GetUserAsync(userId, ct);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }
        return Ok(user);
    }

    /// <summary>
    /// Update user status
    /// PUT /api/v1/admin/users/{userId}/status
    /// </summary>
    [HttpPut("users/{userId}/status")]
    public async Task<IActionResult> UpdateUserStatus(
        Guid userId,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken ct)
    {
        var adminId = GetUserId();
        var success = await _dashboardService.UpdateUserStatusAsync(userId, request, adminId, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to update user status" });
        }

        return Ok(new { message = "User status updated successfully" });
    }

    /// <summary>
    /// Get KYC requests
    /// GET /api/v1/admin/kyc
    /// </summary>
    [HttpGet("kyc")]
    public async Task<IActionResult> GetKYCRequests(
        [FromQuery] KYCListRequest request,
        CancellationToken ct)
    {
        var kycs = await _dashboardService.GetKYCRequestsAsync(request, ct);
        return Ok(kycs);
    }

    /// <summary>
    /// Approve KYC
    /// POST /api/v1/admin/kyc/{kycId}/approve
    /// </summary>
    [HttpPost("kyc/{kycId}/approve")]
    public async Task<IActionResult> ApproveKYC(
        Guid kycId,
        [FromBody] ApproveKYCRequest request,
        CancellationToken ct)
    {
        var adminId = GetUserId();
        var success = await _dashboardService.ApproveKYCAsync(kycId, request, adminId, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to approve KYC" });
        }

        return Ok(new { message = "KYC approved successfully" });
    }

    /// <summary>
    /// Reject KYC
    /// POST /api/v1/admin/kyc/{kycId}/reject
    /// </summary>
    [HttpPost("kyc/{kycId}/reject")]
    public async Task<IActionResult> RejectKYC(
        Guid kycId,
        [FromBody] RejectKYCRequest request,
        CancellationToken ct)
    {
        var adminId = GetUserId();
        var success = await _dashboardService.RejectKYCAsync(kycId, request, adminId, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to reject KYC" });
        }

        return Ok(new { message = "KYC rejected" });
    }

    /// <summary>
    /// Get audit logs
    /// GET /api/v1/admin/audit-logs
    /// </summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] AuditLogRequest request,
        CancellationToken ct)
    {
        var logs = await _dashboardService.GetAuditLogsAsync(request, ct);
        return Ok(logs);
    }

    /// <summary>
    /// Get system configuration
    /// GET /api/v1/admin/config
    /// </summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetSystemConfig(CancellationToken ct)
    {
        var config = await _dashboardService.GetSystemConfigAsync(ct);
        return Ok(config);
    }

    /// <summary>
    /// Update system configuration
    /// PUT /api/v1/admin/config
    /// </summary>
    [HttpPut("config")]
    public async Task<IActionResult> UpdateSystemConfig(
        [FromBody] UpdateConfigRequest request,
        CancellationToken ct)
    {
        var adminId = GetUserId();
        var success = await _dashboardService.UpdateSystemConfigAsync(request, adminId, ct);

        if (!success)
        {
            return BadRequest(new { error = "Configuration key not found" });
        }

        return Ok(new { message = "Configuration updated successfully" });
    }
}

[ApiController]
[Route("api/v1/dpcm")]
[Authorize(Roles = "DPCM")]
public class DPCMController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DPCMController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get DPCM dashboard
    /// GET /api/v1/dpcm/dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var dpcmId = GetUserId();
        var dashboard = await _dashboardService.GetDPCMDashboardAsync(dpcmId, ct);
        return Ok(dashboard);
    }

    /// <summary>
    /// Get managed delivery partners
    /// GET /api/v1/dpcm/partners
    /// </summary>
    [HttpGet("partners")]
    public async Task<IActionResult> GetPartners(
        [FromQuery] DPCMPartnersRequest request,
        CancellationToken ct)
    {
        var dpcmId = GetUserId();
        var partners = await _dashboardService.GetDPCMPartnersAsync(dpcmId, request, ct);
        return Ok(partners);
    }

    /// <summary>
    /// Update DP status (activate/deactivate)
    /// PUT /api/v1/dpcm/partners/{dpId}/status
    /// </summary>
    [HttpPut("partners/{dpId}/status")]
    public async Task<IActionResult> UpdatePartnerStatus(
        Guid dpId,
        [FromBody] UpdateDPStatusRequest request,
        CancellationToken ct)
    {
        var dpcmId = GetUserId();
        var success = await _dashboardService.UpdateDPStatusByDPCMAsync(dpcmId, dpId, request.IsActive, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to update partner status. Partner may not belong to you." });
        }

        return Ok(new { message = "Partner status updated successfully" });
    }

    /// <summary>
    /// Get deliveries by managed DPs
    /// GET /api/v1/dpcm/deliveries
    /// </summary>
    [HttpGet("deliveries")]
    public async Task<IActionResult> GetDeliveries(
        [FromQuery] DPCMDeliveriesRequest request,
        CancellationToken ct)
    {
        var dpcmId = GetUserId();
        var deliveries = await _dashboardService.GetDPCMDeliveriesAsync(dpcmId, request, ct);
        return Ok(deliveries);
    }

    /// <summary>
    /// Get commission configuration
    /// GET /api/v1/dpcm/commission
    /// </summary>
    [HttpGet("commission")]
    public async Task<IActionResult> GetCommissionConfig(CancellationToken ct)
    {
        var dpcmId = GetUserId();
        var config = await _dashboardService.GetDPCMCommissionConfigAsync(dpcmId, ct);
        return Ok(config);
    }

    /// <summary>
    /// Update commission configuration
    /// PUT /api/v1/dpcm/commission
    /// </summary>
    [HttpPut("commission")]
    public async Task<IActionResult> UpdateCommissionConfig(
        [FromBody] UpdateCommissionConfigRequest request,
        CancellationToken ct)
    {
        var dpcmId = GetUserId();
        var success = await _dashboardService.UpdateDPCMCommissionConfigAsync(dpcmId, request, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to update commission configuration" });
        }

        return Ok(new { message = "Commission configuration updated successfully" });
    }

    /// <summary>
    /// Get settlement history
    /// GET /api/v1/dpcm/settlements
    /// </summary>
    [HttpGet("settlements")]
    public async Task<IActionResult> GetSettlements(
        [FromQuery] DPCMSettlementsRequest request,
        CancellationToken ct)
    {
        var dpcmId = GetUserId();
        var settlements = await _dashboardService.GetDPCMSettlementsAsync(dpcmId, request, ct);
        return Ok(settlements);
    }

    /// <summary>
    /// Request a settlement
    /// POST /api/v1/dpcm/settlements/request
    /// </summary>
    [HttpPost("settlements/request")]
    public async Task<IActionResult> RequestSettlement(
        [FromBody] RequestSettlementRequest request,
        CancellationToken ct)
    {
        var dpcmId = GetUserId();
        var success = await _dashboardService.RequestDPCMSettlementAsync(dpcmId, request.Amount, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to request settlement. Minimum amount is ₹100 and must not exceed available balance." });
        }

        return Ok(new { message = "Settlement request submitted successfully" });
    }
}
