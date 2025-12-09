using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.DTOs.Subscription;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly IWalletService _walletService;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ApplicationDbContext context,
        IWalletService walletService,
        ILogger<SubscriptionService> logger)
    {
        _context = context;
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<List<SubscriptionPlanDto>> GetPlansAsync(string? planType = null, CancellationToken ct = default)
    {
        var query = _context.Set<SubscriptionPlan>()
            .Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(planType))
        {
            query = query.Where(p => p.PlanType == planType);
        }

        var plans = await query
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        return plans.Select(MapPlanToDto).ToList();
    }

    public async Task<SubscriptionPlanDto?> GetPlanAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _context.Set<SubscriptionPlan>().FindAsync(new object[] { planId }, ct);
        return plan != null ? MapPlanToDto(plan) : null;
    }

    public async Task<UserSubscriptionDto?> GetActiveSubscriptionAsync(Guid userId, CancellationToken ct = default)
    {
        var subscription = await _context.Set<UserSubscription>()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "ACTIVE", ct);

        return subscription != null ? MapSubscriptionToDto(subscription) : null;
    }

    public async Task<SubscribeResponse> SubscribeAsync(Guid userId, SubscribeRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("User {UserId} subscribing to plan {PlanId}", userId, request.PlanId);

        // Check for existing active subscription
        var existing = await _context.Set<UserSubscription>()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "ACTIVE", ct);

        if (existing != null)
        {
            return new SubscribeResponse
            {
                IsSuccess = false,
                ErrorCode = "ACTIVE_SUBSCRIPTION_EXISTS",
                Message = "You already have an active subscription"
            };
        }

        var plan = await _context.Set<SubscriptionPlan>().FindAsync(new object[] { request.PlanId }, ct);
        if (plan == null || !plan.IsActive)
        {
            return new SubscribeResponse
            {
                IsSuccess = false,
                ErrorCode = "PLAN_NOT_FOUND",
                Message = "Subscription plan not found"
            };
        }

        var amount = plan.DiscountedPrice ?? plan.Price;

        // Apply promo code if provided
        if (!string.IsNullOrEmpty(request.PromoCode))
        {
            var promoValidation = await ValidatePromoCodeAsync(userId, new ValidatePromoCodeRequest
            {
                Code = request.PromoCode,
                ApplyTo = "SUBSCRIPTION",
                OrderAmount = amount
            }, ct);

            if (promoValidation.IsValid && promoValidation.FinalAmount.HasValue)
            {
                amount = promoValidation.FinalAmount.Value;
            }
        }

        // Process payment
        if (request.PaymentMethod == "WALLET")
        {
            var debitSuccess = await _walletService.DebitWalletAsync(userId, amount, "SUBSCRIPTION",
                $"Subscription to {plan.Name}", request.PlanId.ToString(), "SUBSCRIPTION", ct);

            if (!debitSuccess)
            {
                return new SubscribeResponse
                {
                    IsSuccess = false,
                    ErrorCode = "PAYMENT_FAILED",
                    Message = "Insufficient wallet balance"
                };
            }
        }

        // Calculate dates based on billing cycle
        var startDate = DateTime.UtcNow;
        var endDate = plan.BillingCycle switch
        {
            "MONTHLY" => startDate.AddMonths(1),
            "QUARTERLY" => startDate.AddMonths(3),
            "YEARLY" => startDate.AddYears(1),
            _ => startDate.AddMonths(1)
        };

        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = plan.Id,
            Status = "ACTIVE",
            StartDate = startDate,
            EndDate = endDate,
            NextBillingDate = endDate,
            AutoRenew = true,
            DeliveriesUsed = 0,
            DeliveryQuota = plan.DeliveryQuota,
            AmountPaid = amount,
            PaymentMethod = request.PaymentMethod,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<UserSubscription>().Add(subscription);

        // Create invoice
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.Set<SubscriptionInvoice>()
            .CountAsync(i => i.InvoiceNumber.StartsWith($"INV-{today}"), ct);
        var invoiceNumber = $"INV-{today}-{(count + 1):D4}";

        var invoice = new SubscriptionInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            SubscriptionId = subscription.Id,
            UserId = userId,
            BillingPeriod = $"{startDate:MMM yyyy} - {endDate:MMM yyyy}",
            Subtotal = plan.DiscountedPrice ?? plan.Price,
            Discount = (plan.DiscountedPrice ?? plan.Price) - amount,
            TaxAmount = 0, // GST could be added
            TotalAmount = amount,
            Status = "PAID",
            DueDate = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<SubscriptionInvoice>().Add(invoice);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} subscribed to plan {PlanId} successfully", userId, plan.Id);

        return new SubscribeResponse
        {
            IsSuccess = true,
            SubscriptionId = subscription.Id,
            AmountCharged = amount,
            StartDate = startDate,
            EndDate = endDate,
            Message = $"Successfully subscribed to {plan.Name}"
        };
    }

    public async Task<bool> CancelSubscriptionAsync(Guid userId, CancelSubscriptionRequest request, CancellationToken ct = default)
    {
        var subscription = await _context.Set<UserSubscription>()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "ACTIVE", ct);

        if (subscription == null) return false;

        subscription.Status = "CANCELLED";
        subscription.AutoRenew = false;
        subscription.CancellationReason = request.Reason;
        subscription.CancelledAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} cancelled subscription", userId);
        return true;
    }

    public async Task<bool> ToggleAutoRenewAsync(Guid userId, bool autoRenew, CancellationToken ct = default)
    {
        var subscription = await _context.Set<UserSubscription>()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "ACTIVE", ct);

        if (subscription == null) return false;

        subscription.AutoRenew = autoRenew;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> IncrementDeliveryUsageAsync(Guid userId, CancellationToken ct = default)
    {
        var subscription = await _context.Set<UserSubscription>()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "ACTIVE", ct);

        if (subscription == null || subscription.DeliveryQuota == 0) return true; // No subscription or unlimited

        if (subscription.DeliveryQuota != -1 && subscription.DeliveriesUsed >= subscription.DeliveryQuota)
        {
            return false; // Quota exceeded
        }

        subscription.DeliveriesUsed++;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<GetInvoicesResponse> GetInvoicesAsync(Guid userId, GetInvoicesRequest request, CancellationToken ct = default)
    {
        var query = _context.Set<SubscriptionInvoice>()
            .Where(i => i.UserId == userId);

        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(i => i.Status == request.Status);
        }

        var totalCount = await query.CountAsync(ct);

        var invoices = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new GetInvoicesResponse
        {
            Invoices = invoices.Select(MapInvoiceToDto).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _context.Set<SubscriptionInvoice>().FindAsync(new object[] { invoiceId }, ct);
        return invoice != null ? MapInvoiceToDto(invoice) : null;
    }

    public async Task<ValidatePromoCodeResponse> ValidatePromoCodeAsync(Guid userId, ValidatePromoCodeRequest request, CancellationToken ct = default)
    {
        var promo = await _context.Set<PromoCode>()
            .FirstOrDefaultAsync(p => p.Code == request.Code && p.IsActive, ct);

        if (promo == null)
        {
            return new ValidatePromoCodeResponse { IsValid = false, ErrorMessage = "Invalid promo code" };
        }

        // Check validity dates
        if (promo.ValidFrom.HasValue && DateTime.UtcNow < promo.ValidFrom.Value)
        {
            return new ValidatePromoCodeResponse { IsValid = false, ErrorMessage = "Promo code is not yet active" };
        }

        if (promo.ValidTo.HasValue && DateTime.UtcNow > promo.ValidTo.Value)
        {
            return new ValidatePromoCodeResponse { IsValid = false, ErrorMessage = "Promo code has expired" };
        }

        // Check applicability
        if (promo.ApplicableTo != "ALL" && promo.ApplicableTo != request.ApplyTo)
        {
            return new ValidatePromoCodeResponse { IsValid = false, ErrorMessage = "Promo code not applicable" };
        }

        // Check minimum order
        if (promo.MinOrderAmount.HasValue && request.OrderAmount < promo.MinOrderAmount.Value)
        {
            return new ValidatePromoCodeResponse { IsValid = false, ErrorMessage = $"Minimum order amount is {promo.MinOrderAmount}" };
        }

        // Check max usage
        if (promo.MaxUsage.HasValue && promo.CurrentUsage >= promo.MaxUsage.Value)
        {
            return new ValidatePromoCodeResponse { IsValid = false, ErrorMessage = "Promo code usage limit reached" };
        }

        // Check user usage limit
        if (promo.MaxUsagePerUser.HasValue)
        {
            var userUsage = await _context.Set<PromoCodeUsage>()
                .CountAsync(u => u.PromoCodeId == promo.Id && u.UserId == userId, ct);

            if (userUsage >= promo.MaxUsagePerUser.Value)
            {
                return new ValidatePromoCodeResponse { IsValid = false, ErrorMessage = "You have already used this promo code" };
            }
        }

        // Calculate discount
        decimal discountAmount;
        if (promo.DiscountType == "PERCENTAGE")
        {
            discountAmount = request.OrderAmount * (promo.DiscountValue / 100);
            if (promo.MaxDiscountAmount.HasValue)
            {
                discountAmount = Math.Min(discountAmount, promo.MaxDiscountAmount.Value);
            }
        }
        else
        {
            discountAmount = promo.DiscountValue;
        }

        var finalAmount = Math.Max(0, request.OrderAmount - discountAmount);

        return new ValidatePromoCodeResponse
        {
            IsValid = true,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            PromoCode = new PromoCodeDto
            {
                Id = promo.Id,
                Code = promo.Code,
                Description = promo.Description,
                DiscountType = promo.DiscountType,
                DiscountValue = promo.DiscountValue,
                MaxDiscountAmount = promo.MaxDiscountAmount,
                ValidTo = promo.ValidTo,
                IsActive = promo.IsActive
            }
        };
    }

    public async Task<PromoCodeDto?> CreatePromoCodeAsync(CreatePromoCodeRequest request, CancellationToken ct = default)
    {
        var existing = await _context.Set<PromoCode>()
            .FirstOrDefaultAsync(p => p.Code == request.Code, ct);

        if (existing != null) return null;

        var promo = new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = request.Code.ToUpper(),
            Description = request.Description,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MaxDiscountAmount = request.MaxDiscountAmount,
            MinOrderAmount = request.MinOrderAmount,
            ApplicableTo = request.ApplicableTo,
            MaxUsage = request.MaxUsage,
            MaxUsagePerUser = request.MaxUsagePerUser,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<PromoCode>().Add(promo);
        await _context.SaveChangesAsync(ct);

        return new PromoCodeDto
        {
            Id = promo.Id,
            Code = promo.Code,
            Description = promo.Description,
            DiscountType = promo.DiscountType,
            DiscountValue = promo.DiscountValue,
            MaxDiscountAmount = promo.MaxDiscountAmount,
            ValidTo = promo.ValidTo,
            IsActive = promo.IsActive
        };
    }

    public async Task<List<PromoCodeDto>> GetPromoCodesAsync(CancellationToken ct = default)
    {
        var promos = await _context.Set<PromoCode>()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return promos.Select(p => new PromoCodeDto
        {
            Id = p.Id,
            Code = p.Code,
            Description = p.Description,
            DiscountType = p.DiscountType,
            DiscountValue = p.DiscountValue,
            MaxDiscountAmount = p.MaxDiscountAmount,
            ValidTo = p.ValidTo,
            IsActive = p.IsActive
        }).ToList();
    }

    public async Task ProcessExpiredSubscriptionsAsync(CancellationToken ct = default)
    {
        var expired = await _context.Set<UserSubscription>()
            .Where(s => s.Status == "ACTIVE" && s.EndDate < DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var sub in expired)
        {
            sub.Status = "EXPIRED";
            sub.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Processed {Count} expired subscriptions", expired.Count);
    }

    public async Task GenerateRenewalInvoicesAsync(CancellationToken ct = default)
    {
        var dueForRenewal = await _context.Set<UserSubscription>()
            .Include(s => s.Plan)
            .Where(s => s.Status == "ACTIVE" && s.AutoRenew && s.NextBillingDate <= DateTime.UtcNow.AddDays(3))
            .ToListAsync(ct);

        _logger.LogInformation("Found {Count} subscriptions due for renewal", dueForRenewal.Count);
        // In production, would create invoices and send reminders
    }

    private SubscriptionPlanDto MapPlanToDto(SubscriptionPlan plan)
    {
        var features = new List<string>();
        if (!string.IsNullOrEmpty(plan.Features))
        {
            features = JsonSerializer.Deserialize<List<string>>(plan.Features) ?? new List<string>();
        }

        return new SubscriptionPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            PlanType = plan.PlanType,
            BillingCycle = plan.BillingCycle,
            Price = plan.Price,
            DiscountedPrice = plan.DiscountedPrice,
            DeliveryQuota = plan.DeliveryQuota,
            PerDeliveryDiscount = plan.PerDeliveryDiscount,
            PrioritySupport = plan.PrioritySupport,
            AdvancedAnalytics = plan.AdvancedAnalytics,
            Features = features,
            IsActive = plan.IsActive
        };
    }

    private UserSubscriptionDto MapSubscriptionToDto(UserSubscription subscription)
    {
        return new UserSubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            PlanId = subscription.PlanId,
            PlanName = subscription.Plan?.Name ?? "",
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            NextBillingDate = subscription.NextBillingDate,
            AutoRenew = subscription.AutoRenew,
            DeliveriesUsed = subscription.DeliveriesUsed,
            DeliveryQuota = subscription.DeliveryQuota,
            AmountPaid = subscription.AmountPaid,
            CreatedAt = subscription.CreatedAt
        };
    }

    private InvoiceDto MapInvoiceToDto(SubscriptionInvoice invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            BillingPeriod = invoice.BillingPeriod,
            Subtotal = invoice.Subtotal,
            Discount = invoice.Discount,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            Status = invoice.Status,
            DueDate = invoice.DueDate,
            PaidAt = invoice.PaidAt,
            CreatedAt = invoice.CreatedAt
        };
    }
}
