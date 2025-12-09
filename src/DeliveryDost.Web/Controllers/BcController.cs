using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;
using DeliveryDost.Infrastructure.Services;
using DeliveryDost.Web.ViewModels.Bc;

namespace DeliveryDost.Web.Controllers;

[Authorize(Roles = "BC,DBC")]
public class BcController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPANVerificationService _panService;
    private readonly IBankVerificationService _bankService;
    private readonly ILogger<BcController> _logger;

    public BcController(
        ApplicationDbContext context,
        ISubscriptionService subscriptionService,
        IPANVerificationService panService,
        IBankVerificationService bankService,
        ILogger<BcController> logger)
    {
        _context = context;
        _subscriptionService = subscriptionService;
        _panService = panService;
        _bankService = bankService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    private bool IsProfileComplete() => User.FindFirst("ProfileComplete")?.Value == "True";

    /// <summary>
    /// BC Registration - 4 steps: Business Info > Bank Details > Pickup Location > Subscription
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (IsProfileComplete())
        {
            return RedirectToAction("Dashboard");
        }

        var model = new BcRegistrationViewModel
        {
            CurrentStep = 1
        };

        LoadFromSession(model);
        await LoadSubscriptionPlans(model);

        ViewData["Title"] = "Business Registration";
        return View(model);
    }

    /// <summary>
    /// Step 1: Save Business Info
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBusinessInfo(BcRegistrationViewModel model)
    {
        ModelState.Clear();
        if (!TryValidateModel(model.BusinessInfo, nameof(model.BusinessInfo)))
        {
            model.CurrentStep = 1;
            await LoadSubscriptionPlans(model);
            return View("Register", model);
        }

        SaveToSession(model);
        model.CurrentStep = 2;
        await LoadSubscriptionPlans(model);
        return View("Register", model);
    }

    /// <summary>
    /// Step 2: Save Bank Details
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBankDetails(BcRegistrationViewModel model)
    {
        LoadFromSession(model);

        ModelState.Clear();
        if (!TryValidateModel(model.BankDetails, nameof(model.BankDetails)))
        {
            model.CurrentStep = 2;
            await LoadSubscriptionPlans(model);
            return View("Register", model);
        }

        var userId = GetUserId();

        try
        {
            // Verify bank account
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
            _logger.LogError(ex, "Bank verification failed for BC {UserId}", userId);
            TempData["Warning"] = "Bank verification will be processed later";
        }

        SaveToSession(model);
        model.CurrentStep = 3;

        // Pre-fill pickup location from business address
        if (string.IsNullOrEmpty(model.PickupLocation.AddressLine1))
        {
            model.PickupLocation.AddressLine1 = model.BusinessInfo.AddressLine1;
            model.PickupLocation.AddressLine2 = model.BusinessInfo.AddressLine2;
            model.PickupLocation.City = model.BusinessInfo.City;
            model.PickupLocation.State = model.BusinessInfo.State;
            model.PickupLocation.Pincode = model.BusinessInfo.Pincode;
        }

        await LoadSubscriptionPlans(model);
        return View("Register", model);
    }

    /// <summary>
    /// Step 3: Save Pickup Location
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePickupLocation(BcRegistrationViewModel model)
    {
        LoadFromSession(model);

        ModelState.Clear();
        if (!TryValidateModel(model.PickupLocation, nameof(model.PickupLocation)))
        {
            model.CurrentStep = 3;
            await LoadSubscriptionPlans(model);
            return View("Register", model);
        }

        SaveToSession(model);
        model.CurrentStep = 4;
        await LoadSubscriptionPlans(model);
        return View("Register", model);
    }

    /// <summary>
    /// Step 4: Complete Registration with Subscription
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteRegistration(BcRegistrationViewModel model)
    {
        LoadFromSession(model);

        ModelState.Clear();
        if (!TryValidateModel(model.Subscription, nameof(model.Subscription)))
        {
            model.CurrentStep = 4;
            await LoadSubscriptionPlans(model);
            return View("Register", model);
        }

        var userId = GetUserId();

        try
        {
            // Create Business Consumer Profile
            var bcProfile = new BusinessConsumerProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BusinessName = model.BusinessInfo.BusinessName,
                ContactPersonName = model.BusinessInfo.ContactPersonName,
                PAN = model.BusinessInfo.PAN,
                GSTIN = model.BusinessInfo.GSTIN,
                BusinessConstitution = model.BusinessInfo.BusinessConstitution,
                GSTRegistrationType = model.BusinessInfo.GSTRegistrationType,
                BusinessCategory = model.BusinessInfo.BusinessCategory,
                BusinessAddress = System.Text.Json.JsonSerializer.Serialize(new
                {
                    AddressLine1 = model.BusinessInfo.AddressLine1,
                    AddressLine2 = model.BusinessInfo.AddressLine2,
                    City = model.BusinessInfo.City,
                    State = model.BusinessInfo.State,
                    Pincode = model.BusinessInfo.Pincode
                }),
                BankAccountEncrypted = System.Text.Json.JsonSerializer.Serialize(new
                {
                    AccountHolderName = model.BankDetails.AccountHolderName,
                    AccountNumber = model.BankDetails.AccountNumber,
                    IFSCCode = model.BankDetails.IFSCCode,
                    BankName = model.BankDetails.BankName,
                    BranchName = model.BankDetails.BranchName
                }),
                SubscriptionPlanId = model.Subscription.SelectedPlanId,
                SubscriptionStartDate = DateTime.UtcNow,
                IsActive = true,
                ActivatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BusinessConsumerProfiles.Add(bcProfile);

            // Create Pickup Location
            var pickupLocation = new BCPickupLocation
            {
                Id = Guid.NewGuid(),
                BusinessConsumerProfileId = bcProfile.Id,
                LocationName = model.PickupLocation.LocationName,
                AddressLine1 = model.PickupLocation.AddressLine1,
                AddressLine2 = model.PickupLocation.AddressLine2,
                City = model.PickupLocation.City,
                State = model.PickupLocation.State,
                Pincode = model.PickupLocation.Pincode,
                ContactName = model.PickupLocation.ContactName ?? model.BusinessInfo.ContactPersonName,
                ContactPhone = model.PickupLocation.ContactPhone,
                Latitude = model.PickupLocation.Latitude,
                Longitude = model.PickupLocation.Longitude,
                IsDefault = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BCPickupLocations.Add(pickupLocation);

            // Get subscription plan details
            var plan = await _context.SubscriptionPlans.FindAsync(model.Subscription.SelectedPlanId);
            if (plan != null)
            {
                // Create User Subscription
                var subscription = new UserSubscription
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PlanId = plan.Id,
                    Status = "ACTIVE",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(plan.BillingCycle == "YEARLY" ? 12 : (plan.BillingCycle == "QUARTERLY" ? 3 : 1)),
                    NextBillingDate = DateTime.UtcNow.AddMonths(plan.BillingCycle == "YEARLY" ? 12 : (plan.BillingCycle == "QUARTERLY" ? 3 : 1)),
                    AutoRenew = true,
                    DeliveriesUsed = 0,
                    DeliveryQuota = plan.DeliveryQuota,
                    AmountPaid = plan.DiscountedPrice ?? plan.Price,
                    PaymentMethod = "PENDING", // Will be updated after payment
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserSubscriptions.Add(subscription);
            }

            // Update user email and mark profile complete
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Email = model.BusinessInfo.Email;
                user.FullName = model.BusinessInfo.ContactPersonName;
                user.UpdatedAt = DateTime.UtcNow;
            }

            // Verify PAN
            try
            {
                await _panService.VerifyPANAsync(userId, model.BusinessInfo.PAN, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PAN verification failed for BC {UserId}", userId);
            }

            await _context.SaveChangesAsync();

            ClearSession();

            _logger.LogInformation("BC registration completed for user {UserId} with subscription {PlanId}",
                userId, model.Subscription.SelectedPlanId);

            TempData["Success"] = "Registration completed successfully! Welcome to DeliveryDost.";
            return RedirectToAction("Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BC registration failed for user {UserId}", userId);
            ModelState.AddModelError("", "Registration failed. Please try again.");
            model.CurrentStep = 4;
            await LoadSubscriptionPlans(model);
            return View("Register", model);
        }
    }

    /// <summary>
    /// Navigate to specific step
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoToStep(BcRegistrationViewModel model, int step)
    {
        LoadFromSession(model);
        SaveToSession(model);
        model.CurrentStep = Math.Max(1, Math.Min(step, model.TotalSteps));
        await LoadSubscriptionPlans(model);
        return View("Register", model);
    }

    /// <summary>
    /// BC Dashboard
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var userId = GetUserId();

        try
        {
            var profile = await _context.BusinessConsumerProfiles
                .Include(bc => bc.SubscriptionPlan)
                .FirstOrDefaultAsync(bc => bc.UserId == userId);

            if (profile == null)
            {
                return RedirectToAction("Register");
            }

            var subscription = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId && s.Status == "ACTIVE")
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            var pickupCount = await _context.BCPickupLocations
                .CountAsync(pl => pl.BusinessConsumerProfileId == profile.Id && pl.IsActive);

            var deliveryStats = await _context.Deliveries
                .Where(d => d.RequesterId == userId)
                .GroupBy(d => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Active = g.Count(d => d.Status != "DELIVERED" && d.Status != "CANCELLED"),
                    ThisMonth = g.Count(d => d.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                })
                .FirstOrDefaultAsync();

            // Fetch wallet balance from database
            var wallet = await _context.Wallets
                .Where(w => w.UserId == userId && w.IsActive)
                .FirstOrDefaultAsync();

            var model = new BcDashboardViewModel
            {
                UserId = userId,
                BusinessName = profile.BusinessName,
                ContactPersonName = profile.ContactPersonName,
                SubscriptionPlanName = subscription?.Plan?.Name,
                SubscriptionExpiry = subscription?.EndDate,
                DeliveriesUsed = subscription?.DeliveriesUsed ?? 0,
                DeliveryQuota = subscription?.DeliveryQuota ?? 0,
                TotalDeliveries = deliveryStats?.Total ?? 0,
                ActiveDeliveries = deliveryStats?.Active ?? 0,
                CompletedThisMonth = deliveryStats?.ThisMonth ?? 0,
                PickupLocationCount = pickupCount,
                WalletBalance = wallet?.Balance ?? 0
            };

            ViewData["Title"] = "Business Dashboard";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading BC dashboard for user {UserId}", userId);
            TempData["Error"] = "Unable to load dashboard";
            return View(new BcDashboardViewModel { UserId = userId });
        }
    }

    /// <summary>
    /// Manage Pickup Locations
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> PickupLocations()
    {
        var userId = GetUserId();

        var profile = await _context.BusinessConsumerProfiles
            .FirstOrDefaultAsync(bc => bc.UserId == userId);

        if (profile == null)
        {
            return RedirectToAction("Register");
        }

        var locations = await _context.BCPickupLocations
            .Where(pl => pl.BusinessConsumerProfileId == profile.Id)
            .OrderByDescending(pl => pl.IsDefault)
            .ThenBy(pl => pl.LocationName)
            .ToListAsync();

        ViewData["Title"] = "Pickup Locations";
        return View(locations);
    }

    /// <summary>
    /// Add Pickup Location (GET)
    /// </summary>
    [HttpGet]
    public IActionResult AddPickupLocation()
    {
        ViewData["Title"] = "Add Pickup Location";
        return View(new BcPickupLocationViewModel());
    }

    /// <summary>
    /// Add Pickup Location (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPickupLocation(BcPickupLocationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Add Pickup Location";
            return View(model);
        }

        var userId = GetUserId();

        var profile = await _context.BusinessConsumerProfiles
            .FirstOrDefaultAsync(bc => bc.UserId == userId);

        if (profile == null)
        {
            return RedirectToAction("Register");
        }

        var location = new BCPickupLocation
        {
            Id = Guid.NewGuid(),
            BusinessConsumerProfileId = profile.Id,
            LocationName = model.LocationName,
            AddressLine1 = model.AddressLine1,
            AddressLine2 = model.AddressLine2,
            City = model.City,
            State = model.State,
            Pincode = model.Pincode,
            ContactName = model.ContactName,
            ContactPhone = model.ContactPhone,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            IsDefault = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.BCPickupLocations.Add(location);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Pickup location added successfully";
        return RedirectToAction("PickupLocations");
    }

    #region Private Helpers

    private async Task LoadSubscriptionPlans(BcRegistrationViewModel model)
    {
        var plans = await _context.SubscriptionPlans
            .Where(p => p.IsActive && (p.PlanType == "BC" || p.PlanType == "ALL"))
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        model.Subscription.AvailablePlans = plans.Select(p => new SubscriptionPlanOption
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            BillingCycle = p.BillingCycle,
            Price = p.Price,
            DiscountedPrice = p.DiscountedPrice,
            DeliveryQuota = p.DeliveryQuota,
            PerDeliveryDiscount = p.PerDeliveryDiscount,
            PrioritySupport = p.PrioritySupport,
            Features = string.IsNullOrEmpty(p.Features) ? new List<string>() :
                System.Text.Json.JsonSerializer.Deserialize<List<string>>(p.Features) ?? new List<string>(),
            IsPopular = p.Name.Contains("Pro", StringComparison.OrdinalIgnoreCase)
        }).ToList();
    }

    private const string SessionKey = "BcRegistration";

    private void SaveToSession(BcRegistrationViewModel model)
    {
        HttpContext.Session.SetString($"{SessionKey}_ContactPersonName", model.BusinessInfo.ContactPersonName ?? "");
        HttpContext.Session.SetString($"{SessionKey}_BusinessName", model.BusinessInfo.BusinessName ?? "");
        HttpContext.Session.SetString($"{SessionKey}_Email", model.BusinessInfo.Email ?? "");
        HttpContext.Session.SetString($"{SessionKey}_BusinessConstitution", model.BusinessInfo.BusinessConstitution ?? "");
        HttpContext.Session.SetString($"{SessionKey}_BusinessCategory", model.BusinessInfo.BusinessCategory ?? "");
        HttpContext.Session.SetString($"{SessionKey}_PAN", model.BusinessInfo.PAN ?? "");
        HttpContext.Session.SetString($"{SessionKey}_GSTIN", model.BusinessInfo.GSTIN ?? "");
        HttpContext.Session.SetString($"{SessionKey}_GSTRegistrationType", model.BusinessInfo.GSTRegistrationType ?? "");
        HttpContext.Session.SetString($"{SessionKey}_AddressLine1", model.BusinessInfo.AddressLine1 ?? "");
        HttpContext.Session.SetString($"{SessionKey}_AddressLine2", model.BusinessInfo.AddressLine2 ?? "");
        HttpContext.Session.SetString($"{SessionKey}_City", model.BusinessInfo.City ?? "");
        HttpContext.Session.SetString($"{SessionKey}_State", model.BusinessInfo.State ?? "");
        HttpContext.Session.SetString($"{SessionKey}_Pincode", model.BusinessInfo.Pincode ?? "");

        HttpContext.Session.SetString($"{SessionKey}_AccountHolderName", model.BankDetails.AccountHolderName ?? "");
        HttpContext.Session.SetString($"{SessionKey}_AccountNumber", model.BankDetails.AccountNumber ?? "");
        HttpContext.Session.SetString($"{SessionKey}_IFSCCode", model.BankDetails.IFSCCode ?? "");
        HttpContext.Session.SetString($"{SessionKey}_BankName", model.BankDetails.BankName ?? "");
        HttpContext.Session.SetString($"{SessionKey}_BranchName", model.BankDetails.BranchName ?? "");

        HttpContext.Session.SetString($"{SessionKey}_PickupLocationName", model.PickupLocation.LocationName ?? "");
        HttpContext.Session.SetString($"{SessionKey}_PickupAddressLine1", model.PickupLocation.AddressLine1 ?? "");
        HttpContext.Session.SetString($"{SessionKey}_PickupAddressLine2", model.PickupLocation.AddressLine2 ?? "");
        HttpContext.Session.SetString($"{SessionKey}_PickupCity", model.PickupLocation.City ?? "");
        HttpContext.Session.SetString($"{SessionKey}_PickupState", model.PickupLocation.State ?? "");
        HttpContext.Session.SetString($"{SessionKey}_PickupPincode", model.PickupLocation.Pincode ?? "");
        HttpContext.Session.SetString($"{SessionKey}_PickupContactName", model.PickupLocation.ContactName ?? "");
        HttpContext.Session.SetString($"{SessionKey}_PickupContactPhone", model.PickupLocation.ContactPhone ?? "");
    }

    private void LoadFromSession(BcRegistrationViewModel model)
    {
        model.BusinessInfo.ContactPersonName = HttpContext.Session.GetString($"{SessionKey}_ContactPersonName") ?? model.BusinessInfo.ContactPersonName;
        model.BusinessInfo.BusinessName = HttpContext.Session.GetString($"{SessionKey}_BusinessName") ?? model.BusinessInfo.BusinessName;
        model.BusinessInfo.Email = HttpContext.Session.GetString($"{SessionKey}_Email") ?? model.BusinessInfo.Email;
        model.BusinessInfo.BusinessConstitution = HttpContext.Session.GetString($"{SessionKey}_BusinessConstitution") ?? model.BusinessInfo.BusinessConstitution;
        model.BusinessInfo.BusinessCategory = HttpContext.Session.GetString($"{SessionKey}_BusinessCategory") ?? model.BusinessInfo.BusinessCategory;
        model.BusinessInfo.PAN = HttpContext.Session.GetString($"{SessionKey}_PAN") ?? model.BusinessInfo.PAN;
        model.BusinessInfo.GSTIN = HttpContext.Session.GetString($"{SessionKey}_GSTIN") ?? model.BusinessInfo.GSTIN;
        model.BusinessInfo.GSTRegistrationType = HttpContext.Session.GetString($"{SessionKey}_GSTRegistrationType") ?? model.BusinessInfo.GSTRegistrationType;
        model.BusinessInfo.AddressLine1 = HttpContext.Session.GetString($"{SessionKey}_AddressLine1") ?? model.BusinessInfo.AddressLine1;
        model.BusinessInfo.AddressLine2 = HttpContext.Session.GetString($"{SessionKey}_AddressLine2") ?? model.BusinessInfo.AddressLine2;
        model.BusinessInfo.City = HttpContext.Session.GetString($"{SessionKey}_City") ?? model.BusinessInfo.City;
        model.BusinessInfo.State = HttpContext.Session.GetString($"{SessionKey}_State") ?? model.BusinessInfo.State;
        model.BusinessInfo.Pincode = HttpContext.Session.GetString($"{SessionKey}_Pincode") ?? model.BusinessInfo.Pincode;

        model.BankDetails.AccountHolderName = HttpContext.Session.GetString($"{SessionKey}_AccountHolderName") ?? model.BankDetails.AccountHolderName;
        model.BankDetails.AccountNumber = HttpContext.Session.GetString($"{SessionKey}_AccountNumber") ?? model.BankDetails.AccountNumber;
        model.BankDetails.IFSCCode = HttpContext.Session.GetString($"{SessionKey}_IFSCCode") ?? model.BankDetails.IFSCCode;
        model.BankDetails.BankName = HttpContext.Session.GetString($"{SessionKey}_BankName") ?? model.BankDetails.BankName;
        model.BankDetails.BranchName = HttpContext.Session.GetString($"{SessionKey}_BranchName") ?? model.BankDetails.BranchName;

        model.PickupLocation.LocationName = HttpContext.Session.GetString($"{SessionKey}_PickupLocationName") ?? model.PickupLocation.LocationName;
        model.PickupLocation.AddressLine1 = HttpContext.Session.GetString($"{SessionKey}_PickupAddressLine1") ?? model.PickupLocation.AddressLine1;
        model.PickupLocation.AddressLine2 = HttpContext.Session.GetString($"{SessionKey}_PickupAddressLine2") ?? model.PickupLocation.AddressLine2;
        model.PickupLocation.City = HttpContext.Session.GetString($"{SessionKey}_PickupCity") ?? model.PickupLocation.City;
        model.PickupLocation.State = HttpContext.Session.GetString($"{SessionKey}_PickupState") ?? model.PickupLocation.State;
        model.PickupLocation.Pincode = HttpContext.Session.GetString($"{SessionKey}_PickupPincode") ?? model.PickupLocation.Pincode;
        model.PickupLocation.ContactName = HttpContext.Session.GetString($"{SessionKey}_PickupContactName") ?? model.PickupLocation.ContactName;
        model.PickupLocation.ContactPhone = HttpContext.Session.GetString($"{SessionKey}_PickupContactPhone") ?? model.PickupLocation.ContactPhone;
    }

    private void ClearSession()
    {
        var keys = new[]
        {
            "ContactPersonName", "BusinessName", "Email", "BusinessConstitution", "BusinessCategory",
            "PAN", "GSTIN", "GSTRegistrationType", "AddressLine1", "AddressLine2", "City", "State", "Pincode",
            "AccountHolderName", "AccountNumber", "IFSCCode", "BankName", "BranchName",
            "PickupLocationName", "PickupAddressLine1", "PickupAddressLine2", "PickupCity", "PickupState",
            "PickupPincode", "PickupContactName", "PickupContactPhone"
        };

        foreach (var key in keys)
        {
            HttpContext.Session.Remove($"{SessionKey}_{key}");
        }
    }

    #endregion
}
