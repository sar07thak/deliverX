using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeliveryDost.Web.ViewModels.Delivery;

/// <summary>
/// ViewModel for creating a new delivery
/// Enhanced with Group 3 features: Address Name, Alternate Contacts, Save Address, Caution
/// </summary>
public class CreateDeliveryViewModel
{
    // ====== PICKUP INFO ======
    [Display(Name = "Address Name")]
    [StringLength(100)]
    public string? PickupAddressName { get; set; } // e.g., "Home", "Office"

    [Required(ErrorMessage = "Pickup address is required")]
    [Display(Name = "Pickup Address")]
    public string PickupAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select pickup location on map")]
    [Range(-90, 90, ErrorMessage = "Invalid latitude")]
    public decimal PickupLat { get; set; }

    [Required(ErrorMessage = "Please select pickup location on map")]
    [Range(-180, 180, ErrorMessage = "Invalid longitude")]
    public decimal PickupLng { get; set; }

    [Display(Name = "Contact Name")]
    [StringLength(100)]
    public string? PickupContactName { get; set; }

    [Display(Name = "Contact Phone")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Indian phone number (must start with 6-9 and be 10 digits)")]
    public string? PickupContactPhone { get; set; }

    [Display(Name = "Alternate Phone")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Indian phone number")]
    public string? PickupAlternatePhone { get; set; }

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? PickupContactEmail { get; set; }

    [Display(Name = "WhatsApp Number")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid WhatsApp number")]
    public string? PickupWhatsAppNumber { get; set; }

    [Display(Name = "Pickup Instructions")]
    [StringLength(500)]
    public string? PickupInstructions { get; set; }

    // Save Address Option
    [Display(Name = "Save this address for future use")]
    public bool SavePickupAddress { get; set; } = false;

    public Guid? PickupSavedAddressId { get; set; }

    // ====== DROP INFO ======
    [Display(Name = "Address Name")]
    [StringLength(100)]
    public string? DropAddressName { get; set; } // e.g., "Home", "Office"

    [Required(ErrorMessage = "Drop address is required")]
    [Display(Name = "Drop Address")]
    public string DropAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select drop location on map")]
    [Range(-90, 90, ErrorMessage = "Invalid latitude")]
    public decimal DropLat { get; set; }

    [Required(ErrorMessage = "Please select drop location on map")]
    [Range(-180, 180, ErrorMessage = "Invalid longitude")]
    public decimal DropLng { get; set; }

    [Display(Name = "Recipient Name")]
    [StringLength(100)]
    public string? DropContactName { get; set; }

    [Required(ErrorMessage = "Recipient phone is required")]
    [Display(Name = "Recipient Phone")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Indian phone number (must start with 6-9 and be 10 digits)")]
    public string DropContactPhone { get; set; } = string.Empty;

    [Display(Name = "Alternate Phone")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Indian phone number")]
    public string? DropAlternatePhone { get; set; }

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? DropContactEmail { get; set; }

    [Display(Name = "WhatsApp Number")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid WhatsApp number")]
    public string? DropWhatsAppNumber { get; set; }

    [Display(Name = "Drop Instructions")]
    [StringLength(500)]
    public string? DropInstructions { get; set; }

    // Save Address Option
    [Display(Name = "Save this address for future use")]
    public bool SaveDropAddress { get; set; } = false;

    public Guid? DropSavedAddressId { get; set; }

    // ====== PACKAGE INFO ======
    [Required]
    [Range(0.1, 100, ErrorMessage = "Weight must be between 0.1 and 100 kg")]
    [Display(Name = "Weight (kg)")]
    public decimal WeightKg { get; set; } = 1;

    [Required]
    [Display(Name = "Package Type")]
    public string PackageType { get; set; } = "parcel";

    [Display(Name = "Package Value (₹)")]
    [Range(0, 1000000)]
    public decimal? PackageValue { get; set; }

    [Display(Name = "Package Description")]
    [StringLength(500)]
    public string? PackageDescription { get; set; }

    // ====== CAUTION/HAZARD INFO (Group 3) ======
    [Display(Name = "Contains hazardous items")]
    public bool IsHazardous { get; set; } = false;

    [Display(Name = "Caution Type")]
    public string? CautionType { get; set; }

    [Display(Name = "Caution Notes")]
    [StringLength(500)]
    public string? CautionNotes { get; set; }

    [Display(Name = "Requires special handling")]
    public bool RequiresSpecialHandling { get; set; } = false;

    // ====== SCHEDULING ======
    [Required]
    [Display(Name = "Priority")]
    public string Priority { get; set; } = "ASAP";

    [Display(Name = "Scheduled Time")]
    public DateTime? ScheduledAt { get; set; }

    [Display(Name = "Special Instructions")]
    [StringLength(1000)]
    public string? SpecialInstructions { get; set; }

    // Preferred DP (optional)
    public Guid? PreferredDPId { get; set; }

    // ====== CALCULATED FIELDS ======
    public decimal? EstimatedDistance { get; set; }
    public int? EstimatedDuration { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public string? DistanceSource { get; set; }

    // ====== SAVED ADDRESSES (for dropdown selection) ======
    public List<SavedAddressOption> SavedPickupAddresses { get; set; } = new();
    public List<SavedAddressOption> SavedDropAddresses { get; set; } = new();

    // ====== DROPDOWN OPTIONS ======
    public static List<SelectOption> PackageTypes => new()
    {
        new SelectOption("parcel", "Parcel / Box"),
        new SelectOption("food", "Food / Perishables"),
        new SelectOption("document", "Documents"),
        new SelectOption("fragile", "Fragile Items"),
        new SelectOption("electronics", "Electronics"),
        new SelectOption("medicine", "Medicines"),
        new SelectOption("clothing", "Clothing / Apparel"),
        new SelectOption("other", "Other")
    };

    public static List<SelectOption> PriorityOptions => new()
    {
        new SelectOption("ASAP", "ASAP - Deliver Now"),
        new SelectOption("SCHEDULED", "Schedule for Later")
    };

    public static List<SelectOption> CautionTypes => new()
    {
        new SelectOption("NONE", "No Special Caution"),
        new SelectOption("FRAGILE", "Fragile - Handle with Care"),
        new SelectOption("PERISHABLE", "Perishable - Time Sensitive"),
        new SelectOption("LIQUID", "Liquid - Keep Upright"),
        new SelectOption("GLASS", "Glass - Breakable"),
        new SelectOption("ELECTRONIC", "Electronics - Keep Dry"),
        new SelectOption("FLAMMABLE", "Flammable - No Fire"),
        new SelectOption("CHEMICAL", "Chemical - Handle Carefully"),
        new SelectOption("HEAVY", "Heavy - Use Proper Lifting"),
        new SelectOption("VALUABLE", "High Value - Extra Care")
    };

    public static List<SelectOption> AddressTypes => new()
    {
        new SelectOption("HOME", "Home"),
        new SelectOption("OFFICE", "Office"),
        new SelectOption("WAREHOUSE", "Warehouse"),
        new SelectOption("STORE", "Store/Shop"),
        new SelectOption("OTHER", "Other")
    };
}

/// <summary>
/// Saved address option for dropdown
/// </summary>
public class SavedAddressOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortAddress { get; set; } = string.Empty;
    public string AddressType { get; set; } = string.Empty;
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string? Instructions { get; set; }
    public bool IsDefault { get; set; }

    public string DisplayText => IsDefault ? $"{Name} (Default) - {ShortAddress}" : $"{Name} - {ShortAddress}";
}

/// <summary>
/// ViewModel for delivery list page
/// </summary>
public class DeliveryListViewModel
{
    public List<DeliveryListItemViewModel> Deliveries { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // Filters
    public string? StatusFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    // For display
    public string PageTitle { get; set; } = "Deliveries";
    public string ViewMode { get; set; } = "requester"; // requester, dp, admin

    public static List<SelectOption> StatusOptions => new()
    {
        new SelectOption("", "All Statuses"),
        new SelectOption("CREATED", "Created"),
        new SelectOption("MATCHING", "Finding DP"),
        new SelectOption("ASSIGNED", "Assigned"),
        new SelectOption("ACCEPTED", "Accepted"),
        new SelectOption("PICKED_UP", "Picked Up"),
        new SelectOption("IN_TRANSIT", "In Transit"),
        new SelectOption("DELIVERED", "Delivered"),
        new SelectOption("CANCELLED", "Cancelled")
    };
}

/// <summary>
/// Single delivery item in list
/// </summary>
public class DeliveryListItemViewModel
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PickupAddressName { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string? DropAddressName { get; set; }
    public string DropAddress { get; set; } = string.Empty;
    public decimal? EstimatedPrice { get; set; }
    public decimal? DistanceKm { get; set; }
    public string? AssignedDPName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Priority { get; set; } = string.Empty;
    public bool IsHazardous { get; set; }
    public string? CautionType { get; set; }

    public string StatusBadgeClass => Status switch
    {
        "CREATED" => "bg-secondary",
        "MATCHING" => "bg-info",
        "ASSIGNED" => "bg-primary",
        "ACCEPTED" => "bg-primary",
        "PICKED_UP" => "bg-warning text-dark",
        "IN_TRANSIT" => "bg-warning text-dark",
        "DELIVERED" => "bg-success",
        "CANCELLED" => "bg-danger",
        _ => "bg-secondary"
    };

    public string StatusIcon => Status switch
    {
        "CREATED" => "bi-plus-circle",
        "MATCHING" => "bi-search",
        "ASSIGNED" => "bi-person-check",
        "ACCEPTED" => "bi-check-circle",
        "PICKED_UP" => "bi-box-arrow-up",
        "IN_TRANSIT" => "bi-truck",
        "DELIVERED" => "bi-check2-all",
        "CANCELLED" => "bi-x-circle",
        _ => "bi-circle"
    };

    public string CautionBadgeClass => CautionType switch
    {
        "FRAGILE" => "bg-warning text-dark",
        "FLAMMABLE" => "bg-danger",
        "PERISHABLE" => "bg-info",
        "CHEMICAL" => "bg-danger",
        _ => "bg-secondary"
    };
}

/// <summary>
/// ViewModel for delivery details page
/// </summary>
public class DeliveryDetailsViewModel
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Location Info
    public LocationViewModel Pickup { get; set; } = new();
    public LocationViewModel Drop { get; set; } = new();

    // Package Info
    public PackageViewModel Package { get; set; } = new();

    // Assigned DP
    public DPInfoViewModel? AssignedDP { get; set; }

    // Pricing
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public decimal? DistanceKm { get; set; }
    public string? DistanceSource { get; set; }
    public int? EstimatedDurationMinutes { get; set; }

    // Timeline
    public List<TimelineItemViewModel> Timeline { get; set; } = new();

    // Matching Candidates (if in MATCHING status)
    public List<MatchedDPViewModel> MatchedCandidates { get; set; } = new();

    // Actions available
    public bool CanCancel => Status is "CREATED" or "MATCHING" or "ASSIGNED";
    public bool CanTrack => Status is "PICKED_UP" or "IN_TRANSIT";
    public bool ShowMatchingProgress => Status == "MATCHING";

    public string StatusBadgeClass => Status switch
    {
        "CREATED" => "bg-secondary",
        "MATCHING" => "bg-info",
        "ASSIGNED" => "bg-primary",
        "ACCEPTED" => "bg-primary",
        "PICKED_UP" => "bg-warning text-dark",
        "IN_TRANSIT" => "bg-warning text-dark",
        "DELIVERED" => "bg-success",
        "CANCELLED" => "bg-danger",
        _ => "bg-secondary"
    };
}

public class LocationViewModel
{
    public string? AddressName { get; set; }
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string? Instructions { get; set; }
}

public class PackageViewModel
{
    public decimal WeightKg { get; set; }
    public string Type { get; set; } = "parcel";
    public decimal? Value { get; set; }
    public string? Description { get; set; }
    public bool IsHazardous { get; set; }
    public string? CautionType { get; set; }
    public string? CautionNotes { get; set; }
    public bool RequiresSpecialHandling { get; set; }

    public string TypeDisplay => Type switch
    {
        "parcel" => "Parcel / Box",
        "food" => "Food / Perishables",
        "document" => "Documents",
        "fragile" => "Fragile Items",
        "electronics" => "Electronics",
        "medicine" => "Medicines",
        "clothing" => "Clothing / Apparel",
        _ => Type
    };

    public string CautionDisplay => CautionType switch
    {
        "FRAGILE" => "Fragile - Handle with Care",
        "PERISHABLE" => "Perishable - Time Sensitive",
        "LIQUID" => "Liquid - Keep Upright",
        "GLASS" => "Glass - Breakable",
        "ELECTRONIC" => "Electronics - Keep Dry",
        "FLAMMABLE" => "Flammable - No Fire",
        "CHEMICAL" => "Chemical - Handle Carefully",
        "HEAVY" => "Heavy - Use Proper Lifting",
        "VALUABLE" => "High Value - Extra Care",
        _ => "No Special Caution"
    };
}

public class DPInfoViewModel
{
    public Guid DPId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }
    public decimal Rating { get; set; }
    public DateTime? AssignedAt { get; set; }
}

public class TimelineItemViewModel
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsCurrent { get; set; }

    public string IconClass => Status switch
    {
        "CREATED" => "bi-plus-circle",
        "MATCHING" => "bi-search",
        "ASSIGNED" => "bi-person-check",
        "ACCEPTED" => "bi-check-circle",
        "PICKED_UP" => "bi-box-arrow-up",
        "IN_TRANSIT" => "bi-truck",
        "DELIVERED" => "bi-check2-all",
        "CANCELLED" => "bi-x-circle",
        _ => "bi-circle"
    };
}

public class MatchedDPViewModel
{
    public Guid DPId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal Rating { get; set; }
    public decimal EstimatedPrice { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal MatchScore { get; set; }
    public string? Status { get; set; } // PENDING, NOTIFIED, ACCEPTED, REJECTED
}

/// <summary>
/// ViewModel for DP to view available deliveries
/// </summary>
public class AvailableDeliveriesViewModel
{
    public List<AvailableDeliveryItemViewModel> Deliveries { get; set; } = new();
    public bool IsOnline { get; set; }
    public DPAvailabilityViewModel Availability { get; set; } = new();
}

public class AvailableDeliveryItemViewModel
{
    public Guid Id { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string DropAddress { get; set; } = string.Empty;
    public decimal PickupLat { get; set; }
    public decimal PickupLng { get; set; }
    public decimal DropLat { get; set; }
    public decimal DropLng { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal EstimatedPrice { get; set; }
    public string PackageType { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MinutesRemaining { get; set; } // Time to accept
    public bool IsHazardous { get; set; }
    public string? CautionType { get; set; }

    // Bidding Support (Group 4)
    public decimal? CurrentBid { get; set; }
    public int BidCount { get; set; }
    public decimal MinBid { get; set; }
    public decimal MaxBid { get; set; }
}

public class DPAvailabilityViewModel
{
    public bool IsOnline { get; set; }
    public string Status { get; set; } = "OFFLINE"; // ONLINE, BUSY, OFFLINE
    public int ActiveDeliveries { get; set; }
    public int MaxConcurrentDeliveries { get; set; } = 3;
}

/// <summary>
/// Helper class for select options
/// </summary>
public class SelectOption
{
    public string Value { get; set; }
    public string Text { get; set; }

    public SelectOption(string value, string text)
    {
        Value = value;
        Text = text;
    }
}

#region Sprint 6: Delivery Lifecycle & POD

/// <summary>
/// ViewModel for active delivery tracking (DP view)
/// </summary>
public class ActiveDeliveryViewModel
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }

    // Locations
    public LocationViewModel Pickup { get; set; } = new();
    public LocationViewModel Drop { get; set; } = new();

    // Package
    public PackageViewModel Package { get; set; } = new();

    // Pricing
    public decimal? EstimatedEarning { get; set; }
    public decimal? DistanceKm { get; set; }

    // Current step info
    public string CurrentStep { get; set; } = string.Empty;
    public string NextAction { get; set; } = string.Empty;
    public string NextActionButton { get; set; } = string.Empty;
    public string NextActionClass { get; set; } = "btn-primary";

    // Navigation
    public string? GoogleMapsUrl => $"https://www.google.com/maps/dir/?api=1&destination={GetCurrentDestinationLat()},{GetCurrentDestinationLng()}";

    private decimal GetCurrentDestinationLat() => Status is "ACCEPTED" or "PICKED_UP" ? Pickup.Lat : Drop.Lat;
    private decimal GetCurrentDestinationLng() => Status is "ACCEPTED" or "PICKED_UP" ? Pickup.Lng : Drop.Lng;

    // Status helpers
    public bool CanMarkPickedUp => Status == "ACCEPTED";
    public bool CanStartTransit => Status == "PICKED_UP";
    public bool CanCompleteDelivery => Status == "IN_TRANSIT";

    public string StatusBadgeClass => Status switch
    {
        "ACCEPTED" => "bg-primary",
        "PICKED_UP" => "bg-warning text-dark",
        "IN_TRANSIT" => "bg-info",
        "DELIVERED" => "bg-success",
        _ => "bg-secondary"
    };

    // Step progress
    public List<DeliveryStepViewModel> Steps { get; set; } = new();
}

/// <summary>
/// Step in delivery workflow
/// </summary>
public class DeliveryStepViewModel
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// ViewModel for POD (Proof of Delivery) capture
/// </summary>
public class PodCaptureViewModel
{
    public Guid DeliveryId { get; set; }
    public string DropAddress { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public string RecipientPhone { get; set; } = string.Empty;

    // POD Options
    public string PodType { get; set; } = "OTP"; // OTP, PHOTO, SIGNATURE

    // OTP Verification
    [Display(Name = "Delivery OTP")]
    [StringLength(6, MinimumLength = 4)]
    public string? Otp { get; set; }

    public bool OtpSent { get; set; }
    public DateTime? OtpSentAt { get; set; }
    public int OtpExpiryMinutes { get; set; } = 10;

    // Photo POD
    [Display(Name = "Delivery Photo")]
    public string? PhotoBase64 { get; set; }

    [Display(Name = "Photo Notes")]
    [StringLength(500)]
    public string? PhotoNotes { get; set; }

    // Delivery Notes
    [Display(Name = "Delivery Notes")]
    [StringLength(500)]
    public string? DeliveryNotes { get; set; }

    // Recipient Feedback
    [Display(Name = "Recipient Available")]
    public bool RecipientPresent { get; set; } = true;

    [Display(Name = "Left at Door")]
    public bool LeftAtDoor { get; set; }

    // Helper
    public static List<SelectOption> PodTypeOptions => new()
    {
        new SelectOption("OTP", "OTP Verification"),
        new SelectOption("PHOTO", "Photo Proof"),
        new SelectOption("SIGNATURE", "Signature (if available)")
    };
}

/// <summary>
/// ViewModel for live tracking page
/// </summary>
public class LiveTrackingViewModel
{
    public Guid DeliveryId { get; set; }
    public string Status { get; set; } = string.Empty;

    // Locations
    public LocationViewModel Pickup { get; set; } = new();
    public LocationViewModel Drop { get; set; } = new();

    // Current DP location (if tracking)
    public decimal? CurrentLat { get; set; }
    public decimal? CurrentLng { get; set; }
    public DateTime? LastLocationUpdate { get; set; }

    // DP Info
    public DPInfoViewModel? AssignedDP { get; set; }

    // ETA
    public int? EstimatedMinutesRemaining { get; set; }
    public decimal? RemainingDistanceKm { get; set; }

    // Timeline
    public List<TimelineItemViewModel> Timeline { get; set; } = new();

    // Refresh interval in seconds
    public int RefreshIntervalSeconds { get; set; } = 10;

    // Status helpers
    public bool IsActive => Status is "PICKED_UP" or "IN_TRANSIT";
    public bool IsCompleted => Status == "DELIVERED";
    public string StatusMessage => Status switch
    {
        "ACCEPTED" => "Delivery partner is heading to pickup location",
        "PICKED_UP" => "Package has been picked up, heading to you",
        "IN_TRANSIT" => "Your package is on the way!",
        "DELIVERED" => "Package delivered successfully",
        _ => "Tracking not available"
    };
}

/// <summary>
/// Request model for status update
/// </summary>
public class UpdateStatusRequest
{
    public Guid DeliveryId { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public decimal? CurrentLat { get; set; }
    public decimal? CurrentLng { get; set; }
}

/// <summary>
/// Request model for POD submission
/// </summary>
public class SubmitPodRequest
{
    public Guid DeliveryId { get; set; }
    public string PodType { get; set; } = "OTP";
    public string? Otp { get; set; }
    public string? PhotoBase64 { get; set; }
    public string? PhotoNotes { get; set; }
    public string? DeliveryNotes { get; set; }
    public bool RecipientPresent { get; set; }
    public bool LeftAtDoor { get; set; }
}

#endregion
