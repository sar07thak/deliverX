using System;
using System.Collections.Generic;

namespace DeliverX.Domain.Entities;

/// <summary>
/// Subscription plan definition
/// </summary>
public class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // Basic, Pro, Enterprise
    public string Description { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty; // EC, BC, DP
    public string BillingCycle { get; set; } = "MONTHLY"; // MONTHLY, QUARTERLY, YEARLY
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public int DeliveryQuota { get; set; } // -1 for unlimited
    public decimal? PerDeliveryDiscount { get; set; } // Percentage discount on delivery charges
    public bool PrioritySupport { get; set; }
    public bool AdvancedAnalytics { get; set; }
    public string? Features { get; set; } // JSON array of features
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// User subscription
/// </summary>
public class UserSubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, EXPIRED, CANCELLED, SUSPENDED
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public bool AutoRenew { get; set; } = true;
    public int DeliveriesUsed { get; set; }
    public int DeliveryQuota { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public SubscriptionPlan? Plan { get; set; }
    public ICollection<SubscriptionInvoice> Invoices { get; set; } = new List<SubscriptionInvoice>();
}

/// <summary>
/// Subscription invoice
/// </summary>
public class SubscriptionInvoice
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty; // INV-YYYYMMDD-XXXX
    public Guid SubscriptionId { get; set; }
    public Guid UserId { get; set; }
    public string BillingPeriod { get; set; } = string.Empty; // "Nov 2024 - Dec 2024"
    public decimal Subtotal { get; set; }
    public decimal? Discount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, PAID, FAILED, CANCELLED
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? PaymentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public UserSubscription? Subscription { get; set; }
    public User? User { get; set; }
}

/// <summary>
/// Promotional code
/// </summary>
public class PromoCode
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "PERCENTAGE"; // PERCENTAGE, FIXED
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public string ApplicableTo { get; set; } = "ALL"; // ALL, SUBSCRIPTION, DELIVERY
    public int? MaxUsage { get; set; }
    public int CurrentUsage { get; set; }
    public int? MaxUsagePerUser { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Promo code usage tracking
/// </summary>
public class PromoCodeUsage
{
    public Guid Id { get; set; }
    public Guid PromoCodeId { get; set; }
    public Guid UserId { get; set; }
    public string UsedFor { get; set; } = string.Empty; // SUBSCRIPTION, DELIVERY
    public Guid? ReferenceId { get; set; }
    public decimal DiscountApplied { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PromoCode? PromoCode { get; set; }
    public User? User { get; set; }
}
