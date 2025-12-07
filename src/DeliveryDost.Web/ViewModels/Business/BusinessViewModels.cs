using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DeliveryDost.Web.ViewModels.Business;

#region Bulk Delivery ViewModels

public class BulkDeliveryViewModel
{
    public List<BulkDeliveryItemViewModel> Deliveries { get; set; } = new();
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public List<BulkDeliveryError> Errors { get; set; } = new();
}

public class BulkUploadViewModel
{
    [Display(Name = "Upload CSV File")]
    public IFormFile? CsvFile { get; set; }

    public string? SampleCsvUrl => "/sample/bulk-delivery-template.csv";
}

public class BulkDeliveryItemViewModel
{
    public int RowNumber { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string PickupContactName { get; set; } = string.Empty;
    public string PickupContactPhone { get; set; } = string.Empty;
    public string DropAddress { get; set; } = string.Empty;
    public string DropContactName { get; set; } = string.Empty;
    public string DropContactPhone { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public string PackageType { get; set; } = "PARCEL";
    public string Priority { get; set; } = "STANDARD";
    public string? SpecialInstructions { get; set; }
    public decimal EstimatedPrice { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationError { get; set; }
}

public class BulkDeliveryError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class BulkDeliveryConfirmViewModel
{
    public List<BulkDeliveryItemViewModel> ValidDeliveries { get; set; } = new();
    public int TotalCount { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public decimal WalletBalance { get; set; }
    public bool HasSufficientBalance => WalletBalance >= TotalEstimatedCost;
    public string? SessionKey { get; set; }
}

public class BulkDeliveryResultViewModel
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<BulkDeliveryCreatedItem> CreatedDeliveries { get; set; } = new();
    public List<BulkDeliveryError> Errors { get; set; } = new();
}

public class BulkDeliveryCreatedItem
{
    public Guid DeliveryId { get; set; }
    public int RowNumber { get; set; }
    public string DropAddress { get; set; } = string.Empty;
    public string Status { get; set; } = "CREATED";
}

public class BulkDeliveryHistoryViewModel
{
    public List<BulkDeliveryBatchItem> Batches { get; set; } = new();
    public int TotalBatches { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalBatches / PageSize);
}

public class BulkDeliveryBatchItem
{
    public Guid BatchId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalDeliveries { get; set; }
    public int CompletedDeliveries { get; set; }
    public int InProgressDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public decimal TotalCost { get; set; }
    public string Status { get; set; } = string.Empty;

    public string StatusBadgeClass => Status switch
    {
        "COMPLETED" => "bg-success",
        "IN_PROGRESS" => "bg-info",
        "PARTIAL" => "bg-warning",
        "FAILED" => "bg-danger",
        _ => "bg-secondary"
    };
}

#endregion

#region Subscription ViewModels

public class SubscriptionListViewModel
{
    public List<SubscriptionPlanViewModel> Plans { get; set; } = new();
    public SubscriptionViewModel? CurrentSubscription { get; set; }
    public bool HasActiveSubscription => CurrentSubscription != null && CurrentSubscription.IsActive;
}

public class SubscriptionPlanViewModel
{
    public string PlanId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public List<string> Features { get; set; } = new();
    public bool IsPopular { get; set; }
    public bool IsCurrentPlan { get; set; }
    public int MaxDeliveriesPerMonth { get; set; }
    public decimal DiscountOnDeliveries { get; set; }
    public bool HasPrioritySupport { get; set; }
    public bool HasDedicatedManager { get; set; }
    public bool HasApiAccess { get; set; }
}

public class SubscriptionViewModel
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsAutoRenew { get; set; }
    public string BillingCycle { get; set; } = "MONTHLY";
    public decimal PricePerCycle { get; set; }
    public int DeliveriesUsed { get; set; }
    public int DeliveriesLimit { get; set; }
    public decimal DeliveryDiscount { get; set; }
    public int DaysRemaining => (EndDate - DateTime.UtcNow).Days;
    public decimal UsagePercent => DeliveriesLimit > 0 ? (decimal)DeliveriesUsed / DeliveriesLimit * 100 : 0;

    public string StatusBadgeClass => IsActive ? "bg-success" : "bg-secondary";
    public string StatusText => IsActive ? "Active" : "Expired";
}

public class SubscribeViewModel
{
    public string PlanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;

    [Display(Name = "Billing Cycle")]
    public string BillingCycle { get; set; } = "MONTHLY";

    public decimal Price { get; set; }
    public decimal WalletBalance { get; set; }
    public bool HasSufficientBalance => WalletBalance >= Price;

    [Display(Name = "Auto-Renew")]
    public bool AutoRenew { get; set; } = true;

    [Display(Name = "I agree to the terms and conditions")]
    [Required(ErrorMessage = "You must agree to the terms and conditions")]
    public bool AgreeToTerms { get; set; }
}

public class SubscriptionHistoryViewModel
{
    public List<SubscriptionHistoryItem> History { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class SubscriptionHistoryItem
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

#endregion
