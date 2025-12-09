using System;
using System.ComponentModel.DataAnnotations;

namespace DeliveryDost.Application.DTOs.SavedAddress;

/// <summary>
/// DTO for creating a new saved address
/// </summary>
public class CreateSavedAddressRequest
{
    [Required(ErrorMessage = "Address name is required")]
    [StringLength(100, MinimumLength = 1)]
    public string AddressName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address line 1 is required")]
    [StringLength(255)]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(255)]
    public string? AddressLine2 { get; set; }

    [Required(ErrorMessage = "City is required")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "State is required")]
    [StringLength(100)]
    public string State { get; set; } = string.Empty;

    [Required(ErrorMessage = "Pincode is required")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be 6 digits")]
    public string Pincode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Latitude is required")]
    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Required(ErrorMessage = "Longitude is required")]
    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    [StringLength(100)]
    public string? ContactName { get; set; }

    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Indian phone number")]
    public string? ContactPhone { get; set; }

    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Indian phone number")]
    public string? AlternatePhone { get; set; }

    [EmailAddress]
    public string? ContactEmail { get; set; }

    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid WhatsApp number")]
    public string? WhatsAppNumber { get; set; }

    public string AddressType { get; set; } = "OTHER"; // HOME, OFFICE, WAREHOUSE, OTHER

    public bool IsDefault { get; set; } = false;
    public bool IsPickupAddress { get; set; } = true;
    public bool IsDropAddress { get; set; } = true;

    [StringLength(500)]
    public string? DefaultInstructions { get; set; }

    [StringLength(255)]
    public string? Landmark { get; set; }
}

/// <summary>
/// DTO for updating a saved address
/// </summary>
public class UpdateSavedAddressRequest : CreateSavedAddressRequest
{
    public Guid Id { get; set; }
}

/// <summary>
/// DTO for saved address response
/// </summary>
public class SavedAddressDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AddressName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string FullAddress { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string AddressType { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsPickupAddress { get; set; }
    public bool IsDropAddress { get; set; }
    public string? DefaultInstructions { get; set; }
    public string? Landmark { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Display helper
    public string DisplayName => $"{AddressName} ({AddressType})";
    public string ShortAddress => $"{AddressLine1}, {City} - {Pincode}";
}

/// <summary>
/// DTO for listing saved addresses with minimal info
/// </summary>
public class SavedAddressListDto
{
    public Guid Id { get; set; }
    public string AddressName { get; set; } = string.Empty;
    public string AddressType { get; set; } = string.Empty;
    public string ShortAddress { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsPickupAddress { get; set; }
    public bool IsDropAddress { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
}

/// <summary>
/// Response for list of saved addresses
/// </summary>
public class GetSavedAddressesResponse
{
    public List<SavedAddressListDto> Addresses { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Static helper for address type options
/// </summary>
public static class AddressTypeOptions
{
    public static readonly List<(string Value, string Text)> Options = new()
    {
        ("HOME", "Home"),
        ("OFFICE", "Office"),
        ("WAREHOUSE", "Warehouse"),
        ("STORE", "Store/Shop"),
        ("FACTORY", "Factory"),
        ("OTHER", "Other")
    };
}
