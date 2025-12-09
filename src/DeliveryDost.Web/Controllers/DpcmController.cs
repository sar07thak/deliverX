using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryDost.Application.DTOs.Registration;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;
using DeliveryDost.Infrastructure.Services;
using DeliveryDost.Web.ViewModels.Dpcm;

namespace DeliveryDost.Web.Controllers;

[Authorize(Roles = "DPCM")]
public class DpcmController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPANVerificationService _panService;
    private readonly IBankVerificationService _bankService;
    private readonly IDuplicateDetectionService _duplicateDetection;
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DpcmController> _logger;

    public DpcmController(
        ApplicationDbContext context,
        IPANVerificationService panService,
        IBankVerificationService bankService,
        IDuplicateDetectionService duplicateDetection,
        IDashboardService dashboardService,
        ILogger<DpcmController> logger)
    {
        _context = context;
        _panService = panService;
        _bankService = bankService;
        _duplicateDetection = duplicateDetection;
        _dashboardService = dashboardService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    private bool IsProfileComplete() => User.FindFirst("ProfileComplete")?.Value == "True";

    /// <summary>
    /// DPCM Registration - simpler than DP (3 steps)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (IsProfileComplete())
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var model = new DpcmRegistrationViewModel
        {
            CurrentStep = 1
        };

        LoadFromSession(model);

        ViewData["Title"] = "DPCM Registration";
        return View(model);
    }

    /// <summary>
    /// Step 1: Save Personal Info
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePersonalInfo(DpcmRegistrationViewModel model)
    {
        // Load existing session data first
        LoadFromSession(model);

        ModelState.Clear();
        if (!TryValidateModel(model.PersonalInfo, nameof(model.PersonalInfo)))
        {
            model.CurrentStep = 1;
            return View("Register", model);
        }

        // Validate age (must be 18+)
        if (model.PersonalInfo.DOB.HasValue)
        {
            var age = DateTime.Today.Year - model.PersonalInfo.DOB.Value.Year;
            if (model.PersonalInfo.DOB.Value > DateTime.Today.AddYears(-age)) age--;

            if (age < 18)
            {
                ModelState.AddModelError("PersonalInfo.DOB", "You must be at least 18 years old");
                model.CurrentStep = 1;
                return View("Register", model);
            }
        }

        SaveToSession(model);
        model.CurrentStep = 2;
        return View("Register", model);
    }

    /// <summary>
    /// Step 2: Save Bank Details
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBankDetails(DpcmRegistrationViewModel model)
    {
        LoadFromSession(model);

        ModelState.Clear();
        if (!TryValidateModel(model.BankDetails, nameof(model.BankDetails)))
        {
            model.CurrentStep = 2;
            return View("Register", model);
        }

        var userId = GetUserId();

        try
        {
            var bankResult = await _bankService.VerifyBankAccountAsync(
                userId,
                model.BankDetails.AccountNumber,
                model.BankDetails.IFSCCode,
                model.BankDetails.AccountHolderName,
                "PENNY_DROP",
                CancellationToken.None);

            if (bankResult.IsSuccess)
            {
                TempData["Success"] = "Bank verification initiated";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bank verification failed for DPCM {UserId}", userId);
            TempData["Warning"] = "Bank verification will be processed later";
        }

        SaveToSession(model);
        model.CurrentStep = 3;
        return View("Register", model);
    }

    /// <summary>
    /// Step 3: Save KYC and Complete Registration
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteRegistration(DpcmRegistrationViewModel model)
    {
        LoadFromSession(model);

        ModelState.Clear();
        if (!TryValidateModel(model.KycDocuments, nameof(model.KycDocuments)))
        {
            model.CurrentStep = 3;
            return View("Register", model);
        }

        var userId = GetUserId();

        try
        {
            // Check duplicate PAN using CheckDuplicatesAsync
            var duplicateCheck = await _duplicateDetection.CheckDuplicatesAsync(
                phone: null,
                aadhaarHash: null,
                pan: model.KycDocuments.PAN,
                bankAccountHash: null,
                CancellationToken.None);

            if (duplicateCheck.IsDuplicate)
            {
                ModelState.AddModelError("KycDocuments.PAN", "This PAN is already registered");
                model.CurrentStep = 3;
                return View("Register", model);
            }

            // Verify PAN
            await _panService.VerifyPANAsync(
                userId,
                model.KycDocuments.PAN,
                CancellationToken.None);

            // Create or update DPCM profile
            var existingProfile = await _context.DPCManagers
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (existingProfile == null)
            {
                // Create new DPCM profile
                var dpcmProfile = new DPCManager
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ContactPersonName = model.PersonalInfo.FullName ?? "",
                    OrganizationName = model.PersonalInfo.FullName ?? "", // Use name as organization for now
                    PAN = model.KycDocuments.PAN ?? "",
                    ServiceRegions = JsonSerializer.Serialize(new[] { model.PersonalInfo.City }),
                    BankAccountEncrypted = JsonSerializer.Serialize(new
                    {
                        AccountHolderName = model.BankDetails.AccountHolderName,
                        AccountNumber = model.BankDetails.AccountNumber,
                        IFSCCode = model.BankDetails.IFSCCode,
                        BankName = model.BankDetails.BankName,
                        BranchName = model.BankDetails.BranchName
                    }),
                    CommissionType = "PERCENTAGE",
                    CommissionValue = 5, // Default 5% commission
                    IsActive = true,
                    ActivatedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DPCManagers.Add(dpcmProfile);
            }
            else
            {
                // Update existing profile
                existingProfile.ContactPersonName = model.PersonalInfo.FullName ?? "";
                existingProfile.PAN = model.KycDocuments.PAN ?? "";
                existingProfile.UpdatedAt = DateTime.UtcNow;
            }

            // Update user email if provided
            var user = await _context.Users.FindAsync(userId);
            if (user != null && !string.IsNullOrEmpty(model.PersonalInfo.Email))
            {
                user.Email = model.PersonalInfo.Email;
            }

            // Create wallet if not exists
            var existingWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (existingWallet == null)
            {
                var wallet = new Wallet
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Balance = 0,
                    HoldBalance = 0,
                    Currency = "INR",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Wallets.Add(wallet);
            }

            await _context.SaveChangesAsync();

            ClearSession();

            _logger.LogInformation("DPCM registration completed for user {UserId}", userId);

            TempData["Success"] = "Registration completed! Your account is being verified.";
            return RedirectToAction("Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DPCM registration failed for user {UserId}", userId);
            ModelState.AddModelError("", "Registration failed. Please try again.");
            model.CurrentStep = 3;
            return View("Register", model);
        }
    }

    /// <summary>
    /// Navigate to a specific step (GET for back links)
    /// </summary>
    [HttpGet]
    public IActionResult GoToStep(int step)
    {
        var model = new DpcmRegistrationViewModel();
        LoadFromSession(model);
        model.CurrentStep = Math.Max(1, Math.Min(step, model.TotalSteps));
        return View("Register", model);
    }

    /// <summary>
    /// Navigate to previous step (POST for form submissions)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult GoToStep(DpcmRegistrationViewModel model, int step)
    {
        LoadFromSession(model);
        SaveToSession(model);
        model.CurrentStep = Math.Max(1, Math.Min(step, model.TotalSteps));
        return View("Register", model);
    }

    /// <summary>
    /// DPCM Dashboard - Overview of managed DPs
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var userId = GetUserId();

        try
        {
            // Fetch actual data from database service
            var dashboardData = await _dashboardService.GetDPCMDashboardAsync(userId, CancellationToken.None);

            var model = new DpcmDashboardViewModel
            {
                UserId = userId,
                FullName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Channel Manager",
                ReferralCode = $"DPCM{userId.ToString().Substring(0, 8).ToUpper()}",
                TotalDPs = dashboardData.Stats.TotalManagedDPs,
                ActiveDPs = dashboardData.Stats.ActiveDPs,
                PendingKYC = dashboardData.Stats.PendingOnboarding,
                TotalCommission = dashboardData.Earnings.TotalEarnings,
                MonthlyCommission = dashboardData.Earnings.EarningsThisMonth,
                RecentDPs = dashboardData.ManagedDPs.Take(5).Select(dp => new DPSummaryItem
                {
                    Id = dp.DPId,
                    Name = dp.Name,
                    Phone = dp.Phone,
                    Status = dp.Status,
                    IsActive = dp.Status == "ACTIVE",
                    IsOnline = dp.IsOnline,
                    KycStatus = dp.Status == "ACTIVE" ? "VERIFIED" : "PENDING",
                    JoinedAt = dp.LastActive ?? DateTime.UtcNow,
                    DeliveriesCompleted = dp.TotalDeliveries,
                    TotalDeliveries = dp.TotalDeliveries,
                    Rating = dp.Rating,
                    TotalEarnings = 0
                }).ToList(),
                PendingActions = dashboardData.PendingActions.Select(pa => new PendingActionItem
                {
                    Type = pa.ActionType,
                    Description = pa.Description,
                    ActionUrl = "#",
                    CreatedAt = pa.DueDate
                }).ToList()
            };

            ViewData["Title"] = "DPCM Dashboard";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching DPCM dashboard for user {UserId}", userId);

            // Return empty dashboard on error
            var model = new DpcmDashboardViewModel
            {
                UserId = userId,
                FullName = "Channel Manager",
                ReferralCode = $"DPCM{userId.ToString().Substring(0, 8).ToUpper()}",
                TotalDPs = 0,
                ActiveDPs = 0,
                PendingKYC = 0,
                TotalCommission = 0,
                MonthlyCommission = 0,
                RecentDPs = new List<DPSummaryItem>(),
                PendingActions = new List<PendingActionItem>()
            };

            TempData["Error"] = "Unable to load dashboard data. Please try again.";
            ViewData["Title"] = "DPCM Dashboard";
            return View(model);
        }
    }

    /// <summary>
    /// View all DPs under this DPCM
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> MyDPs(string? status = null, int page = 1)
    {
        var userId = GetUserId();

        try
        {
            // Fetch DPs from database service
            var request = new DeliveryDost.Application.DTOs.Dashboard.DPCMPartnersRequest
            {
                Status = status,
                Page = page,
                PageSize = 20
            };

            var response = await _dashboardService.GetDPCMPartnersAsync(userId, request, CancellationToken.None);

            var dpList = response.Items.Select(dp => new DPSummaryItem
            {
                Id = dp.Id,
                Name = dp.Name,
                Phone = dp.Phone,
                Status = dp.Status,
                IsActive = dp.Status == "ACTIVE",
                IsOnline = dp.IsOnline,
                KycStatus = dp.KYCStatus,
                JoinedAt = dp.CreatedAt,
                DeliveriesCompleted = dp.TotalDeliveries,
                TotalDeliveries = dp.TotalDeliveries,
                Rating = dp.Rating,
                TotalEarnings = dp.Earnings
            }).ToList();

            ViewData["Title"] = "My Delivery Partners";
            ViewData["CurrentStatus"] = status;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)response.TotalCount / response.PageSize);
            ViewData["TotalCount"] = response.TotalCount;

            return View(dpList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching DPs for DPCM {UserId}", userId);
            TempData["Error"] = "Unable to load delivery partners. Please try again.";
            ViewData["Title"] = "My Delivery Partners";
            return View(new List<DPSummaryItem>());
        }
    }

    /// <summary>
    /// Update DP status (activate/deactivate)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDPStatus(Guid dpId, bool isActive)
    {
        var userId = GetUserId();

        try
        {
            var result = await _dashboardService.UpdateDPStatusByDPCMAsync(userId, dpId, isActive, CancellationToken.None);

            if (result)
            {
                TempData["Success"] = isActive ? "Delivery Partner activated successfully" : "Delivery Partner deactivated successfully";
            }
            else
            {
                TempData["Error"] = "Unable to update status. You can only manage DPs recruited by you.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DP {DPId} status by DPCM {UserId}", dpId, userId);
            TempData["Error"] = "An error occurred while updating status.";
        }

        return RedirectToAction(nameof(MyDPs));
    }

    #region Session Management

    private const string SessionKey = "DpcmRegistration";

    private void SaveToSession(DpcmRegistrationViewModel model)
    {
        HttpContext.Session.SetString($"{SessionKey}_FullName", model.PersonalInfo.FullName ?? "");
        HttpContext.Session.SetString($"{SessionKey}_Email", model.PersonalInfo.Email ?? "");
        if (model.PersonalInfo.DOB.HasValue)
            HttpContext.Session.SetString($"{SessionKey}_DOB", model.PersonalInfo.DOB.Value.ToString("yyyy-MM-dd"));
        HttpContext.Session.SetString($"{SessionKey}_Gender", model.PersonalInfo.Gender ?? "");
        HttpContext.Session.SetString($"{SessionKey}_AddressLine1", model.PersonalInfo.AddressLine1 ?? "");
        HttpContext.Session.SetString($"{SessionKey}_AddressLine2", model.PersonalInfo.AddressLine2 ?? "");
        HttpContext.Session.SetString($"{SessionKey}_City", model.PersonalInfo.City ?? "");
        HttpContext.Session.SetString($"{SessionKey}_State", model.PersonalInfo.State ?? "");
        HttpContext.Session.SetString($"{SessionKey}_Pincode", model.PersonalInfo.Pincode ?? "");

        HttpContext.Session.SetString($"{SessionKey}_AccountHolderName", model.BankDetails.AccountHolderName ?? "");
        HttpContext.Session.SetString($"{SessionKey}_AccountNumber", model.BankDetails.AccountNumber ?? "");
        HttpContext.Session.SetString($"{SessionKey}_ConfirmAccountNumber", model.BankDetails.ConfirmAccountNumber ?? "");
        HttpContext.Session.SetString($"{SessionKey}_IFSCCode", model.BankDetails.IFSCCode ?? "");
        HttpContext.Session.SetString($"{SessionKey}_BankName", model.BankDetails.BankName ?? "");
        HttpContext.Session.SetString($"{SessionKey}_BranchName", model.BankDetails.BranchName ?? "");

        HttpContext.Session.SetString($"{SessionKey}_PAN", model.KycDocuments.PAN ?? "");
    }

    private void LoadFromSession(DpcmRegistrationViewModel model)
    {
        model.PersonalInfo.FullName = HttpContext.Session.GetString($"{SessionKey}_FullName") ?? model.PersonalInfo.FullName;
        model.PersonalInfo.Email = HttpContext.Session.GetString($"{SessionKey}_Email") ?? model.PersonalInfo.Email;
        var dobStr = HttpContext.Session.GetString($"{SessionKey}_DOB");
        if (!string.IsNullOrEmpty(dobStr) && DateTime.TryParse(dobStr, out var dob))
            model.PersonalInfo.DOB = dob;
        model.PersonalInfo.Gender = HttpContext.Session.GetString($"{SessionKey}_Gender") ?? model.PersonalInfo.Gender;
        model.PersonalInfo.AddressLine1 = HttpContext.Session.GetString($"{SessionKey}_AddressLine1") ?? model.PersonalInfo.AddressLine1;
        model.PersonalInfo.AddressLine2 = HttpContext.Session.GetString($"{SessionKey}_AddressLine2") ?? model.PersonalInfo.AddressLine2;
        model.PersonalInfo.City = HttpContext.Session.GetString($"{SessionKey}_City") ?? model.PersonalInfo.City;
        model.PersonalInfo.State = HttpContext.Session.GetString($"{SessionKey}_State") ?? model.PersonalInfo.State;
        model.PersonalInfo.Pincode = HttpContext.Session.GetString($"{SessionKey}_Pincode") ?? model.PersonalInfo.Pincode;

        model.BankDetails.AccountHolderName = HttpContext.Session.GetString($"{SessionKey}_AccountHolderName") ?? model.BankDetails.AccountHolderName;
        model.BankDetails.AccountNumber = HttpContext.Session.GetString($"{SessionKey}_AccountNumber") ?? model.BankDetails.AccountNumber;
        model.BankDetails.ConfirmAccountNumber = HttpContext.Session.GetString($"{SessionKey}_ConfirmAccountNumber") ?? model.BankDetails.ConfirmAccountNumber;
        model.BankDetails.IFSCCode = HttpContext.Session.GetString($"{SessionKey}_IFSCCode") ?? model.BankDetails.IFSCCode;
        model.BankDetails.BankName = HttpContext.Session.GetString($"{SessionKey}_BankName") ?? model.BankDetails.BankName;
        model.BankDetails.BranchName = HttpContext.Session.GetString($"{SessionKey}_BranchName") ?? model.BankDetails.BranchName;

        model.KycDocuments.PAN = HttpContext.Session.GetString($"{SessionKey}_PAN") ?? model.KycDocuments.PAN;
    }

    private void ClearSession()
    {
        var keys = new[]
        {
            "FullName", "Email", "DOB", "Gender",
            "AddressLine1", "AddressLine2", "City", "State", "Pincode",
            "AccountHolderName", "AccountNumber", "ConfirmAccountNumber", "IFSCCode", "BankName", "BranchName",
            "PAN"
        };

        foreach (var key in keys)
        {
            HttpContext.Session.Remove($"{SessionKey}_{key}");
        }
    }

    #endregion
}
