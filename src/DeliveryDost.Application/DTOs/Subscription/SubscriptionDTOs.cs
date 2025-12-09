using System;
using System.Collections.Generic;

namespace DeliveryDost.Application.DTOs.Subscription;

// Plan DTOs
public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public int DeliveryQuota { get; set; }
    public decimal? PerDeliveryDiscount { get; set; }
    public bool PrioritySupport { get; set; }
    public bool AdvancedAnalytics { get; set; }
    public List<string> Features { get; set; } = new();
    public bool IsActive { get; set; }
}

// Subscription DTOs
public class UserSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public bool AutoRenew { get; set; }
    public int DeliveriesUsed { get; set; }
    public int DeliveryQuota { get; set; }
    public int DeliveriesRemaining => DeliveryQuota == -1 ? -1 : Math.Max(0, DeliveryQuota - DeliveriesUsed);
    public decimal AmountPaid { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SubscribeRequest
{
    public Guid PlanId { get; set; }
    public string PaymentMethod { get; set; } = "WALLET"; // WALLET, UPI, CARD
    public string? PromoCode { get; set; }
}

public class SubscribeResponse
{
    public bool IsSuccess { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public decimal? AmountCharged { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class CancelSubscriptionRequest
{
    public string Reason { get; set; } = string.Empty;
}

// Invoice DTOs
public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal? Discount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetInvoicesRequest
{
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetInvoicesResponse
{
    public List<InvoiceDto> Invoices { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

// Promo Code DTOs
public class PromoCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
}

public class ValidatePromoCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public string ApplyTo { get; set; } = string.Empty; // SUBSCRIPTION, DELIVERY
    public decimal OrderAmount { get; set; }
}

public class ValidatePromoCodeResponse
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? FinalAmount { get; set; }
    public PromoCodeDto? PromoCode { get; set; }
}

public class CreatePromoCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "PERCENTAGE";
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public string ApplicableTo { get; set; } = "ALL";
    public int? MaxUsage { get; set; }
    public int? MaxUsagePerUser { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}
