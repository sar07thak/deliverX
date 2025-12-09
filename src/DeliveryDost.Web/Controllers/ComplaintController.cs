using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryDost.Application.DTOs.Complaint;
using DeliveryDost.Application.Services;
using DeliveryDost.Web.ViewModels.Complaint;

namespace DeliveryDost.Web.Controllers;

[Authorize]
public class ComplaintController : Controller
{
    private readonly IComplaintService _complaintService;
    private readonly IDeliveryService _deliveryService;
    private readonly ILogger<ComplaintController> _logger;

    public ComplaintController(IComplaintService complaintService, IDeliveryService deliveryService, ILogger<ComplaintController> logger)
    {
        _complaintService = complaintService;
        _deliveryService = deliveryService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    private string GetUserRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> Index(string? status, string? category, int page = 1)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        try
        {
            var request = new GetComplaintsRequest { Status = status, Category = category, Page = page, PageSize = 20 };
            var response = await _complaintService.GetComplaintsAsync(request, userId, userRole);

            var model = new ComplaintListViewModel
            {
                Complaints = response.Complaints.Select(c => new ComplaintItemViewModel
                {
                    Id = c.Id, ComplaintNumber = c.ComplaintNumber, Category = c.Category,
                    Subject = c.Subject, Status = c.Status, Severity = c.Severity,
                    AssignedToName = c.AssignedToName, CreatedAt = c.CreatedAt
                }).ToList(),
                TotalCount = response.TotalCount, Page = response.Page, PageSize = response.PageSize,
                StatusFilter = status, CategoryFilter = category, ViewMode = "user"
            };

            ViewData["Title"] = "My Complaints";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading complaints");
            TempData["Error"] = "Failed to load complaints";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid deliveryId)
    {
        try
        {
            var delivery = await _deliveryService.GetDeliveryAsync(deliveryId);
            if (delivery == null) { TempData["Error"] = "Delivery not found"; return RedirectToAction("Index", "Delivery"); }

            var model = new CreateComplaintViewModel
            {
                DeliveryId = deliveryId,
                DeliveryNumber = delivery.Id.ToString().Substring(0, 8).ToUpper(),
                AgainstId = delivery.AssignedDP?.DPId,
                AgainstType = "DP",
                AgainstName = delivery.AssignedDP?.DPName
            };

            ViewData["Title"] = "File Complaint";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading complaint form");
            TempData["Error"] = "Failed to load form";
            return RedirectToAction("Index", "Delivery");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateComplaintViewModel model)
    {
        if (!ModelState.IsValid) { ViewData["Title"] = "File Complaint"; return View(model); }

        try
        {
            var request = new CreateComplaintRequest
            {
                DeliveryId = model.DeliveryId, AgainstId = model.AgainstId, AgainstType = model.AgainstType,
                Category = model.Category, Subject = model.Subject, Description = model.Description
            };

            var response = await _complaintService.CreateComplaintAsync(GetUserId(), GetUserRole(), request);

            if (response.IsSuccess)
            {
                TempData["Success"] = $"Complaint {response.ComplaintNumber} filed successfully";
                return RedirectToAction("Details", new { id = response.ComplaintId });
            }

            TempData["Error"] = response.Message ?? "Failed to file complaint";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filing complaint");
            TempData["Error"] = "Failed to file complaint";
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var complaint = await _complaintService.GetComplaintAsync(id);
            if (complaint == null) { TempData["Error"] = "Complaint not found"; return RedirectToAction("Index"); }

            var model = new ComplaintDetailsViewModel
            {
                Id = complaint.Id, ComplaintNumber = complaint.ComplaintNumber, DeliveryId = complaint.DeliveryId,
                DeliveryNumber = complaint.DeliveryTrackingNumber, RaisedByName = complaint.RaisedByName,
                RaisedByType = complaint.RaisedByType, AgainstName = complaint.AgainstName, AgainstType = complaint.AgainstType,
                Category = complaint.Category, Severity = complaint.Severity, Subject = complaint.Subject,
                Description = complaint.Description, Status = complaint.Status, Resolution = complaint.Resolution,
                ResolutionNotes = complaint.ResolutionNotes, AssignedToName = complaint.AssignedToName,
                ResolvedAt = complaint.ResolvedAt, CreatedAt = complaint.CreatedAt,
                Evidences = complaint.Evidences.Select(e => new EvidenceViewModel
                {
                    Id = e.Id, Type = e.Type, FileName = e.FileName, FileUrl = e.FileUrl,
                    Description = e.Description, UploadedAt = e.UploadedAt
                }).ToList(),
                Comments = complaint.Comments.Select(c => new CommentViewModel
                {
                    Id = c.Id, AuthorName = c.AuthorName, Content = c.Content,
                    IsInternal = c.IsInternal, CreatedAt = c.CreatedAt
                }).ToList()
            };

            ViewData["Title"] = $"Complaint #{complaint.ComplaintNumber}";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading complaint {Id}", id);
            TempData["Error"] = "Failed to load complaint";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid complaintId, string content)
    {
        try
        {
            var request = new AddCommentRequest { Content = content, IsInternal = false };
            await _complaintService.AddCommentAsync(complaintId, GetUserId(), request);
            TempData["Success"] = "Comment added";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment");
            TempData["Error"] = "Failed to add comment";
        }
        return RedirectToAction("Details", new { id = complaintId });
    }

    #region Inspector Actions

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,Inspector")]
    public async Task<IActionResult> InspectorDashboard()
    {
        try
        {
            var userId = GetUserId();
            var stats = await _complaintService.GetComplaintStatsAsync(userId);
            var openComplaints = await _complaintService.GetComplaintsAsync(new GetComplaintsRequest { Status = "OPEN", PageSize = 10 });
            var myComplaints = await _complaintService.GetComplaintsAsync(new GetComplaintsRequest { AssignedToId = userId, Status = "IN_PROGRESS", PageSize = 10 });

            var model = new InspectorDashboardViewModel
            {
                OpenCases = stats.OpenComplaints,
                InProgressCases = stats.InProgressComplaints,
                ResolvedToday = stats.ResolvedComplaints,
                AvgResolutionHours = stats.AverageResolutionTimeHours,
                ByCategory = stats.ByCategory,
                BySeverity = stats.BySeverity,
                UnassignedComplaints = openComplaints.Complaints.Select(c => new ComplaintItemViewModel
                {
                    Id = c.Id, ComplaintNumber = c.ComplaintNumber, Category = c.Category,
                    Subject = c.Subject, Status = c.Status, Severity = c.Severity, CreatedAt = c.CreatedAt
                }).ToList(),
                AssignedComplaints = myComplaints.Complaints.Select(c => new ComplaintItemViewModel
                {
                    Id = c.Id, ComplaintNumber = c.ComplaintNumber, Category = c.Category,
                    Subject = c.Subject, Status = c.Status, Severity = c.Severity, CreatedAt = c.CreatedAt
                }).ToList()
            };

            ViewData["Title"] = "Inspector Dashboard";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading inspector dashboard");
            TempData["Error"] = "Failed to load dashboard";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,Inspector")]
    public async Task<IActionResult> AssignToMe(Guid complaintId)
    {
        try
        {
            await _complaintService.AssignComplaintAsync(complaintId, GetUserId());
            await _complaintService.UpdateStatusAsync(complaintId, "IN_PROGRESS");
            TempData["Success"] = "Complaint assigned to you";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning complaint");
            TempData["Error"] = "Failed to assign complaint";
        }
        return RedirectToAction("InspectorDashboard");
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,Inspector")]
    public async Task<IActionResult> Resolve(Guid id)
    {
        var complaint = await _complaintService.GetComplaintAsync(id);
        if (complaint == null) { TempData["Error"] = "Complaint not found"; return RedirectToAction("InspectorDashboard"); }

        var model = new ResolveComplaintViewModel { ComplaintId = id, ComplaintNumber = complaint.ComplaintNumber };
        ViewData["Title"] = "Resolve Complaint";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,Inspector")]
    public async Task<IActionResult> Resolve(ResolveComplaintViewModel model)
    {
        if (!ModelState.IsValid) { ViewData["Title"] = "Resolve Complaint"; return View(model); }

        try
        {
            var request = new ResolveComplaintRequest { Resolution = model.Resolution, ResolutionNotes = model.ResolutionNotes };
            await _complaintService.ResolveComplaintAsync(model.ComplaintId, GetUserId(), request);
            TempData["Success"] = "Complaint resolved";
            return RedirectToAction("InspectorDashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving complaint");
            TempData["Error"] = "Failed to resolve complaint";
            return View(model);
        }
    }

    #endregion
}
