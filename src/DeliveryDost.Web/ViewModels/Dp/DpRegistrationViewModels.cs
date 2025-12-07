using System.ComponentModel.DataAnnotations;

namespace DeliveryDost.Web.ViewModels.Dp;

/// <summary>
/// Main DP Registration ViewModel (multi-step wizard)
/// </summary>
public class DpRegistrationViewModel
{
    public int CurrentStep { get; set; } = 1;
    public int TotalSteps { get; } = 5;

    // Step 1: Personal Info
    public PersonalInfoStepViewModel PersonalInfo { get; set; } = new();

    // Step 2: Vehicle Info
    public VehicleInfoStepViewModel VehicleInfo { get; set; } = new();

    // Step 3: Bank Details
    public BankDetailsStepViewModel BankDetails { get; set; } = new();

    // Step 4: KYC Documents
    public KycDocumentsStepViewModel KycDocuments { get; set; } = new();

    // Step 5: Service Area & Pricing
    public ServiceAreaStepViewModel ServiceArea { get; set; } = new();
}

/// <summary>
/// Step 1: Personal Information
/// </summary>
public class PersonalInfoStepViewModel
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email Address")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime? DOB { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [Display(Name = "Gender")]
    public string Gender { get; set; } = string.Empty;

    [Display(Name = "Profile Photo")]
    public string? ProfilePhotoUrl { get; set; }

    // Address
    [Required(ErrorMessage = "Address line 1 is required")]
    [StringLength(200, ErrorMessage = "Address line 1 cannot exceed 200 characters")]
    [Display(Name = "Address Line 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Address line 2 cannot exceed 200 characters")]
    [Display(Name = "Address Line 2")]
    public string? AddressLine2 { get; set; }

    [Required(ErrorMessage = "City is required")]
    [StringLength(50, ErrorMessage = "City name cannot exceed 50 characters")]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "State is required")]
    [StringLength(50, ErrorMessage = "State name cannot exceed 50 characters")]
    [Display(Name = "State")]
    public string State { get; set; } = string.Empty;

    [Required(ErrorMessage = "Pincode is required")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be 6 digits")]
    [Display(Name = "Pincode")]
    public string Pincode { get; set; } = string.Empty;

    [Display(Name = "Languages Known")]
    public List<string> Languages { get; set; } = new();
}

/// <summary>
/// Step 2: Vehicle Information
/// </summary>
public class VehicleInfoStepViewModel
{
    [Required(ErrorMessage = "Vehicle type is required")]
    [Display(Name = "Vehicle Type")]
    public string VehicleType { get; set; } = string.Empty;

    [Display(Name = "Vehicle Registration Number")]
    [RegularExpression(@"^[A-Z]{2}\d{2}[A-Z]{1,2}\d{4}$", ErrorMessage = "Invalid vehicle number format (e.g., MH12AB1234)")]
    public string? VehicleNumber { get; set; }

    [Display(Name = "Driving License Number")]
    public string? DrivingLicenseNumber { get; set; }

    [Display(Name = "Driving License Expiry")]
    [DataType(DataType.Date)]
    public DateTime? DrivingLicenseExpiry { get; set; }

    [Display(Name = "Vehicle RC Document")]
    public string? VehicleRCDocumentUrl { get; set; }

    [Display(Name = "Driving License Document")]
    public string? DrivingLicenseDocumentUrl { get; set; }

    // Available vehicle types
    public static List<SelectOption> VehicleTypes => new()
    {
        new("BICYCLE", "Bicycle"),
        new("TWO_WHEELER", "Two Wheeler (Bike/Scooter)"),
        new("THREE_WHEELER", "Three Wheeler (Auto)"),
        new("FOUR_WHEELER", "Four Wheeler (Car/Van)"),
        new("TRUCK", "Truck/Tempo")
    };
}

/// <summary>
/// Step 3: Bank Details for Payouts
/// </summary>
public class BankDetailsStepViewModel
{
    [Required(ErrorMessage = "Account holder name is required")]
    [StringLength(100, ErrorMessage = "Account holder name cannot exceed 100 characters")]
    [Display(Name = "Account Holder Name")]
    public string AccountHolderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account number is required")]
    [RegularExpression(@"^\d{9,18}$", ErrorMessage = "Account number must be between 9 and 18 digits")]
    [Display(Name = "Account Number")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm account number")]
    [Compare("AccountNumber", ErrorMessage = "Account numbers do not match")]
    [Display(Name = "Confirm Account Number")]
    public string ConfirmAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "IFSC code is required")]
    [RegularExpression(@"^[A-Z]{4}0[A-Z0-9]{6}$", ErrorMessage = "Invalid IFSC code format")]
    [Display(Name = "IFSC Code")]
    public string IFSCCode { get; set; } = string.Empty;

    // Auto-populated from IFSC lookup
    [Display(Name = "Bank Name")]
    public string? BankName { get; set; }

    [Display(Name = "Branch Name")]
    public string? BranchName { get; set; }
}

/// <summary>
/// Step 4: KYC Documents
/// </summary>
public class KycDocumentsStepViewModel
{
    // Aadhaar
    [Display(Name = "Aadhaar Number (Last 4 digits)")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Enter last 4 digits of Aadhaar")]
    public string? AadhaarLast4 { get; set; }

    [Display(Name = "Aadhaar Verification Method")]
    public string AadhaarMethod { get; set; } = "DIGILOCKER"; // MANUAL_UPLOAD, DIGILOCKER

    [Display(Name = "Aadhaar Document (Front)")]
    public string? AadhaarFrontUrl { get; set; }

    [Display(Name = "Aadhaar Document (Back)")]
    public string? AadhaarBackUrl { get; set; }

    // PAN
    [Required(ErrorMessage = "PAN number is required")]
    [RegularExpression(@"^[A-Z]{5}\d{4}[A-Z]$", ErrorMessage = "Invalid PAN format (e.g., ABCDE1234F)")]
    [Display(Name = "PAN Number")]
    public string PAN { get; set; } = string.Empty;

    [Display(Name = "Name as per PAN")]
    public string? NameAsPerPan { get; set; }

    [Display(Name = "PAN Card Document")]
    public string? PANDocumentUrl { get; set; }

    // Status (read-only, populated from backend)
    public string? AadhaarStatus { get; set; }
    public string? PANStatus { get; set; }
    public string? BankStatus { get; set; }

    public static List<SelectOption> VerificationMethods => new()
    {
        new("DIGILOCKER", "DigiLocker (Recommended)"),
        new("MANUAL_UPLOAD", "Manual Upload")
    };
}

/// <summary>
/// Step 5: Service Area and Pricing
/// </summary>
public class ServiceAreaStepViewModel
{
    [Required(ErrorMessage = "Service area center latitude is required")]
    [Range(-90, 90, ErrorMessage = "Invalid latitude")]
    [Display(Name = "Center Latitude")]
    public decimal CenterLat { get; set; }

    [Required(ErrorMessage = "Service area center longitude is required")]
    [Range(-180, 180, ErrorMessage = "Invalid longitude")]
    [Display(Name = "Center Longitude")]
    public decimal CenterLng { get; set; }

    [Required(ErrorMessage = "Service radius is required")]
    [Range(1, 50, ErrorMessage = "Radius must be between 1 and 50 km")]
    [Display(Name = "Service Radius (km)")]
    public decimal RadiusKm { get; set; } = 5;

    [Display(Name = "Address")]
    public string? AddressText { get; set; }

    // Pricing
    [Required(ErrorMessage = "Per km rate is required")]
    [Range(1, 100, ErrorMessage = "Rate must be between Rs.1 and Rs.100")]
    [Display(Name = "Rate per km (Rs.)")]
    public decimal PerKmRate { get; set; } = 10;

    [Required(ErrorMessage = "Per kg rate is required")]
    [Range(1, 50, ErrorMessage = "Rate must be between Rs.1 and Rs.50")]
    [Display(Name = "Rate per kg (Rs.)")]
    public decimal PerKgRate { get; set; } = 5;

    [Required(ErrorMessage = "Minimum charge is required")]
    [Range(10, 500, ErrorMessage = "Minimum charge must be between Rs.10 and Rs.500")]
    [Display(Name = "Minimum Charge (Rs.)")]
    public decimal MinCharge { get; set; } = 30;

    [Display(Name = "Maximum Distance (km)")]
    [Range(1, 100, ErrorMessage = "Maximum distance must be between 1 and 100 km")]
    public decimal? MaxDistanceKm { get; set; }

    [Required(ErrorMessage = "Availability is required")]
    [Display(Name = "Availability")]
    public string Availability { get; set; } = "FULL_TIME";

    public static List<SelectOption> AvailabilityOptions => new()
    {
        new("FULL_TIME", "Full Time (8+ hours/day)"),
        new("PART_TIME", "Part Time (4-8 hours/day)"),
        new("WEEKENDS", "Weekends Only"),
        new("ON_DEMAND", "On Demand")
    };
}

/// <summary>
/// KYC Status View Model
/// </summary>
public class KycStatusViewModel
{
    public Guid UserId { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
    public bool CanActivate { get; set; }
    public List<string> PendingVerifications { get; set; } = new();
    public string? NextStep { get; set; }

    public KycVerificationItem AadhaarVerification { get; set; } = new();
    public KycVerificationItem PANVerification { get; set; } = new();
    public KycVerificationItem BankVerification { get; set; } = new();
    public KycVerificationItem? PoliceVerification { get; set; }
}

public class KycVerificationItem
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public DateTime? VerifiedAt { get; set; }
    public DateTime? InitiatedAt { get; set; }
    public string? ReferenceId { get; set; }
    public string? Message { get; set; }

    public string StatusBadgeClass => Status switch
    {
        "VERIFIED" => "bg-success",
        "PENDING" => "bg-warning",
        "REJECTED" => "bg-danger",
        "IN_PROGRESS" => "bg-info",
        _ => "bg-secondary"
    };
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
