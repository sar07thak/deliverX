namespace DeliveryDost.Application.DTOs.Reports;

/// <summary>
/// Common request for paginated reports
/// </summary>
public class ReportRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string? StateCode { get; set; }
    public string? DistrictName { get; set; }
    public string? Pincode { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; } = true;
}

/// <summary>
/// Common paginated response
/// </summary>
public class ReportResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

#region End Consumer Report

/// <summary>
/// End Consumer Report Item - No Aadhaar Verification Required
/// Fields with [Encrypted] are shown masked, hover to reveal
/// </summary>
public class EndConsumerReportItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Encrypted fields - stored encrypted, shown masked in UI
    public string MobileNumber { get; set; } = string.Empty;
    public string MobileNumberMasked { get; set; } = string.Empty;
    public string? EmailId { get; set; }
    public string? EmailIdMasked { get; set; }

    public string? StateName { get; set; }
    public string? DistrictName { get; set; }
    public string? Pincode { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime DateOfJoining { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastServiceAccessDate { get; set; }
    public int TotalDeliveries { get; set; }
}

#endregion

#region Business Consumer Report

/// <summary>
/// Business Consumer Report Item
/// Fields with [Encrypted] shown masked, hover to reveal
/// Verification status shown as badge
/// </summary>
public class BusinessConsumerReportItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BusinessName { get; set; }

    // Personal PAN [with verification status] - Encrypted
    public string? PersonalPAN { get; set; }
    public string? PersonalPANMasked { get; set; }
    public string PersonalPANVerificationStatus { get; set; } = "NOT_VERIFIED";

    // Business PAN (if any) [with verification status] - Encrypted
    public string? BusinessPAN { get; set; }
    public string? BusinessPANMasked { get; set; }
    public string? BusinessPANVerificationStatus { get; set; }

    // Aadhaar [with verification status] - Encrypted
    public string? AadhaarNumber { get; set; }
    public string? AadhaarNumberMasked { get; set; }
    public string AadhaarVerificationStatus { get; set; } = "NOT_VERIFIED";

    // Mobile & Email - Encrypted
    public string MobileNumber { get; set; } = string.Empty;
    public string MobileNumberMasked { get; set; } = string.Empty;
    public string? EmailId { get; set; }
    public string? EmailIdMasked { get; set; }

    public string? StateName { get; set; }
    public string? DistrictName { get; set; }
    public string? Pincode { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime DateOfJoining { get; set; }
    public int NumberOfPickupLocations { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastServiceAccessDate { get; set; }

    // Additional business fields
    public string? GSTIN { get; set; }
    public string? BusinessCategory { get; set; }
    public string? SubscriptionPlanName { get; set; }
    public DateTime? SubscriptionExpiry { get; set; }
}

#endregion

#region Delivery Partner Report

/// <summary>
/// Delivery Partner Report Item
/// </summary>
public class DeliveryPartnerReportItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Personal PAN [with verification status] - Encrypted
    public string? PersonalPAN { get; set; }
    public string? PersonalPANMasked { get; set; }
    public string PersonalPANVerificationStatus { get; set; } = "NOT_VERIFIED";

    // Aadhaar [with verification status] - Encrypted
    public string? AadhaarNumber { get; set; }
    public string? AadhaarNumberMasked { get; set; }
    public string AadhaarVerificationStatus { get; set; } = "NOT_VERIFIED";

    // Mobile & Email - Encrypted
    public string MobileNumber { get; set; } = string.Empty;
    public string MobileNumberMasked { get; set; } = string.Empty;
    public string? EmailId { get; set; }
    public string? EmailIdMasked { get; set; }

    public string? StateName { get; set; }
    public string? DistrictName { get; set; }
    public string? Pincode { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime DateOfJoining { get; set; }

    // Service Area
    public string? ServiceAreaDescription { get; set; }
    public decimal? ServiceAreaRadiusKm { get; set; }

    // Delivery Rate (per kg per km basis)
    public decimal? PerKgRate { get; set; }
    public decimal? PerKmRate { get; set; }
    public decimal? MinCharge { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime? LastServiceAccessDate { get; set; }

    // Additional fields
    public string? VehicleType { get; set; }
    public int TotalDeliveriesCompleted { get; set; }
    public decimal? AverageRating { get; set; }
    public string? DPCMName { get; set; }
}

#endregion

#region DPCM Report

/// <summary>
/// DPCM (Delivery Partner Cluster Manager) Report Item
/// </summary>
public class DPCMReportItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BusinessName { get; set; }

    // Personal PAN [with verification status] - Encrypted
    public string? PersonalPAN { get; set; }
    public string? PersonalPANMasked { get; set; }
    public string PersonalPANVerificationStatus { get; set; } = "NOT_VERIFIED";

    // Business PAN (if any) [with verification status] - Encrypted
    public string? BusinessPAN { get; set; }
    public string? BusinessPANMasked { get; set; }
    public string? BusinessPANVerificationStatus { get; set; }

    // Aadhaar [with verification status] - Encrypted
    public string? AadhaarNumber { get; set; }
    public string? AadhaarNumberMasked { get; set; }
    public string AadhaarVerificationStatus { get; set; } = "NOT_VERIFIED";

    // Mobile & Email - Encrypted
    public string MobileNumber { get; set; } = string.Empty;
    public string MobileNumberMasked { get; set; } = string.Empty;
    public string? EmailId { get; set; }
    public string? EmailIdMasked { get; set; }

    public string? StateName { get; set; }
    public string? DistrictName { get; set; }
    public string? Pincode { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime DateOfJoining { get; set; }
    public int NumberOfPickupLocations { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastServiceAccessDate { get; set; }

    // DPCM specific - Counts in their area
    public int NumberOfBusinessUsersInArea { get; set; }
    public int NumberOfEndUsersInArea { get; set; }
    public int NumberOfDeliveryPartnersInArea { get; set; }

    // Additional fields
    public string? CommissionType { get; set; }
    public decimal? CommissionValue { get; set; }
    public string? ServiceRegions { get; set; }
    public decimal? SecurityDeposit { get; set; }
    public string? AgreementDocumentUrl { get; set; }
    public decimal TotalEarnings { get; set; }
    public int TotalDeliveriesManaged { get; set; }
}

#endregion

#region Helper Classes

/// <summary>
/// Helper class for masking sensitive data
/// </summary>
public static class DataMaskingHelper
{
    /// <summary>
    /// Masks phone number: 9876543210 -> ******3210
    /// </summary>
    public static string MaskPhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 4)
            return "****";
        return new string('*', phone.Length - 4) + phone.Substring(phone.Length - 4);
    }

    /// <summary>
    /// Masks email: test@example.com -> t***@e*****.com
    /// </summary>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "****@****.***";

        var parts = email.Split('@');
        var local = parts[0];
        var domain = parts[1];

        var maskedLocal = local.Length > 1
            ? local[0] + new string('*', Math.Min(local.Length - 1, 3))
            : local;

        var domainParts = domain.Split('.');
        var maskedDomain = domainParts[0].Length > 1
            ? domainParts[0][0] + new string('*', Math.Min(domainParts[0].Length - 1, 5))
            : domainParts[0];

        return $"{maskedLocal}@{maskedDomain}.{string.Join(".", domainParts.Skip(1))}";
    }

    /// <summary>
    /// Masks PAN: ABCDE1234F -> ****E1234*
    /// </summary>
    public static string MaskPAN(string? pan)
    {
        if (string.IsNullOrEmpty(pan) || pan.Length != 10)
            return "**********";
        return "****" + pan.Substring(4, 5) + "*";
    }

    /// <summary>
    /// Masks Aadhaar: 123456789012 -> ********9012
    /// </summary>
    public static string MaskAadhaar(string? aadhaar)
    {
        if (string.IsNullOrEmpty(aadhaar) || aadhaar.Length < 4)
            return "************";
        return new string('*', aadhaar.Length - 4) + aadhaar.Substring(aadhaar.Length - 4);
    }
}

#endregion
