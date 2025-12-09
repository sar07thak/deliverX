using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryDost.Application.Services;
using DeliveryDost.Application.DTOs.Dashboard;
using ReportRequest = DeliveryDost.Application.DTOs.Reports.ReportRequest;
using DeliveryDost.Web.ViewModels.Admin;

namespace DeliveryDost.Web.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class AdminController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IComplaintService _complaintService;
    private readonly ISuperAdminReportService _reportService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IDashboardService dashboardService,
        IComplaintService complaintService,
        ISuperAdminReportService reportService,
        ILogger<AdminController> logger)
    {
        _dashboardService = dashboardService;
        _complaintService = complaintService;
        _reportService = reportService;
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
            var request = new DeliveryDost.Application.DTOs.Complaint.GetComplaintsRequest
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

    #region Super Admin Reports

    /// <summary>
    /// End Consumer Report - No Aadhaar verification required
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> EndConsumerReport(
        string? searchTerm = null,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var request = new ReportRequest
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Status = status
            };

            var response = await _reportService.GetEndConsumerReportAsync(request, CancellationToken.None);

            var model = new EndConsumerReportViewModel
            {
                Items = response.Items.Select(i => new EndConsumerReportItemViewModel
                {
                    Id = i.Id,
                    Name = i.Name,
                    MobileNumber = i.MobileNumber,
                    MobileNumberMasked = i.MobileNumberMasked,
                    EmailId = i.EmailId,
                    EmailIdMasked = i.EmailIdMasked,
                    StateName = i.StateName,
                    DistrictName = i.DistrictName,
                    Pincode = i.Pincode,
                    Address = i.Address,
                    DateOfBirth = i.DateOfBirth,
                    DateOfJoining = i.DateOfJoining,
                    Status = i.Status,
                    LastServiceAccessDate = i.LastServiceAccessDate,
                    TotalDeliveries = i.TotalDeliveries
                }).ToList(),
                TotalCount = response.TotalCount,
                TotalPages = response.TotalPages,
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Status = status
            };

            ViewData["Title"] = "End Consumer Report";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating End Consumer report");
            TempData["Error"] = "Failed to load End Consumer report";
            return View(new EndConsumerReportViewModel());
        }
    }

    /// <summary>
    /// Business Consumer Report
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> BusinessConsumerReport(
        string? searchTerm = null,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var request = new ReportRequest
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Status = status
            };

            var response = await _reportService.GetBusinessConsumerReportAsync(request, CancellationToken.None);

            var model = new BusinessConsumerReportViewModel
            {
                Items = response.Items.Select(i => new BusinessConsumerReportItemViewModel
                {
                    Id = i.Id,
                    Name = i.Name,
                    BusinessName = i.BusinessName,
                    PersonalPAN = i.PersonalPAN,
                    PersonalPANMasked = i.PersonalPANMasked,
                    PersonalPANVerificationStatus = i.PersonalPANVerificationStatus,
                    BusinessPAN = i.BusinessPAN,
                    BusinessPANMasked = i.BusinessPANMasked,
                    BusinessPANVerificationStatus = i.BusinessPANVerificationStatus,
                    AadhaarNumber = i.AadhaarNumber,
                    AadhaarNumberMasked = i.AadhaarNumberMasked,
                    AadhaarVerificationStatus = i.AadhaarVerificationStatus,
                    MobileNumber = i.MobileNumber,
                    MobileNumberMasked = i.MobileNumberMasked,
                    EmailId = i.EmailId,
                    EmailIdMasked = i.EmailIdMasked,
                    StateName = i.StateName,
                    DistrictName = i.DistrictName,
                    Pincode = i.Pincode,
                    Address = i.Address,
                    DateOfBirth = i.DateOfBirth,
                    DateOfJoining = i.DateOfJoining,
                    NumberOfPickupLocations = i.NumberOfPickupLocations,
                    Status = i.Status,
                    LastServiceAccessDate = i.LastServiceAccessDate,
                    GSTIN = i.GSTIN,
                    BusinessCategory = i.BusinessCategory,
                    SubscriptionPlanName = i.SubscriptionPlanName,
                    SubscriptionExpiry = i.SubscriptionExpiry
                }).ToList(),
                TotalCount = response.TotalCount,
                TotalPages = response.TotalPages,
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Status = status
            };

            ViewData["Title"] = "Business Consumer Report";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Business Consumer report");
            TempData["Error"] = "Failed to load Business Consumer report";
            return View(new BusinessConsumerReportViewModel());
        }
    }

    /// <summary>
    /// Delivery Partner Report
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DeliveryPartnerReport(
        string? searchTerm = null,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var request = new ReportRequest
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Status = status
            };

            var response = await _reportService.GetDeliveryPartnerReportAsync(request, CancellationToken.None);

            var model = new DeliveryPartnerReportViewModel
            {
                Items = response.Items.Select(i => new DeliveryPartnerReportItemViewModel
                {
                    Id = i.Id,
                    Name = i.Name,
                    PersonalPAN = i.PersonalPAN,
                    PersonalPANMasked = i.PersonalPANMasked,
                    PersonalPANVerificationStatus = i.PersonalPANVerificationStatus,
                    AadhaarNumber = i.AadhaarNumber,
                    AadhaarNumberMasked = i.AadhaarNumberMasked,
                    AadhaarVerificationStatus = i.AadhaarVerificationStatus,
                    MobileNumber = i.MobileNumber,
                    MobileNumberMasked = i.MobileNumberMasked,
                    EmailId = i.EmailId,
                    EmailIdMasked = i.EmailIdMasked,
                    StateName = i.StateName,
                    DistrictName = i.DistrictName,
                    Pincode = i.Pincode,
                    Address = i.Address,
                    DateOfBirth = i.DateOfBirth,
                    DateOfJoining = i.DateOfJoining,
                    ServiceAreaDescription = i.ServiceAreaDescription,
                    ServiceAreaRadiusKm = i.ServiceAreaRadiusKm,
                    PerKgRate = i.PerKgRate,
                    PerKmRate = i.PerKmRate,
                    MinCharge = i.MinCharge,
                    Status = i.Status,
                    LastServiceAccessDate = i.LastServiceAccessDate,
                    VehicleType = i.VehicleType,
                    TotalDeliveriesCompleted = i.TotalDeliveriesCompleted,
                    AverageRating = i.AverageRating,
                    DPCMName = i.DPCMName
                }).ToList(),
                TotalCount = response.TotalCount,
                TotalPages = response.TotalPages,
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Status = status
            };

            ViewData["Title"] = "Delivery Partner Report";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Delivery Partner report");
            TempData["Error"] = "Failed to load Delivery Partner report";
            return View(new DeliveryPartnerReportViewModel());
        }
    }

    /// <summary>
    /// DPCM Report
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DPCMReport(
        string? searchTerm = null,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var request = new ReportRequest
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Status = status
            };

            var response = await _reportService.GetDPCMReportAsync(request, CancellationToken.None);

            var model = new DPCMReportViewModel
            {
                Items = response.Items.Select(i => new DPCMReportItemViewModel
                {
                    Id = i.Id,
                    Name = i.Name,
                    BusinessName = i.BusinessName,
                    PersonalPAN = i.PersonalPAN,
                    PersonalPANMasked = i.PersonalPANMasked,
                    PersonalPANVerificationStatus = i.PersonalPANVerificationStatus,
                    BusinessPAN = i.BusinessPAN,
                    BusinessPANMasked = i.BusinessPANMasked,
                    BusinessPANVerificationStatus = i.BusinessPANVerificationStatus,
                    AadhaarNumber = i.AadhaarNumber,
                    AadhaarNumberMasked = i.AadhaarNumberMasked,
                    AadhaarVerificationStatus = i.AadhaarVerificationStatus,
                    MobileNumber = i.MobileNumber,
                    MobileNumberMasked = i.MobileNumberMasked,
                    EmailId = i.EmailId,
                    EmailIdMasked = i.EmailIdMasked,
                    StateName = i.StateName,
                    DistrictName = i.DistrictName,
                    Pincode = i.Pincode,
                    Address = i.Address,
                    DateOfBirth = i.DateOfBirth,
                    DateOfJoining = i.DateOfJoining,
                    NumberOfPickupLocations = i.NumberOfPickupLocations,
                    Status = i.Status,
                    LastServiceAccessDate = i.LastServiceAccessDate,
                    NumberOfBusinessUsersInArea = i.NumberOfBusinessUsersInArea,
                    NumberOfEndUsersInArea = i.NumberOfEndUsersInArea,
                    NumberOfDeliveryPartnersInArea = i.NumberOfDeliveryPartnersInArea,
                    CommissionType = i.CommissionType,
                    CommissionValue = i.CommissionValue,
                    ServiceRegions = i.ServiceRegions,
                    SecurityDeposit = i.SecurityDeposit,
                    AgreementDocumentUrl = i.AgreementDocumentUrl,
                    TotalEarnings = i.TotalEarnings,
                    TotalDeliveriesManaged = i.TotalDeliveriesManaged
                }).ToList(),
                TotalCount = response.TotalCount,
                TotalPages = response.TotalPages,
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Status = status
            };

            ViewData["Title"] = "DPCM Report";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating DPCM report");
            TempData["Error"] = "Failed to load DPCM report";
            return View(new DPCMReportViewModel());
        }
    }

    #endregion
}
