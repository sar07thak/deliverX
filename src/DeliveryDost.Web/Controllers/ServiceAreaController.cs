using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliverX.Application.DTOs.ServiceArea;
using DeliverX.Application.Services;
using DeliveryDost.Web.ViewModels.ServiceArea;

namespace DeliveryDost.Web.Controllers;

/// <summary>
/// MVC Controller for managing service areas and pricing
/// </summary>
[Authorize(Roles = "DP,DPCM,Admin")]
public class ServiceAreaController : Controller
{
    private readonly IServiceAreaService _serviceAreaService;
    private readonly ILogger<ServiceAreaController> _logger;

    public ServiceAreaController(
        IServiceAreaService serviceAreaService,
        ILogger<ServiceAreaController> logger)
    {
        _serviceAreaService = serviceAreaService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    /// <summary>
    /// Display current service area with map
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();

        try
        {
            var response = await _serviceAreaService.GetServiceAreaAsync(userId);

            var model = new ServiceAreaIndexViewModel
            {
                ServiceAreas = response.ServiceAreas.Select(sa => new ServiceAreaDisplayModel
                {
                    Id = sa.Id,
                    Type = sa.Type,
                    CenterLat = sa.CenterLat,
                    CenterLng = sa.CenterLng,
                    RadiusKm = sa.RadiusKm,
                    AreaName = sa.AreaName,
                    IsActive = sa.IsActive,
                    AllowDropOutsideArea = sa.AllowDropOutsideArea,
                    EstimatedCoverage = sa.EstimatedCoverage,
                    CreatedAt = sa.CreatedAt,
                    UpdatedAt = sa.UpdatedAt
                }).ToList()
            };

            ViewData["Title"] = "My Service Area";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service area for user {UserId}", userId);
            TempData["Error"] = "Failed to load service area";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    /// <summary>
    /// Configure/Edit service area with map
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Configure()
    {
        var userId = GetUserId();

        try
        {
            var response = await _serviceAreaService.GetServiceAreaAsync(userId);
            var activeArea = response.ServiceAreas.FirstOrDefault(a => a.IsActive);

            var model = new ServiceAreaConfigureViewModel();

            if (activeArea != null)
            {
                model.ServiceAreaId = activeArea.Id;
                model.CenterLat = activeArea.CenterLat;
                model.CenterLng = activeArea.CenterLng;
                model.RadiusKm = activeArea.RadiusKm;
                model.AreaName = activeArea.AreaName;
                model.AllowDropOutsideArea = activeArea.AllowDropOutsideArea;
                model.CurrentArea = new ServiceAreaDisplayModel
                {
                    Id = activeArea.Id,
                    CenterLat = activeArea.CenterLat,
                    CenterLng = activeArea.CenterLng,
                    RadiusKm = activeArea.RadiusKm,
                    AreaName = activeArea.AreaName,
                    IsActive = activeArea.IsActive,
                    EstimatedCoverage = activeArea.EstimatedCoverage
                };
            }
            else
            {
                // Default to Jaipur, India for new users
                model.CenterLat = 26.9124m;
                model.CenterLng = 75.7873m;
                model.RadiusKm = 5;
            }

            ViewData["Title"] = activeArea != null ? "Edit Service Area" : "Set Up Service Area";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service area config for user {UserId}", userId);
            TempData["Error"] = "Failed to load configuration";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Save service area configuration
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Configure(ServiceAreaConfigureViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = model.ServiceAreaId.HasValue ? "Edit Service Area" : "Set Up Service Area";
            return View(model);
        }

        var userId = GetUserId();

        try
        {
            var request = new SetServiceAreaRequest
            {
                CenterLat = model.CenterLat,
                CenterLng = model.CenterLng,
                RadiusKm = model.RadiusKm,
                AreaName = model.AreaName,
                AllowDropOutsideArea = model.AllowDropOutsideArea
            };

            var response = await _serviceAreaService.SetServiceAreaAsync(userId, request);

            _logger.LogInformation("Service area configured for user {UserId}: {ServiceAreaId}", userId, response.ServiceAreaId);

            TempData["Success"] = response.Message;
            return RedirectToAction("Index");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewData["Title"] = model.ServiceAreaId.HasValue ? "Edit Service Area" : "Set Up Service Area";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving service area for user {UserId}", userId);
            ModelState.AddModelError("", "Failed to save service area. Please try again.");
            ViewData["Title"] = model.ServiceAreaId.HasValue ? "Edit Service Area" : "Set Up Service Area";
            return View(model);
        }
    }

    /// <summary>
    /// Deactivate service area
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid serviceAreaId)
    {
        var userId = GetUserId();

        try
        {
            var success = await _serviceAreaService.DeactivateServiceAreaAsync(userId, serviceAreaId);

            if (success)
            {
                TempData["Success"] = "Service area deactivated successfully";
            }
            else
            {
                TempData["Error"] = "Service area not found";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating service area {ServiceAreaId}", serviceAreaId);
            TempData["Error"] = "Failed to deactivate service area";
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// API endpoint for checking coverage (AJAX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CheckCoverage([FromBody] CheckCoverageModel model)
    {
        try
        {
            var request = new CheckCoverageRequest
            {
                DPId = GetUserId(),
                PointLat = model.Lat,
                PointLng = model.Lng
            };

            var response = await _serviceAreaService.CheckCoverageAsync(request);

            return Json(new
            {
                isCovered = response.IsCovered,
                distanceKm = response.DistanceFromCenterKm,
                areaName = response.AreaName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking coverage");
            return Json(new { error = "Failed to check coverage" });
        }
    }

    /// <summary>
    /// Calculate distance between two points (AJAX)
    /// </summary>
    [HttpGet]
    public IActionResult CalculateDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        var distance = _serviceAreaService.CalculateDistanceKm(
            (double)lat1, (double)lng1,
            (double)lat2, (double)lng2);

        return Json(new
        {
            distanceKm = Math.Round(distance, 2),
            distanceM = Math.Round(distance * 1000, 0)
        });
    }
}

/// <summary>
/// Model for AJAX coverage check
/// </summary>
public class CheckCoverageModel
{
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
}
