using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryDost.Application.DTOs.Complaint;
using DeliveryDost.Application.Services;

namespace DeliveryDost.API.Controllers;

[ApiController]
[Route("api/v1/complaints")]
[Authorize]
public class ComplaintsController : ControllerBase
{
    private readonly IComplaintService _complaintService;

    public ComplaintsController(IComplaintService complaintService)
    {
        _complaintService = complaintService;
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
    /// Create a new complaint
    /// POST /api/v1/complaints
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateComplaint(
        [FromBody] CreateComplaintRequest request,
        CancellationToken ct)
    {
        var raiserId = GetUserId();
        var raiserType = GetUserRole();

        var result = await _complaintService.CreateComplaintAsync(raiserId, raiserType, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { code = result.ErrorCode, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get complaint by ID
    /// GET /api/v1/complaints/{complaintId}
    /// </summary>
    [HttpGet("{complaintId}")]
    public async Task<IActionResult> GetComplaint(Guid complaintId, CancellationToken ct)
    {
        var complaint = await _complaintService.GetComplaintAsync(complaintId, ct);
        if (complaint == null)
        {
            return NotFound(new { error = "Complaint not found" });
        }
        return Ok(complaint);
    }

    /// <summary>
    /// Get complaint by number
    /// GET /api/v1/complaints/number/{complaintNumber}
    /// </summary>
    [HttpGet("number/{complaintNumber}")]
    public async Task<IActionResult> GetComplaintByNumber(string complaintNumber, CancellationToken ct)
    {
        var complaint = await _complaintService.GetComplaintByNumberAsync(complaintNumber, ct);
        if (complaint == null)
        {
            return NotFound(new { error = "Complaint not found" });
        }
        return Ok(complaint);
    }

    /// <summary>
    /// Get list of complaints
    /// GET /api/v1/complaints
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetComplaints(
        [FromQuery] GetComplaintsRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        var result = await _complaintService.GetComplaintsAsync(request, userId, userRole, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get my complaints (raised by current user)
    /// GET /api/v1/complaints/my
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyComplaints(
        [FromQuery] GetComplaintsRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _complaintService.GetComplaintsAsync(request, userId, null, ct);
        return Ok(result);
    }

    /// <summary>
    /// Add evidence to a complaint
    /// POST /api/v1/complaints/{complaintId}/evidence
    /// </summary>
    [HttpPost("{complaintId}/evidence")]
    public async Task<IActionResult> AddEvidence(
        Guid complaintId,
        [FromBody] AddEvidenceRequest request,
        CancellationToken ct)
    {
        var uploaderId = GetUserId();
        var success = await _complaintService.AddEvidenceAsync(complaintId, uploaderId, request, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to add evidence" });
        }

        return Ok(new { message = "Evidence added successfully" });
    }

    /// <summary>
    /// Add comment to a complaint
    /// POST /api/v1/complaints/{complaintId}/comments
    /// </summary>
    [HttpPost("{complaintId}/comments")]
    public async Task<IActionResult> AddComment(
        Guid complaintId,
        [FromBody] AddCommentRequest request,
        CancellationToken ct)
    {
        var authorId = GetUserId();
        var success = await _complaintService.AddCommentAsync(complaintId, authorId, request, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to add comment" });
        }

        return Ok(new { message = "Comment added successfully" });
    }

    /// <summary>
    /// Assign complaint to inspector
    /// POST /api/v1/complaints/{complaintId}/assign
    /// </summary>
    [HttpPost("{complaintId}/assign")]
    [Authorize(Roles = "SA,DPCM,INSPECTOR")]
    public async Task<IActionResult> AssignComplaint(
        Guid complaintId,
        [FromBody] AssignComplaintRequest request,
        CancellationToken ct)
    {
        var success = await _complaintService.AssignComplaintAsync(complaintId, request.InspectorId, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to assign complaint" });
        }

        return Ok(new { message = "Complaint assigned successfully" });
    }

    /// <summary>
    /// Update complaint status
    /// PATCH /api/v1/complaints/{complaintId}/status
    /// </summary>
    [HttpPatch("{complaintId}/status")]
    [Authorize(Roles = "SA,DPCM,INSPECTOR")]
    public async Task<IActionResult> UpdateStatus(
        Guid complaintId,
        [FromBody] UpdateStatusDto request,
        CancellationToken ct)
    {
        var success = await _complaintService.UpdateStatusAsync(complaintId, request.Status, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to update status" });
        }

        return Ok(new { message = "Status updated successfully" });
    }

    /// <summary>
    /// Update complaint severity
    /// PATCH /api/v1/complaints/{complaintId}/severity
    /// </summary>
    [HttpPatch("{complaintId}/severity")]
    [Authorize(Roles = "SA,DPCM,INSPECTOR")]
    public async Task<IActionResult> UpdateSeverity(
        Guid complaintId,
        [FromBody] UpdateSeverityRequest request,
        CancellationToken ct)
    {
        var success = await _complaintService.UpdateSeverityAsync(complaintId, request.Severity, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to update severity" });
        }

        return Ok(new { message = "Severity updated successfully" });
    }

    /// <summary>
    /// Resolve a complaint
    /// POST /api/v1/complaints/{complaintId}/resolve
    /// </summary>
    [HttpPost("{complaintId}/resolve")]
    [Authorize(Roles = "SA,DPCM,INSPECTOR")]
    public async Task<IActionResult> ResolveComplaint(
        Guid complaintId,
        [FromBody] ResolveComplaintRequest request,
        CancellationToken ct)
    {
        var resolverId = GetUserId();
        var success = await _complaintService.ResolveComplaintAsync(complaintId, resolverId, request, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to resolve complaint" });
        }

        return Ok(new { message = "Complaint resolved successfully" });
    }

    /// <summary>
    /// Close a complaint
    /// POST /api/v1/complaints/{complaintId}/close
    /// </summary>
    [HttpPost("{complaintId}/close")]
    [Authorize(Roles = "SA,DPCM,INSPECTOR")]
    public async Task<IActionResult> CloseComplaint(Guid complaintId, CancellationToken ct)
    {
        var success = await _complaintService.CloseComplaintAsync(complaintId, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to close complaint" });
        }

        return Ok(new { message = "Complaint closed successfully" });
    }

    /// <summary>
    /// Reject a complaint
    /// POST /api/v1/complaints/{complaintId}/reject
    /// </summary>
    [HttpPost("{complaintId}/reject")]
    [Authorize(Roles = "SA,DPCM,INSPECTOR")]
    public async Task<IActionResult> RejectComplaint(
        Guid complaintId,
        [FromBody] RejectComplaintDto request,
        CancellationToken ct)
    {
        var success = await _complaintService.RejectComplaintAsync(complaintId, request.Reason, ct);

        if (!success)
        {
            return BadRequest(new { error = "Failed to reject complaint" });
        }

        return Ok(new { message = "Complaint rejected" });
    }

    /// <summary>
    /// Get complaint statistics
    /// GET /api/v1/complaints/stats
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "SA,DPCM,INSPECTOR")]
    public async Task<IActionResult> GetStats(
        [FromQuery] Guid? inspectorId,
        CancellationToken ct)
    {
        var stats = await _complaintService.GetComplaintStatsAsync(inspectorId, ct);
        return Ok(stats);
    }

    // ==================
    // Inspector Endpoints
    // ==================

    /// <summary>
    /// Get available inspectors
    /// GET /api/v1/complaints/inspectors
    /// </summary>
    [HttpGet("inspectors")]
    [Authorize(Roles = "SA,DPCM")]
    public async Task<IActionResult> GetAvailableInspectors(CancellationToken ct)
    {
        var inspectors = await _complaintService.GetAvailableInspectorsAsync(ct);
        return Ok(inspectors);
    }

    /// <summary>
    /// Create an inspector
    /// POST /api/v1/complaints/inspectors
    /// </summary>
    [HttpPost("inspectors")]
    [Authorize(Roles = "SA")]
    public async Task<IActionResult> CreateInspector(
        [FromBody] CreateInspectorRequest request,
        CancellationToken ct)
    {
        var inspector = await _complaintService.CreateInspectorAsync(request, ct);

        if (inspector == null)
        {
            return BadRequest(new { error = "Failed to create inspector" });
        }

        return Ok(inspector);
    }

    /// <summary>
    /// Get inspector by ID
    /// GET /api/v1/complaints/inspectors/{inspectorId}
    /// </summary>
    [HttpGet("inspectors/{inspectorId}")]
    [Authorize(Roles = "SA,DPCM")]
    public async Task<IActionResult> GetInspector(Guid inspectorId, CancellationToken ct)
    {
        var inspector = await _complaintService.GetInspectorAsync(inspectorId, ct);
        if (inspector == null)
        {
            return NotFound(new { error = "Inspector not found" });
        }
        return Ok(inspector);
    }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class RejectComplaintDto
{
    public string Reason { get; set; } = string.Empty;
}
