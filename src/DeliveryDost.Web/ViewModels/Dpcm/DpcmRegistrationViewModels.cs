using System.ComponentModel.DataAnnotations;

namespace DeliveryDost.Web.ViewModels.Dpcm;

/// <summary>
/// DPCM (Delivery Partner Channel Manager) Registration ViewModel
/// Simpler than DP registration - no vehicle/delivery info needed
/// </summary>
public class DpcmRegistrationViewModel
{
    public int CurrentStep { get; set; } = 1;
    public int TotalSteps { get; } = 3;

    // Step 1: Personal Info
    public DpcmPersonalInfoViewModel PersonalInfo { get; set; } = new();

    // Step 2: Bank Details
    public DpcmBankDetailsViewModel BankDetails { get; set; } = new();

    // Step 3: KYC Documents
    public DpcmKycViewModel KycDocuments { get; set; } = new();
}

/// <summary>
/// Step 1: Personal Information for DPCM
/// </summary>
public class DpcmPersonalInfoViewModel
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required for DPCM")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

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
    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
    [Display(Name = "Address Line 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(200)]
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

    // DPCM specific
    [Display(Name = "Operating Area")]
    public string? OperatingArea { get; set; }

    [Display(Name = "Target Number of DPs")]
    [Range(1, 1000, ErrorMessage = "Target must be between 1 and 1000")]
    public int TargetDPCount { get; set; } = 10;
}

/// <summary>
/// Step 2: Bank Details for DPCM Commission Payouts
/// </summary>
public class DpcmBankDetailsViewModel
{
    [Required(ErrorMessage = "Account holder name is required")]
    [StringLength(100)]
    [Display(Name = "Account Holder Name")]
    public string AccountHolderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account number is required")]
    [RegularExpression(@"^\d{9,18}$", ErrorMessage = "Account number must be between 9 and 18 digits")]
    [Display(Name = "Account Number")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm account number")]
    [Compare("AccountNumber", ErrorMessage = "Account numbers do not match")]
    [Display(Name = "Confirm Account Number")]
    public string ConfirmAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "IFSC code is required")]
    [RegularExpression(@"^[A-Z]{4}0[A-Z0-9]{6}$", ErrorMessage = "Invalid IFSC code format")]
    [Display(Name = "IFSC Code")]
    public string IFSCCode { get; set; } = string.Empty;

    [Display(Name = "Bank Name")]
    public string? BankName { get; set; }

    [Display(Name = "Branch Name")]
    public string? BranchName { get; set; }
}

/// <summary>
/// Step 3: KYC for DPCM
/// </summary>
public class DpcmKycViewModel
{
    // Aadhaar
    [Display(Name = "Aadhaar Number (Last 4 digits)")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Enter last 4 digits of Aadhaar")]
    public string? AadhaarLast4 { get; set; }

    [Display(Name = "Verification Method")]
    public string AadhaarMethod { get; set; } = "DIGILOCKER";

    [Display(Name = "Aadhaar Document (Front)")]
    public string? AadhaarFrontUrl { get; set; }

    [Display(Name = "Aadhaar Document (Back)")]
    public string? AadhaarBackUrl { get; set; }

    // PAN
    [Required(ErrorMessage = "PAN number is required")]
    [RegularExpression(@"^[A-Z]{5}\d{4}[A-Z]$", ErrorMessage = "Invalid PAN format")]
    [Display(Name = "PAN Number")]
    public string PAN { get; set; } = string.Empty;

    [Display(Name = "Name as per PAN")]
    public string? NameAsPerPan { get; set; }

    // Verification statuses
    public string? AadhaarStatus { get; set; }
    public string? PANStatus { get; set; }
    public string? BankStatus { get; set; }
}

/// <summary>
/// DPCM Dashboard Overview
/// </summary>
public class DpcmDashboardViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ReferralCode { get; set; } = string.Empty;

    // Stats
    public int TotalDPs { get; set; }
    public int ActiveDPs { get; set; }
    public int PendingKYC { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal MonthlyCommission { get; set; }

    // Recent DPs
    public List<DPSummaryItem> RecentDPs { get; set; } = new();

    // Pending Actions
    public List<PendingActionItem> PendingActions { get; set; } = new();
}

public class DPSummaryItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsOnline { get; set; }
    public string KycStatus { get; set; } = "PENDING";
    public DateTime JoinedAt { get; set; }
    public int DeliveriesCompleted { get; set; }
    public int TotalDeliveries { get; set; }
    public decimal Rating { get; set; }
    public decimal TotalEarnings { get; set; }
}

public class PendingActionItem
{
    public string Type { get; set; } = string.Empty; // KYC_REVIEW, DP_ISSUE, etc.
    public string Description { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
