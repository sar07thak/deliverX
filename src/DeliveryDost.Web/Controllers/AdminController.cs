using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliverX.Application.Services;
using DeliverX.Application.DTOs.Dashboard;
using DeliveryDost.Web.ViewModels.Admin;

namespace DeliveryDost.Web.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class AdminController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IComplaintService _complaintService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IDashboardService dashboardService,
        IComplaintService complaintService,
        ILogger<AdminController> logger)
    {
        _dashboardService = dashboardService;
        _complaintService = complaintService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    private string GetUserRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> Users(string? role = null, string? status = null, int page = 1)
    {
        try
        {
            var request = new UserListRequest
            {
                Page = page,
                PageSize = 20,
                Role = role,
                Status = status
            };

            var response = await _dashboardService.GetUsersAsync(request, CancellationToken.None);

            var model = new UsersListViewModel
            {
                Users = response.Items.Select(u => new UserItemViewModel
                {
                    Id = u.Id,
                    Phone = u.Phone ?? "",
                    Name = u.Name ?? u.Phone ?? "Unknown",
                    Role = u.Role ?? "",
                    Status = u.Status?.ToUpper() == "ACTIVE" ? "Active" : "Inactive",
                    CreatedAt = u.CreatedAt
                }).ToList(),
                CurrentPage = response.Page,
                TotalPages = response.TotalPages,
                TotalCount = response.TotalCount,
                RoleFilter = role,
                StatusFilter = status
            };

            ViewData["Title"] = "User Management";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users");
            TempData["Error"] = "Failed to load users";
            return View(new UsersListViewModel());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserStatus(Guid userId, string status, string? reason)
    {
        try
        {
            var adminId = GetUserId();
            var request = new UpdateUserStatusRequest
            {
                Status = status,
                Reason = reason
            };

            await _dashboardService.UpdateUserStatusAsync(userId, request, adminId, CancellationToken.None);
            TempData["Success"] = $"User status updated to {status}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status for {UserId}", userId);
            TempData["Error"] = "Failed to update user status";
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> KycRequests(string? status = "PENDING", int page = 1)
    {
        try
        {
            var request = new KYCListRequest
            {
                Page = page,
                PageSize = 20,
                Status = status
            };

            var response = await _dashboardService.GetKYCRequestsAsync(request, CancellationToken.None);

            var model = new KycRequestsViewModel
            {
                Requests = response.Items.Select(k => new KycRequestItemViewModel
                {
                    Id = k.Id,
                    UserId = k.UserId,
                    UserPhone = k.Phone ?? "Unknown",
                    UserName = k.UserName ?? "Unknown",
                    DocumentType = k.DocumentType ?? "",
                    Status = k.Status ?? "PENDING",
                    SubmittedAt = k.SubmittedAt,
                    ReviewedAt = k.ReviewedAt,
                    RejectionReason = null
                }).ToList(),
                CurrentPage = response.Page,
                TotalPages = response.TotalPages,
                TotalCount = response.TotalCount,
                StatusFilter = status
            };

            ViewData["Title"] = "KYC Requests";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching KYC requests");
            TempData["Error"] = "Failed to load KYC requests";
            return View(new KycRequestsViewModel());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ApproveKyc(Guid kycId)
    {
        try
        {
            var adminId = GetUserId();
            var request = new ApproveKYCRequest { Notes = "Approved via web UI" };
            await _dashboardService.ApproveKYCAsync(kycId, request, adminId, CancellationToken.None);
            TempData["Success"] = "KYC request approved successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving KYC {KycId}", kycId);
            TempData["Error"] = "Failed to approve KYC request";
        }

        return RedirectToAction(nameof(KycRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RejectKyc(Guid kycId, string reason)
    {
        try
        {
            var adminId = GetUserId();
            var request = new RejectKYCRequest { Reason = reason };
            await _dashboardService.RejectKYCAsync(kycId, request, adminId, CancellationToken.None);
            TempData["Success"] = "KYC request rejected";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting KYC {KycId}", kycId);
            TempData["Error"] = "Failed to reject KYC request";
        }

        return RedirectToAction(nameof(KycRequests));
    }

    [HttpGet]
    public async Task<IActionResult> Complaints(string? status = null, int page = 1)
    {
        try
        {
            var request = new DeliverX.Application.DTOs.Complaint.GetComplaintsRequest
            {
                Page = page,
                PageSize = 20,
                Status = status
            };

            var response = await _complaintService.GetComplaintsAsync(request, null, null, CancellationToken.None);

            var model = new ComplaintsListViewModel
            {
                Complaints = response.Complaints.Select(c => new ComplaintItemViewModel
                {
                    Id = c.Id,
                    TicketNumber = c.ComplaintNumber ?? c.Id.ToString().Substring(0, 8),
                    Category = c.Category ?? "",
                    Priority = c.Severity ?? "LOW",
                    Status = c.Status ?? "OPEN",
                    Description = c.Description ?? "",
                    CreatedAt = c.CreatedAt,
                    CreatedByPhone = c.RaisedByName ?? "Unknown"
                }).ToList(),
                CurrentPage = response.Page,
                TotalPages = response.TotalPages,
                TotalCount = response.TotalCount,
                StatusFilter = status
            };

            ViewData["Title"] = "Complaints Management";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching complaints");
            TempData["Error"] = "Failed to load complaints";
            return View(new ComplaintsListViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> ComplaintDetails(Guid id)
    {
        try
        {
            var complaint = await _complaintService.GetComplaintAsync(id, CancellationToken.None);
            if (complaint == null)
            {
                TempData["Error"] = "Complaint not found";
                return RedirectToAction(nameof(Complaints));
            }

            ViewData["Title"] = $"Complaint #{complaint.ComplaintNumber}";
            return View(complaint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching complaint {Id}", id);
            TempData["Error"] = "Failed to load complaint details";
            return RedirectToAction(nameof(Complaints));
        }
    }
}
