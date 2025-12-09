using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeliveryDost.Application.DTOs.DPCM;

#region DPCM Manual Registration (Admin Only)

/// <summary>
/// Request to manually register a new DPCM (Admin only)
/// </summary>
public class CreateDPCMRequest
{
    // Personal Info
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Indian phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required")]
    public DateTime DateOfBirth { get; set; }

    public string? Gender { get; set; }

    // Business Info
    public string? OrganizationName { get; set; }
    public string? BusinessPAN { get; set; }
    public string? GSTIN { get; set; }

    // Address
    [Required(ErrorMessage = "Address is required")]
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    [Required(ErrorMessage = "City is required")]
    public string City { get; set; } = string.Empty;
    [Required(ErrorMessage = "State is required")]
    public string State { get; set; } = string.Empty;
    [Required(ErrorMessage = "Pincode is required")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be 6 digits")]
    public string Pincode { get; set; } = string.Empty;

    // KYC
    [Required(ErrorMessage = "Personal PAN is required")]
    [RegularExpression(@"^[A-Z]{5}\d{4}[A-Z]$", ErrorMessage = "Invalid PAN format")]
    public string PersonalPAN { get; set; } = string.Empty;

    [RegularExpression(@"^\d{12}$", ErrorMessage = "Aadhaar must be 12 digits")]
    public string? AadhaarNumber { get; set; }

    // Bank Details
    [Required(ErrorMessage = "Account holder name is required")]
    public string AccountHolderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account number is required")]
    [RegularExpression(@"^\d{9,18}$", ErrorMessage = "Invalid account number")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "IFSC code is required")]
    [RegularExpression(@"^[A-Z]{4}0[A-Z0-9]{6}$", ErrorMessage = "Invalid IFSC format")]
    public string IFSCCode { get; set; } = string.Empty;

    public string? BankName { get; set; }
    public string? BranchName { get; set; }

    // Security Deposit
    [Required(ErrorMessage = "Security deposit amount is required")]
    [Range(0, 1000000, ErrorMessage = "Security deposit must be between 0 and 10 lakh")]
    public decimal SecurityDeposit { get; set; }

    public string? SecurityDepositTransactionRef { get; set; }

    // Agreement
    public string? AgreementDocumentUrl { get; set; }
    public string? AgreementVersion { get; set; }

    // Commission Configuration
    [Required(ErrorMessage = "Commission type is required")]
    public string CommissionType { get; set; } = "HYBRID"; // PERCENTAGE, FLAT, HYBRID

    [Range(0, 100, ErrorMessage = "Commission percentage must be between 0 and 100")]
    public decimal? CommissionPercentage { get; set; }

    [Range(0, 10000, ErrorMessage = "Min commission amount must be between 0 and 10000")]
    public decimal? MinCommissionAmount { get; set; }

    // Service Pincodes to assign
    public List<string> ServicePincodes { get; set; } = new();
}

/// <summary>
/// Response after DPCM creation
/// </summary>
public class CreateDPCMResponse
{
    public bool IsSuccess { get; set; }
    public Guid? DPCMId { get; set; }
    public Guid? UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public int PincodesAssigned { get; set; }
}

/// <summary>
/// Request to update DPCM details
/// </summary>
public class UpdateDPCMRequest
{
    public string? OrganizationName { get; set; }
    public string? ContactPersonName { get; set; }
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }

    // Security Deposit
    public decimal? SecurityDeposit { get; set; }
    public string? SecurityDepositStatus { get; set; }
    public string? SecurityDepositTransactionRef { get; set; }

    // Agreement
    public string? AgreementDocumentUrl { get; set; }
    public string? AgreementVersion { get; set; }

    // Commission
    public string? CommissionType { get; set; }
    public decimal? CommissionPercentage { get; set; }
    public decimal? MinCommissionAmount { get; set; }

    public bool? IsActive { get; set; }
}

#endregion

#region Pincode-DPCM Mapping

/// <summary>
/// Request to assign pincodes to a DPCM
/// </summary>
public class AssignPincodesToDPCMRequest
{
    [Required]
    public Guid DPCMId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one pincode is required")]
    public List<string> Pincodes { get; set; } = new();

    public bool OverrideExisting { get; set; } = false;
}

/// <summary>
/// Response after assigning pincodes
/// </summary>
public class AssignPincodesToDPCMResponse
{
    public bool IsSuccess { get; set; }
    public int TotalRequested { get; set; }
    public int SuccessfullyAssigned { get; set; }
    public int AlreadyAssignedToOthers { get; set; }
    public int AlreadyAssignedToSame { get; set; }
    public List<PincodeAssignmentResult> Results { get; set; } = new();
}

public class PincodeAssignmentResult
{
    public string Pincode { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ExistingDPCMName { get; set; }
}

/// <summary>
/// Request to unassign pincodes from a DPCM
/// </summary>
public class UnassignPincodesRequest
{
    [Required]
    public Guid DPCMId { get; set; }

    [Required]
    [MinLength(1)]
    public List<string> Pincodes { get; set; } = new();

    public string? Reason { get; set; }
}

/// <summary>
/// Get DPCM for a pincode
/// </summary>
public class GetDPCMByPincodeResponse
{
    public bool HasDPCM { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public Guid? DPCMId { get; set; }
    public string? DPCMName { get; set; }
    public string? OrganizationName { get; set; }
    public string? Phone { get; set; }
    public DateTime? AssignedAt { get; set; }
}

/// <summary>
/// Get pincodes assigned to a DPCM
/// </summary>
public class DPCMPincodeListResponse
{
    public Guid DPCMId { get; set; }
    public string DPCMName { get; set; } = string.Empty;
    public int TotalPincodes { get; set; }
    public List<DPCMPincodeItem> Pincodes { get; set; } = new();
}

public class DPCMPincodeItem
{
    public string Pincode { get; set; } = string.Empty;
    public string? StateName { get; set; }
    public string? DistrictName { get; set; }
    public DateTime AssignedAt { get; set; }
    public bool IsActive { get; set; }
}

#endregion

#region DPCM Commission Configuration

/// <summary>
/// Calculate DPCM commission for a delivery
/// Commission = Max(PercentageAmount, MinAmount) - "whichever is higher"
/// </summary>
public class CalculateDPCMCommissionRequest
{
    public Guid DPCMId { get; set; }
    public decimal DeliveryAmount { get; set; }
}

public class CalculateDPCMCommissionResponse
{
    public decimal DeliveryAmount { get; set; }
    public string CommissionType { get; set; } = string.Empty;
    public decimal CommissionPercentage { get; set; }
    public decimal MinCommissionAmount { get; set; }
    public decimal CalculatedPercentageAmount { get; set; }
    public decimal FinalCommission { get; set; }
    public string AppliedMethod { get; set; } = string.Empty; // "PERCENTAGE" or "MIN_AMOUNT"
    public decimal GSTAmount { get; set; } // 18% GST
    public decimal TotalWithGST { get; set; }
}

/// <summary>
/// Update DPCM commission configuration
/// </summary>
public class UpdateDPCMCommissionRequest
{
    [Required]
    public Guid DPCMId { get; set; }

    [Required]
    public string CommissionType { get; set; } = "HYBRID";

    [Range(0, 100)]
    public decimal CommissionPercentage { get; set; }

    [Range(0, 10000)]
    public decimal MinCommissionAmount { get; set; }

    public DateTime? EffectiveFrom { get; set; }
}

#endregion

#region DPCM List & Search

/// <summary>
/// Request to list DPCMs
/// </summary>
public class ListDPCMsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? Status { get; set; } // ACTIVE, INACTIVE
    public string? State { get; set; }
    public string? District { get; set; }
}

/// <summary>
/// DPCM list response
/// </summary>
public class ListDPCMsResponse
{
    public List<DPCMListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class DPCMListItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OrganizationName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? State { get; set; }
    public string? District { get; set; }
    public bool IsActive { get; set; }
    public int TotalPincodes { get; set; }
    public int TotalDPs { get; set; }
    public decimal SecurityDeposit { get; set; }
    public string? SecurityDepositStatus { get; set; }
    public bool HasAgreement { get; set; }
    public string? CommissionType { get; set; }
    public decimal? CommissionValue { get; set; }
    public DateTime CreatedAt { get; set; }
}

#endregion
