using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeliveryDost.Web.ViewModels.ServiceArea;

/// <summary>
/// ViewModel for configuring service area with map
/// </summary>
public class ServiceAreaConfigureViewModel
{
    public Guid? ServiceAreaId { get; set; }

    [Required(ErrorMessage = "Please select a location on the map")]
    [Range(-90, 90, ErrorMessage = "Invalid latitude")]
    public decimal CenterLat { get; set; }

    [Required(ErrorMessage = "Please select a location on the map")]
    [Range(-180, 180, ErrorMessage = "Invalid longitude")]
    public decimal CenterLng { get; set; }

    [Required(ErrorMessage = "Please set a radius")]
    [Range(1, 50, ErrorMessage = "Radius must be between 1 and 50 km")]
    public decimal RadiusKm { get; set; } = 5;

    [StringLength(100, ErrorMessage = "Area name cannot exceed 100 characters")]
    public string? AreaName { get; set; }

    [Display(Name = "Allow deliveries outside area (drop only)")]
    public bool AllowDropOutsideArea { get; set; }

    /// <summary>
    /// Current active service area details (if exists)
    /// </summary>
    public ServiceAreaDisplayModel? CurrentArea { get; set; }

    /// <summary>
    /// Address search query for geocoding
    /// </summary>
    public string? SearchAddress { get; set; }
}

/// <summary>
/// ViewModel for displaying service area details
/// </summary>
public class ServiceAreaDisplayModel
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "CIRCLE";
    public decimal CenterLat { get; set; }
    public decimal CenterLng { get; set; }
    public decimal RadiusKm { get; set; }
    public string? AreaName { get; set; }
    public bool IsActive { get; set; }
    public bool AllowDropOutsideArea { get; set; }
    public string EstimatedCoverage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// ViewModel for service area list page
/// </summary>
public class ServiceAreaIndexViewModel
{
    public List<ServiceAreaDisplayModel> ServiceAreas { get; set; } = new();
    public bool HasActiveArea => ServiceAreas.Any(a => a.IsActive);
    public ServiceAreaDisplayModel? ActiveArea => ServiceAreas.FirstOrDefault(a => a.IsActive);
}

/// <summary>
/// ViewModel for pricing configuration
/// </summary>
public class PricingConfigureViewModel
{
    [Required(ErrorMessage = "Per km rate is required")]
    [Range(0.01, 1000, ErrorMessage = "Rate must be between ₹0.01 and ₹1000")]
    [Display(Name = "Rate per kilometer (₹)")]
    public decimal PerKmRate { get; set; } = 10;

    [Required(ErrorMessage = "Per kg rate is required")]
    [Range(0, 500, ErrorMessage = "Rate must be between ₹0 and ₹500")]
    [Display(Name = "Rate per kilogram (₹)")]
    public decimal PerKgRate { get; set; } = 2;

    [Required(ErrorMessage = "Minimum charge is required")]
    [Range(10, 5000, ErrorMessage = "Minimum charge must be between ₹10 and ₹5000")]
    [Display(Name = "Minimum charge (₹)")]
    public decimal MinCharge { get; set; } = 50;

    [Range(1, 100, ErrorMessage = "Maximum distance must be between 1 and 100 km")]
    [Display(Name = "Maximum delivery distance (km)")]
    public decimal MaxDistanceKm { get; set; } = 25;

    /// <summary>
    /// For display: estimated earnings based on typical deliveries
    /// </summary>
    public PricingEstimateModel? Estimates { get; set; }
}

/// <summary>
/// Helper model for pricing estimates
/// </summary>
public class PricingEstimateModel
{
    public decimal ShortDistance5Km { get; set; }
    public decimal MediumDistance10Km { get; set; }
    public decimal LongDistance20Km { get; set; }
}

/// <summary>
/// Combined ViewModel for Service Area + Pricing page
/// </summary>
public class ServiceAreaAndPricingViewModel
{
    public ServiceAreaConfigureViewModel ServiceArea { get; set; } = new();
    public PricingConfigureViewModel Pricing { get; set; } = new();

    /// <summary>
    /// Tab to show: "area" or "pricing"
    /// </summary>
    public string ActiveTab { get; set; } = "area";
}
