using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryDost.Application.DTOs.Registration;
using DeliveryDost.Application.Services;
using DeliveryDost.Infrastructure.Services;
using DeliveryDost.Web.ViewModels.Dp;

namespace DeliveryDost.Web.Controllers;

[Authorize(Roles = "DP")]
public class DpController : Controller
{
    private readonly IDPRegistrationService _registrationService;
    private readonly IAadhaarVerificationService _aadhaarService;
    private readonly IPANVerificationService _panService;
    private readonly IBankVerificationService _bankService;
    private readonly IDuplicateDetectionService _duplicateDetection;
    private readonly ILogger<DpController> _logger;

    public DpController(
        IDPRegistrationService registrationService,
        IAadhaarVerificationService aadhaarService,
        IPANVerificationService panService,
        IBankVerificationService bankService,
        IDuplicateDetectionService duplicateDetection,
        ILogger<DpController> logger)
    {
        _registrationService = registrationService;
        _aadhaarService = aadhaarService;
        _panService = panService;
        _bankService = bankService;
        _duplicateDetection = duplicateDetection;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    private bool IsProfileComplete() => User.FindFirst("ProfileComplete")?.Value == "True";

    /// <summary>
    /// Main registration wizard entry point
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (IsProfileComplete())
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var userId = GetUserId();

        // Check current KYC status to determine step
        var kycStatus = await _registrationService.GetKYCStatusAsync(userId);

        var model = new DpRegistrationViewModel
        {
            CurrentStep = DetermineCurrentStep(kycStatus)
        };

        // Load saved data from session if available
        LoadFromSession(model);

        ViewData["Title"] = "Complete Your Registration";
        return View(model);
    }

    /// <summary>
    /// Step 1: Save Personal Info
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePersonalInfo(DpRegistrationViewModel model)
    {
        // Validate only Step 1 fields
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

        // Save to session for later submission
        SaveToSession(model);

        // Move to next step
        model.CurrentStep = 2;
        return View("Register", model);
    }

    /// <summary>
    /// Step 2: Save Vehicle Info
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveVehicleInfo(DpRegistrationViewModel model)
    {
        // First validate the submitted vehicle info BEFORE loading session
        ModelState.Clear();
        if (!TryValidateModel(model.VehicleInfo, nameof(model.VehicleInfo)))
        {
            // Load other step data from session for display
            LoadFromSession(model);
            model.CurrentStep = 2;
            return View("Register", model);
        }

        // Load previous steps data from session
        var submittedVehicleInfo = model.VehicleInfo;
        LoadFromSession(model);
        model.VehicleInfo = submittedVehicleInfo;

        // Save to session
        SaveToSession(model);

        // Move to next step
        model.CurrentStep = 3;
        return View("Register", model);
    }

    /// <summary>
    /// Step 3: Save Bank Details
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBankDetails(DpRegistrationViewModel model)
    {
        // First validate the submitted bank details BEFORE loading session
        ModelState.Clear();
        if (!TryValidateModel(model.BankDetails, nameof(model.BankDetails)))
        {
            // Load other step data from session for display
            LoadFromSession(model);
            model.CurrentStep = 3;
            return View("Register", model);
        }

        // Load previous steps data from session (personal info, vehicle info)
        var submittedBankDetails = model.BankDetails; // Save submitted values
        LoadFromSession(model);
        model.BankDetails = submittedBankDetails; // Restore submitted bank details

        var userId = GetUserId();

        try
        {
            // Initiate bank verification
            var bankResult = await _bankService.VerifyBankAccountAsync(
                userId,
                model.BankDetails.AccountNumber,
                model.BankDetails.IFSCCode,
                model.BankDetails.AccountHolderName,
                "PENNY_DROP",
                CancellationToken.None);

            if (bankResult.IsSuccess)
            {
                TempData["Success"] = "Bank account verification initiated";
            }
            else
            {
                TempData["Warning"] = bankResult.ErrorMessage ?? "Bank verification pending";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bank verification failed for user {UserId}", userId);
            TempData["Warning"] = "Bank verification will be processed later";
        }

        // Save to session
        SaveToSession(model);

        // Move to next step
        model.CurrentStep = 4;
        return View("Register", model);
    }

    /// <summary>
    /// Step 4: Save KYC Documents
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveKycDocuments(DpRegistrationViewModel model)
    {
        // First validate the submitted KYC documents BEFORE loading session
        ModelState.Clear();
        if (!TryValidateModel(model.KycDocuments, nameof(model.KycDocuments)))
        {
            // Load other step data from session for display
            LoadFromSession(model);
            model.CurrentStep = 4;
            return View("Register", model);
        }

        // Load previous steps data from session
        var submittedKycDocuments = model.KycDocuments;
        LoadFromSession(model);
        model.KycDocuments = submittedKycDocuments;

        var userId = GetUserId();

        try
        {
            // Check for duplicate PAN using CheckDuplicatesAsync
            var duplicateCheck = await _duplicateDetection.CheckDuplicatesAsync(
                phone: null,
                aadhaarHash: null,
                pan: model.KycDocuments.PAN,
                bankAccountHash: null,
                CancellationToken.None);

            if (duplicateCheck.IsDuplicate)
            {
                ModelState.AddModelError("KycDocuments.PAN", "This PAN is already registered with another account");
                model.CurrentStep = 4;
                return View("Register", model);
            }

            // Initiate PAN verification
            var panResult = await _panService.VerifyPANAsync(
                userId,
                model.KycDocuments.PAN,
                CancellationToken.None);

            if (panResult.IsSuccess)
            {
                TempData["Success"] = "PAN verification initiated";
            }

            // Handle Aadhaar based on method
            if (model.KycDocuments.AadhaarMethod == "DIGILOCKER")
            {
                // Store that we need DigiLocker verification
                TempData["PendingDigiLocker"] = true;
            }
            else if (!string.IsNullOrEmpty(model.KycDocuments.AadhaarFrontUrl))
            {
                // Manual upload - initiate verification
                var aadhaarResult = await _aadhaarService.ManualVerificationAsync(
                    userId,
                    model.KycDocuments.AadhaarLast4 ?? "",
                    model.KycDocuments.AadhaarFrontUrl,
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KYC verification failed for user {UserId}", userId);
            TempData["Warning"] = "Some verifications will be processed later";
        }

        // Save to session
        SaveToSession(model);

        // Move to final step
        model.CurrentStep = 5;
        return View("Register", model);
    }

    /// <summary>
    /// Step 5: Save Service Area and Complete Registration
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteRegistration(DpRegistrationViewModel model)
    {
        // First validate the submitted service area BEFORE loading session
        ModelState.Clear();
        if (!TryValidateModel(model.ServiceArea, nameof(model.ServiceArea)))
        {
            // Load other step data from session for display
            LoadFromSession(model);
            model.CurrentStep = 5;
            return View("Register", model);
        }

        // Load previous steps data from session
        var submittedServiceArea = model.ServiceArea;
        LoadFromSession(model);
        model.ServiceArea = submittedServiceArea;

        var userId = GetUserId();

        try
        {
            // Complete profile with all collected data
            var profileRequest = new ProfileCompleteRequest
            {
                FullName = model.PersonalInfo.FullName,
                Email = model.PersonalInfo.Email,
                DOB = model.PersonalInfo.DOB ?? DateTime.MinValue,
                Gender = model.PersonalInfo.Gender,
                ProfilePhotoUrl = model.PersonalInfo.ProfilePhotoUrl,
                Address = new AddressDto
                {
                    Line1 = model.PersonalInfo.AddressLine1,
                    Line2 = model.PersonalInfo.AddressLine2,
                    City = model.PersonalInfo.City,
                    State = model.PersonalInfo.State,
                    Pincode = model.PersonalInfo.Pincode
                },
                VehicleType = model.VehicleInfo.VehicleType,
                Languages = model.PersonalInfo.Languages,
                Availability = model.ServiceArea.Availability,
                ServiceArea = new ServiceAreaDto
                {
                    CenterLat = model.ServiceArea.CenterLat,
                    CenterLng = model.ServiceArea.CenterLng,
                    RadiusKm = model.ServiceArea.RadiusKm
                },
                Pricing = new PricingDto
                {
                    PerKmRate = model.ServiceArea.PerKmRate,
                    PerKgRate = model.ServiceArea.PerKgRate,
                    MinCharge = model.ServiceArea.MinCharge,
                    MaxDistanceKm = model.ServiceArea.MaxDistanceKm
                }
            };

            var result = await _registrationService.CompleteProfileAsync(userId, profileRequest);

            // Clear session data
            ClearSession();

            _logger.LogInformation("DP registration completed for user {UserId}", userId);

            TempData["Success"] = "Registration completed! Your KYC documents are being verified.";
            return RedirectToAction("KycStatus");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete registration for user {UserId}", userId);
            ModelState.AddModelError("", "Failed to complete registration. Please try again.");
            model.CurrentStep = 5;
            return View("Register", model);
        }
    }

    /// <summary>
    /// Navigate back to a previous step
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult GoToStep(DpRegistrationViewModel model, int step)
    {
        LoadFromSession(model);

        // Merge any data from current form submission
        SaveToSession(model);

        model.CurrentStep = Math.Max(1, Math.Min(step, model.TotalSteps));
        return View("Register", model);
    }

    /// <summary>
    /// View KYC verification status
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> KycStatus()
    {
        var userId = GetUserId();

        try
        {
            var status = await _registrationService.GetKYCStatusAsync(userId);

            var model = new KycStatusViewModel
            {
                UserId = userId,
                OverallStatus = status.OverallStatus,
                CanActivate = status.CanActivate,
                PendingVerifications = status.PendingVerifications,
                NextStep = status.NextStep,
                AadhaarVerification = MapVerification("Aadhaar", status.Verifications.GetValueOrDefault("aadhaar")),
                PANVerification = MapVerification("PAN", status.Verifications.GetValueOrDefault("pan")),
                BankVerification = MapVerification("Bank Account", status.Verifications.GetValueOrDefault("bank"))
            };

            if (status.Verifications.ContainsKey("police"))
            {
                model.PoliceVerification = MapVerification("Police Verification", status.Verifications["police"]);
            }

            ViewData["Title"] = "KYC Status";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get KYC status for user {UserId}", userId);
            TempData["Error"] = "Failed to load KYC status";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    /// <summary>
    /// Initiate DigiLocker verification
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InitiateDigiLocker()
    {
        var userId = GetUserId();

        try
        {
            var redirectUrl = Url.Action("DigiLockerCallback", "Dp", null, Request.Scheme) ?? "";
            var result = await _aadhaarService.InitiateDigiLockerVerificationAsync(
                userId,
                redirectUrl,
                CancellationToken.None);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.RedirectUrl))
            {
                return Redirect(result.RedirectUrl);
            }

            TempData["Error"] = result.ErrorMessage ?? "Failed to initiate DigiLocker";
            return RedirectToAction("KycStatus");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DigiLocker initiation failed for user {UserId}", userId);
            TempData["Error"] = "Failed to initiate DigiLocker verification";
            return RedirectToAction("KycStatus");
        }
    }

    /// <summary>
    /// DigiLocker callback handler
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DigiLockerCallback([FromQuery] string? code, [FromQuery] string? state)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(code))
        {
            TempData["Error"] = "DigiLocker authorization was cancelled or failed";
            return RedirectToAction("KycStatus");
        }

        try
        {
            // Process the callback
            // In production, this would exchange the code for Aadhaar data
            TempData["Success"] = "DigiLocker verification submitted successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DigiLocker callback failed for user {UserId}", userId);
            TempData["Error"] = "Failed to process DigiLocker response";
        }

        return RedirectToAction("KycStatus");
    }

    #region Session Management

    private const string SessionKey = "DpRegistration";

    private void SaveToSession(DpRegistrationViewModel model)
    {
        HttpContext.Session.SetString($"{SessionKey}_Step", model.CurrentStep.ToString());

        // Personal Info
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

        // Vehicle Info
        HttpContext.Session.SetString($"{SessionKey}_VehicleType", model.VehicleInfo.VehicleType ?? "");
        HttpContext.Session.SetString($"{SessionKey}_VehicleNumber", model.VehicleInfo.VehicleNumber ?? "");

        // Bank Details
        HttpContext.Session.SetString($"{SessionKey}_AccountHolderName", model.BankDetails.AccountHolderName ?? "");
        HttpContext.Session.SetString($"{SessionKey}_AccountNumber", model.BankDetails.AccountNumber ?? "");
        HttpContext.Session.SetString($"{SessionKey}_ConfirmAccountNumber", model.BankDetails.ConfirmAccountNumber ?? "");
        HttpContext.Session.SetString($"{SessionKey}_IFSCCode", model.BankDetails.IFSCCode ?? "");
        HttpContext.Session.SetString($"{SessionKey}_BankName", model.BankDetails.BankName ?? "");
        HttpContext.Session.SetString($"{SessionKey}_BranchName", model.BankDetails.BranchName ?? "");

        // KYC
        HttpContext.Session.SetString($"{SessionKey}_PAN", model.KycDocuments.PAN ?? "");
        HttpContext.Session.SetString($"{SessionKey}_AadhaarMethod", model.KycDocuments.AadhaarMethod ?? "DIGILOCKER");

        // Service Area
        HttpContext.Session.SetString($"{SessionKey}_CenterLat", model.ServiceArea.CenterLat.ToString());
        HttpContext.Session.SetString($"{SessionKey}_CenterLng", model.ServiceArea.CenterLng.ToString());
        HttpContext.Session.SetString($"{SessionKey}_RadiusKm", model.ServiceArea.RadiusKm.ToString());
        HttpContext.Session.SetString($"{SessionKey}_PerKmRate", model.ServiceArea.PerKmRate.ToString());
        HttpContext.Session.SetString($"{SessionKey}_PerKgRate", model.ServiceArea.PerKgRate.ToString());
        HttpContext.Session.SetString($"{SessionKey}_MinCharge", model.ServiceArea.MinCharge.ToString());
        HttpContext.Session.SetString($"{SessionKey}_Availability", model.ServiceArea.Availability ?? "FULL_TIME");
    }

    private void LoadFromSession(DpRegistrationViewModel model)
    {
        // Helper to get session value only if not empty
        string GetSessionOrKeep(string key, string currentValue)
        {
            var sessionValue = HttpContext.Session.GetString($"{SessionKey}_{key}");
            return !string.IsNullOrEmpty(sessionValue) ? sessionValue : currentValue;
        }

        // Personal Info
        model.PersonalInfo.FullName = GetSessionOrKeep("FullName", model.PersonalInfo.FullName);
        model.PersonalInfo.Email = GetSessionOrKeep("Email", model.PersonalInfo.Email);
        var dobStr = HttpContext.Session.GetString($"{SessionKey}_DOB");
        if (!string.IsNullOrEmpty(dobStr) && DateTime.TryParse(dobStr, out var dob))
            model.PersonalInfo.DOB = dob;
        model.PersonalInfo.Gender = GetSessionOrKeep("Gender", model.PersonalInfo.Gender);
        model.PersonalInfo.AddressLine1 = GetSessionOrKeep("AddressLine1", model.PersonalInfo.AddressLine1);
        model.PersonalInfo.AddressLine2 = GetSessionOrKeep("AddressLine2", model.PersonalInfo.AddressLine2);
        model.PersonalInfo.City = GetSessionOrKeep("City", model.PersonalInfo.City);
        model.PersonalInfo.State = GetSessionOrKeep("State", model.PersonalInfo.State);
        model.PersonalInfo.Pincode = GetSessionOrKeep("Pincode", model.PersonalInfo.Pincode);

        // Vehicle Info
        model.VehicleInfo.VehicleType = GetSessionOrKeep("VehicleType", model.VehicleInfo.VehicleType);
        model.VehicleInfo.VehicleNumber = GetSessionOrKeep("VehicleNumber", model.VehicleInfo.VehicleNumber);

        // Bank Details
        model.BankDetails.AccountHolderName = GetSessionOrKeep("AccountHolderName", model.BankDetails.AccountHolderName);
        model.BankDetails.AccountNumber = GetSessionOrKeep("AccountNumber", model.BankDetails.AccountNumber);
        model.BankDetails.ConfirmAccountNumber = GetSessionOrKeep("ConfirmAccountNumber", model.BankDetails.ConfirmAccountNumber);
        model.BankDetails.IFSCCode = GetSessionOrKeep("IFSCCode", model.BankDetails.IFSCCode);
        model.BankDetails.BankName = GetSessionOrKeep("BankName", model.BankDetails.BankName);
        model.BankDetails.BranchName = GetSessionOrKeep("BranchName", model.BankDetails.BranchName);

        // KYC
        model.KycDocuments.PAN = GetSessionOrKeep("PAN", model.KycDocuments.PAN);
        model.KycDocuments.AadhaarMethod = GetSessionOrKeep("AadhaarMethod", model.KycDocuments.AadhaarMethod);

        // Service Area
        if (decimal.TryParse(HttpContext.Session.GetString($"{SessionKey}_CenterLat"), out var lat))
            model.ServiceArea.CenterLat = lat;
        if (decimal.TryParse(HttpContext.Session.GetString($"{SessionKey}_CenterLng"), out var lng))
            model.ServiceArea.CenterLng = lng;
        if (decimal.TryParse(HttpContext.Session.GetString($"{SessionKey}_RadiusKm"), out var radius))
            model.ServiceArea.RadiusKm = radius;
        if (decimal.TryParse(HttpContext.Session.GetString($"{SessionKey}_PerKmRate"), out var perKm))
            model.ServiceArea.PerKmRate = perKm;
        if (decimal.TryParse(HttpContext.Session.GetString($"{SessionKey}_PerKgRate"), out var perKg))
            model.ServiceArea.PerKgRate = perKg;
        if (decimal.TryParse(HttpContext.Session.GetString($"{SessionKey}_MinCharge"), out var minCharge))
            model.ServiceArea.MinCharge = minCharge;
        model.ServiceArea.Availability = GetSessionOrKeep("Availability", model.ServiceArea.Availability);
    }

    private void ClearSession()
    {
        var keys = new[]
        {
            "Step", "FullName", "Email", "DOB", "Gender",
            "AddressLine1", "AddressLine2", "City", "State", "Pincode",
            "VehicleType", "VehicleNumber",
            "AccountHolderName", "AccountNumber", "ConfirmAccountNumber", "IFSCCode", "BankName", "BranchName",
            "PAN", "AadhaarMethod",
            "CenterLat", "CenterLng", "RadiusKm", "PerKmRate", "PerKgRate", "MinCharge", "Availability"
        };

        foreach (var key in keys)
        {
            HttpContext.Session.Remove($"{SessionKey}_{key}");
        }
    }

    #endregion

    #region Helpers

    private int DetermineCurrentStep(KYCStatusResponse kycStatus)
    {
        // If any mandatory verification is complete, assume profile is done
        if (kycStatus.Verifications.Any())
        {
            // Check KYC status
            if (kycStatus.OverallStatus == "FULLY_VERIFIED")
                return 5; // All done, redirect

            // Check what's pending
            if (kycStatus.PendingVerifications.Contains("aadhaar") || kycStatus.PendingVerifications.Contains("pan"))
                return 4; // KYC step

            if (kycStatus.PendingVerifications.Contains("bank"))
                return 3; // Bank step

            return 5; // Service area step
        }

        // Start from beginning
        return 1;
    }

    private KycVerificationItem MapVerification(string name, VerificationStatusDto? status)
    {
        if (status == null)
        {
            return new KycVerificationItem
            {
                Name = name,
                Status = "NOT_STARTED"
            };
        }

        return new KycVerificationItem
        {
            Name = name,
            Status = status.Status,
            VerifiedAt = status.VerifiedAt,
            InitiatedAt = status.InitiatedAt,
            ReferenceId = status.ReferenceId,
            Message = status.Message
        };
    }

    #endregion
}
