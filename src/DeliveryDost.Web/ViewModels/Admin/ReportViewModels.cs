namespace DeliveryDost.Web.ViewModels.Admin;

/// <summary>
/// Base view model for all reports
/// </summary>
public class ReportFilterViewModel
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string? StateCode { get; set; }
    public string? DistrictName { get; set; }
    public string? Pincode { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; } = true;
}

#region End Consumer Report

public class EndConsumerReportViewModel : ReportFilterViewModel
{
    public List<EndConsumerReportItemViewModel> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class EndConsumerReportItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
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

public class BusinessConsumerReportViewModel : ReportFilterViewModel
{
    public List<BusinessConsumerReportItemViewModel> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class BusinessConsumerReportItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BusinessName { get; set; }

    // Personal PAN
    public string? PersonalPAN { get; set; }
    public string? PersonalPANMasked { get; set; }
    public string PersonalPANVerificationStatus { get; set; } = "NOT_VERIFIED";

    // Business PAN
    public string? BusinessPAN { get; set; }
    public string? BusinessPANMasked { get; set; }
    public string? BusinessPANVerificationStatus { get; set; }

    // Aadhaar
    public string? AadhaarNumber { get; set; }
    public string? AadhaarNumberMasked { get; set; }
    public string AadhaarVerificationStatus { get; set; } = "NOT_VERIFIED";

    // Contact
    public string MobileNumber { get; set; } = string.Empty;
    public string MobileNumberMasked { get; set; } = string.Empty;
    public string? EmailId { get; set; }
    public string? EmailIdMasked { get; set; }

    // Location
    public string? StateName { get; set; }
    public string? DistrictName { get; set; }
    public string? Pincode { get; set; }
    public string? Address { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public DateTime DateOfJoining { get; set; }
    public int NumberOfPickupLocations { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastServiceAccessDate { get; set; }

    // Business Details
    public string? GSTIN { get; set; }
    public string? BusinessCategory { get; set; }
    public string? SubscriptionPlanName { get; set; }
    public DateTime? SubscriptionExpiry { get; set; }
}

#endregion

#region Delivery Partner Report

public class DeliveryPartnerReportViewModel : ReportFilterViewModel
{
    public List<DeliveryPartnerReportItemViewModel> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class DeliveryPartnerReportItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Personal PAN
    public string? PersonalPAN { get; set; }
    public string? PersonalPANMasked { get; set; }
    public string PersonalPANVerificationStatus { get; set; } = "NOT_VERIFIED";

    // Aadhaar
    public string? AadhaarNumber { get; set; }
    public string? AadhaarNumberMasked { get; set; }
    public string AadhaarVerificationStatus { get; set; } = "NOT_VERIFIED";

    // Contact
    public string MobileNumber { get; set; } = string.Empty;
    public string MobileNumberMasked { get; set; } = string.Empty;
    public string? EmailId { get; set; }
    public string? EmailIdMasked { get; set; }

    // Location
    public string? StateName { get; set; }
    public string? DistrictName { get; set; }
    public string? Pincode { get; set; }
    public string? Address { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public DateTime DateOfJoining { get; set; }

    // Service Area
    public string? ServiceAreaDescription { get; set; }
    public decimal? ServiceAreaRadiusKm { get; set; }

    // Delivery Rates
    public decimal? PerKgRate { get; set; }
    public decimal? PerKmRate { get; set; }
    public decimal? MinCharge { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime? LastServiceAccessDate { get; set; }

    // Additional
    public string? VehicleType { get; set; }
    public int TotalDeliveriesCompleted { get; set; }
    public decimal? AverageRating { get; set; }
    public string? DPCMName { get; set; }
}

#endregion

#region DPCM Report

public class DPCMReportViewModel : ReportFilterViewModel
{
    public List<DPCMReportItemViewModel> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class DPCMReportItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BusinessName { get; set; }

    // Personal PAN
    public string? PersonalPAN { get; set; }
    public string? PersonalPANMasked { get; set; }
    public string PersonalPANVerificationStatus { get; set; } = "NOT_VERIFIED";

    // Business PAN
    public string? BusinessPAN { get; set; }
    public string? BusinessPANMasked { get; set; }
    public string? BusinessPANVerificationStatus { get; set; }

    // Aadhaar
    public string? AadhaarNumber { get; set; }
    public string? AadhaarNumberMasked { get; set; }
    public string AadhaarVerificationStatus { get; set; } = "NOT_VERIFIED";

    // Contact
    public string MobileNumber { get; set; } = string.Empty;
    public string MobileNumberMasked { get; set; } = string.Empty;
    public string? EmailId { get; set; }
    public string? EmailIdMasked { get; set; }

    // Location
    public string? StateName { get; set; }
    public string? DistrictName { get; set; }
    public string? Pincode { get; set; }
    public string? Address { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public DateTime DateOfJoining { get; set; }
    public int NumberOfPickupLocations { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastServiceAccessDate { get; set; }

    // DPCM Specific - Area Counts
    public int NumberOfBusinessUsersInArea { get; set; }
    public int NumberOfEndUsersInArea { get; set; }
    public int NumberOfDeliveryPartnersInArea { get; set; }

    // Commission & Earnings
    public string? CommissionType { get; set; }
    public decimal? CommissionValue { get; set; }
    public string? ServiceRegions { get; set; }
    public decimal? SecurityDeposit { get; set; }
    public string? AgreementDocumentUrl { get; set; }
    public decimal TotalEarnings { get; set; }
    public int TotalDeliveriesManaged { get; set; }
}

#endregion
