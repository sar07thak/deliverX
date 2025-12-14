using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryDost.Application.DTOs.Auth;
using DeliveryDost.Application.Services;
using DeliveryDost.Web.ViewModels.Account;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthService authService, ILogger<AccountController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendOtp(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View("Login", model);

        try
        {
            var result = await _authService.SendOtpAsync(new OtpSendRequest
            {
                Phone = model.Phone,
                Role = model.Role
            }, CancellationToken.None);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.ErrorMessage ?? "Failed to send OTP");
                return View("Login", model);
            }

            // Store in TempData for verification step
            TempData["Phone"] = model.Phone;
            TempData["Role"] = model.Role;
            TempData["ReturnUrl"] = returnUrl;
            TempData["OtpMessage"] = result.Data?.Message ?? "OTP sent successfully"; // Contains OTP in dev mode

            return RedirectToAction("VerifyOtp");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP for {Phone}", model.Phone);
            ModelState.AddModelError("", $"Error: {ex.Message}");
            return View("Login", model);
        }
    }

    [HttpGet]
    public IActionResult VerifyOtp()
    {
        var phone = TempData["Phone"]?.ToString();
        var role = TempData["Role"]?.ToString();

        if (string.IsNullOrEmpty(phone))
            return RedirectToAction("Login");

        // Keep TempData for the post request
        TempData.Keep("Phone");
        TempData.Keep("Role");
        TempData.Keep("ReturnUrl");

        ViewBag.OtpMessage = TempData["OtpMessage"];

        return View(new VerifyOtpViewModel
        {
            Phone = phone,
            Role = role ?? "EC"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var role = TempData["Role"]?.ToString() ?? model.Role;
        var returnUrl = TempData["ReturnUrl"]?.ToString();

        try
        {
            // Get client IP and User Agent for audit
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var result = await _authService.VerifyOtpAsync(new OtpVerifyRequest
            {
                Phone = model.Phone,
                Otp = model.Otp,
                Role = role,
                DeviceId = "web-browser"
            }, ipAddress, userAgent, CancellationToken.None);

            if (!result.IsSuccess || result.Data == null)
            {
                ModelState.AddModelError("", result.ErrorMessage ?? "Invalid OTP");
                TempData["Phone"] = model.Phone;
                TempData["Role"] = role;
                return View(model);
            }

            var tokenResponse = result.Data;
            var userId = tokenResponse.User?.Id ?? Guid.Empty;
            var profileComplete = tokenResponse.User?.ProfileComplete ?? false;

            // Create claims for cookie authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.MobilePhone, model.Phone),
                new Claim(ClaimTypes.Role, role),
                new Claim("ProfileComplete", profileComplete.ToString()),
                new Claim("AccessToken", tokenResponse.AccessToken ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            _logger.LogInformation("User {Phone} logged in successfully with role {Role}", model.Phone, role);

            // Redirect based on profile completion
            if (!profileComplete)
            {
                var controller = GetControllerForRole(role);
                if (controller != null)
                {
                    return RedirectToAction("Register", controller);
                }
                // For EC/BC/Admin - no special registration, go to dashboard
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for {Phone}", model.Phone);
            ModelState.AddModelError("", $"Error: {ex.Message}");
            TempData["Phone"] = model.Phone;
            TempData["Role"] = role;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var phone = User.FindFirst(ClaimTypes.MobilePhone)?.Value;
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User {Phone} logged out", phone);
        return RedirectToAction("Login");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var user = await _authService.GetUserProfileAsync(userId, CancellationToken.None);

        var model = new ProfileViewModel
        {
            UserId = userId,
            Phone = User.FindFirst(ClaimTypes.MobilePhone)?.Value ?? "",
            Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "",
            FullName = user?.FullName,
            Email = user?.Email,
            IsActive = user?.IsActive ?? true,
            CreatedAt = user?.CreatedAt ?? DateTime.UtcNow
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(string? fullName, string? email)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

        try
        {
            var request = new UpdateProfileRequest
            {
                FullName = fullName,
                Email = email
            };

            var result = await _authService.UpdateProfileAsync(userId, request, CancellationToken.None);

            if (result.IsSuccess)
            {
                TempData["Success"] = "Profile updated successfully";
            }
            else
            {
                TempData["Error"] = result.ErrorMessage ?? "Failed to update profile";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            TempData["Error"] = "An error occurred while updating profile";
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private string? GetControllerForRole(string role) => role switch
    {
        "DP" => "Dp",
        "DPCM" => "Dpcm",
        "BC" => "Bc",  // Business Consumer registration with subscription
        "DBC" => "Bc", // Digital Business Consumer also uses BC registration
        _ => null  // EC, Admin go directly to dashboard
    };
}
