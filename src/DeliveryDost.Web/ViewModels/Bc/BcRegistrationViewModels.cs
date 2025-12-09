using System.ComponentModel.DataAnnotations;

namespace DeliveryDost.Web.ViewModels.Bc;

/// <summary>
/// Business Consumer Registration ViewModel
/// 4-step registration: Business Info > Bank Details > Pickup Locations > Subscription Selection
/// </summary>
public class BcRegistrationViewModel
{
    public int CurrentStep { get; set; } = 1;
    public int TotalSteps { get; } = 4;

    // Step 1: Business Info
    public BcBusinessInfoViewModel BusinessInfo { get; set; } = new();

    // Step 2: Bank Details
    public BcBankDetailsViewModel BankDetails { get; set; } = new();

    // Step 3: Pickup Location
    public BcPickupLocationViewModel PickupLocation { get; set; } = new();

    // Step 4: Subscription Selection
    public BcSubscriptionViewModel Subscription { get; set; } = new();
}

/// <summary>
/// Step 1: Business Information
/// </summary>
public class BcBusinessInfoViewModel
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Contact Person Name")]
    public string ContactPersonName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Business name is required")]
    [StringLength(255)]
    [Display(Name = "Business Name")]
    public string BusinessName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    // Business Constitution dropdown
    [Required(ErrorMessage = "Business constitution is required")]
    [Display(Name = "Business Constitution")]
    public string BusinessConstitution { get; set; } = string.Empty;

    // Business Category
    [Display(Name = "Business Category")]
    public string? BusinessCategory { get; set; }

    // PAN
    [Required(ErrorMessage = "PAN is required")]
    [RegularExpression(@"^[A-Z]{5}\d{4}[A-Z]$", ErrorMessage = "Invalid PAN format")]
    [Display(Name = "Business PAN")]
    public string PAN { get; set; } = string.Empty;

    // GSTIN
    [Display(Name = "GSTIN")]
    [RegularExpression(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$", ErrorMessage = "Invalid GSTIN format")]
    public string? GSTIN { get; set; }

    // GST Registration Type dropdown
    [Display(Name = "GST Registration Type")]
    public string? GSTRegistrationType { get; set; }

    // Address
    [Required(ErrorMessage = "Address is required")]
    [StringLength(255)]
    [Display(Name = "Address Line 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(255)]
    [Display(Name = "Address Line 2")]
    public string? AddressLine2 { get; set; }

    [Required(ErrorMessage = "City is required")]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "State is required")]
    [Display(Name = "State")]
    public string State { get; set; } = string.Empty;

    [Required(ErrorMessage = "Pincode is required")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be 6 digits")]
    [Display(Name = "Pincode")]
    public string Pincode { get; set; } = string.Empty;

    // Dropdown options
    public static List<SelectOption> BusinessConstitutionOptions => new()
    {
        new("PROPRIETORSHIP", "Proprietorship"),
        new("PARTNERSHIP", "Partnership"),
        new("LLP", "Limited Liability Partnership (LLP)"),
        new("PRIVATE_LIMITED", "Private Limited Company"),
        new("PUBLIC_LIMITED", "Public Limited Company"),
        new("ONE_PERSON", "One Person Company (OPC)"),
        new("HUF", "Hindu Undivided Family (HUF)"),
        new("TRUST", "Trust"),
        new("SOCIETY", "Society"),
        new("OTHER", "Other")
    };

    public static List<SelectOption> GSTRegistrationTypeOptions => new()
    {
        new("REGULAR", "Regular"),
        new("COMPOSITION", "Composition"),
        new("UNREGISTERED", "Unregistered / Not Applicable")
    };

    public static List<SelectOption> BusinessCategoryOptions => new()
    {
        new("ECOMMERCE", "E-Commerce"),
        new("FOOD", "Food & Beverages"),
        new("PHARMA", "Pharmaceuticals"),
        new("ELECTRONICS", "Electronics"),
        new("FASHION", "Fashion & Apparel"),
        new("GROCERY", "Grocery & FMCG"),
        new("DOCUMENTS", "Documents & Courier"),
        new("MANUFACTURING", "Manufacturing"),
        new("RETAIL", "Retail"),
        new("OTHER", "Other")
    };
}

/// <summary>
/// Step 2: Bank Details
/// </summary>
public class BcBankDetailsViewModel
{
    [Required(ErrorMessage = "Account holder name is required")]
    [StringLength(100)]
    [Display(Name = "Account Holder Name")]
    public string AccountHolderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account number is required")]
    [RegularExpression(@"^\d{9,18}$", ErrorMessage = "Account number must be 9-18 digits")]
    [Display(Name = "Account Number")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm account number")]
    [Compare("AccountNumber", ErrorMessage = "Account numbers do not match")]
    [Display(Name = "Confirm Account Number")]
    public string ConfirmAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "IFSC code is required")]
    [RegularExpression(@"^[A-Z]{4}0[A-Z0-9]{6}$", ErrorMessage = "Invalid IFSC format")]
    [Display(Name = "IFSC Code")]
    public string IFSCCode { get; set; } = string.Empty;

    [Display(Name = "Bank Name")]
    public string? BankName { get; set; }

    [Display(Name = "Branch Name")]
    public string? BranchName { get; set; }
}

/// <summary>
/// Step 3: Pickup Location
/// </summary>
public class BcPickupLocationViewModel
{
    [Required(ErrorMessage = "Location name is required")]
    [StringLength(100)]
    [Display(Name = "Location Name")]
    public string LocationName { get; set; } = "Main Office";

    [Required(ErrorMessage = "Address is required")]
    [StringLength(255)]
    [Display(Name = "Address Line 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(255)]
    [Display(Name = "Address Line 2")]
    public string? AddressLine2 { get; set; }

    [Required(ErrorMessage = "City is required")]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "State is required")]
    [Display(Name = "State")]
    public string State { get; set; } = string.Empty;

    [Required(ErrorMessage = "Pincode is required")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be 6 digits")]
    [Display(Name = "Pincode")]
    public string Pincode { get; set; } = string.Empty;

    [Display(Name = "Contact Person")]
    public string? ContactName { get; set; }

    [Display(Name = "Contact Phone")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Indian phone number")]
    public string? ContactPhone { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

/// <summary>
/// Step 4: Subscription Selection
/// </summary>
public class BcSubscriptionViewModel
{
    [Required(ErrorMessage = "Please select a subscription plan")]
    [Display(Name = "Subscription Plan")]
    public Guid SelectedPlanId { get; set; }

    public string? PromoCode { get; set; }

    // Available plans - populated from database
    public List<SubscriptionPlanOption> AvailablePlans { get; set; } = new();
}

/// <summary>
/// Subscription plan display model
/// </summary>
public class SubscriptionPlanOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public int DeliveryQuota { get; set; }
    public decimal? PerDeliveryDiscount { get; set; }
    public bool PrioritySupport { get; set; }
    public List<string> Features { get; set; } = new();
    public bool IsPopular { get; set; }
}

/// <summary>
/// Generic select option helper
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

/// <summary>
/// BC Dashboard Overview
/// </summary>
public class BcDashboardViewModel
{
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;

    // Subscription Info
    public string? SubscriptionPlanName { get; set; }
    public DateTime? SubscriptionExpiry { get; set; }
    public int DeliveriesUsed { get; set; }
    public int DeliveryQuota { get; set; }

    // Stats
    public int TotalDeliveries { get; set; }
    public int ActiveDeliveries { get; set; }
    public int CompletedThisMonth { get; set; }
    public decimal TotalSpent { get; set; }

    // Wallet
    public decimal WalletBalance { get; set; }

    // Pickup Locations
    public int PickupLocationCount { get; set; }
}
