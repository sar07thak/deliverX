using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryDost.Application.Services;
using DeliveryDost.Application.DTOs.Dashboard;
using ReportRequest = DeliveryDost.Application.DTOs.Reports.ReportRequest;
using DeliveryDost.Web.ViewModels.Admin;

namespace DeliveryDost.Web.Controllers;

[Authorize(Roles = "Admin")]
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
    public async Task<IActionResult> KycRequests(string? status = null, int page = 1)
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

    #region Stakeholder Onboarding

    /// <summary>
    /// Stakeholder Onboarding Dashboard - Main page showing stats and quick actions
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> StakeholderOnboarding()
    {
        try
        {
            var stats = await _dashboardService.GetOnboardingStatsAsync(CancellationToken.None);
            var availableDPCMs = await _dashboardService.GetAvailableDPCMsAsync(CancellationToken.None);

            var model = new StakeholderOnboardingViewModel
            {
                Stats = stats,
                AvailableDPCMs = availableDPCMs
            };

            ViewData["Title"] = "Stakeholder Onboarding";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading stakeholder onboarding page");
            TempData["Error"] = "Failed to load onboarding data";
            return View(new StakeholderOnboardingViewModel());
        }
    }

    /// <summary>
    /// Register a new stakeholder (DPCM, DP, BC, EC)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterStakeholder(RegisterStakeholderViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill all required fields";
                return RedirectToAction(nameof(StakeholderOnboarding));
            }

            var adminId = GetUserId();
            var request = new RegisterStakeholderRequest
            {
                Phone = model.Phone,
                Role = model.Role,
                FullName = model.FullName,
                Email = model.Email,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Pincode = model.Pincode,
                BusinessName = model.BusinessName,
                BusinessType = model.BusinessType,
                GSTIN = model.GSTIN,
                BusinessPAN = model.BusinessPAN,
                CommissionType = model.CommissionType,
                CommissionValue = model.CommissionValue,
                SecurityDeposit = model.SecurityDeposit,
                ServiceRegions = model.ServiceRegions?.Split(',').Select(s => s.Trim()).ToList(),
                DPCMId = model.DPCMId,
                VehicleType = model.VehicleType,
                VehicleNumber = model.VehicleNumber,
                ServicePincodes = model.ServicePincodes?.Split(',').Select(s => s.Trim()).ToList(),
                SendWelcomeSms = model.SendWelcomeSms,
                AutoCreateWallet = model.AutoCreateWallet,
                SkipKYC = model.SkipKYC,
                Notes = model.Notes
            };

            var result = await _dashboardService.RegisterStakeholderAsync(request, adminId, CancellationToken.None);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(StakeholderOnboarding));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering stakeholder");
            TempData["Error"] = "Failed to register stakeholder";
            return RedirectToAction(nameof(StakeholderOnboarding));
        }
    }

    /// <summary>
    /// Stakeholder List with drill-down
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Stakeholders(
        string? role = null,
        string? status = null,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var request = new StakeholderListRequest
            {
                Role = role,
                Status = status,
                SearchTerm = search,
                Page = page,
                PageSize = pageSize,
                SortDesc = true
            };

            var response = await _dashboardService.GetStakeholdersAsync(request, CancellationToken.None);

            var model = new StakeholdersListViewModel
            {
                Stakeholders = response.Items,
                TotalCount = response.TotalCount,
                Page = response.Page,
                PageSize = response.PageSize,
                TotalPages = response.TotalPages,
                RoleFilter = role,
                StatusFilter = status,
                SearchTerm = search,
                TotalDPCMs = response.TotalDPCMs,
                TotalDPs = response.TotalDPs,
                TotalBCs = response.TotalBCs,
                TotalECs = response.TotalECs,
                PendingKYC = response.PendingKYC
            };

            ViewData["Title"] = "Stakeholder List";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stakeholders");
            TempData["Error"] = "Failed to load stakeholders";
            return View(new StakeholdersListViewModel());
        }
    }

    /// <summary>
    /// Stakeholder Detail View
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> StakeholderDetail(Guid id)
    {
        try
        {
            var detail = await _dashboardService.GetStakeholderDetailAsync(id, CancellationToken.None);
            if (detail == null)
            {
                TempData["Error"] = "Stakeholder not found";
                return RedirectToAction(nameof(Stakeholders));
            }

            ViewData["Title"] = $"Stakeholder - {detail.FullName}";
            return View(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stakeholder detail for {Id}", id);
            TempData["Error"] = "Failed to load stakeholder details";
            return RedirectToAction(nameof(Stakeholders));
        }
    }

    /// <summary>
    /// Get available DPCMs for dropdown (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAvailableDPCMs()
    {
        try
        {
            var dpcms = await _dashboardService.GetAvailableDPCMsAsync(CancellationToken.None);
            return Json(dpcms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available DPCMs");
            return Json(new List<AvailableDPCMDto>());
        }
    }

    #endregion
}
