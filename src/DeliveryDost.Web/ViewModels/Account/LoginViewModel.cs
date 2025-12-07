using System.ComponentModel.DataAnnotations;

namespace DeliveryDost.Web.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be 10 digits")]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a role")]
    [Display(Name = "Role")]
    public string Role { get; set; } = "EC";
}

public class VerifyOtpViewModel
{
    [Required(ErrorMessage = "Phone number is required")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
    [Display(Name = "OTP")]
    public string Otp { get; set; } = string.Empty;

    public string Role { get; set; } = "EC";
}

public class ProfileViewModel
{
    public Guid UserId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // For DP
    public string? FullName { get; set; }
    public string? VehicleType { get; set; }
    public string? VehicleNumber { get; set; }

    // For BC
    public string? BusinessName { get; set; }
    public string? BusinessType { get; set; }
}
