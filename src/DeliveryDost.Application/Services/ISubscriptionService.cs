using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs.Subscription;

namespace DeliveryDost.Application.Services;

public interface ISubscriptionService
{
    // Plans
    Task<List<SubscriptionPlanDto>> GetPlansAsync(string? planType = null, CancellationToken ct = default);
    Task<SubscriptionPlanDto?> GetPlanAsync(Guid planId, CancellationToken ct = default);

    // User subscriptions
    Task<UserSubscriptionDto?> GetActiveSubscriptionAsync(Guid userId, CancellationToken ct = default);
    Task<SubscribeResponse> SubscribeAsync(Guid userId, SubscribeRequest request, CancellationToken ct = default);
    Task<bool> CancelSubscriptionAsync(Guid userId, CancelSubscriptionRequest request, CancellationToken ct = default);
    Task<bool> ToggleAutoRenewAsync(Guid userId, bool autoRenew, CancellationToken ct = default);
    Task<bool> IncrementDeliveryUsageAsync(Guid userId, CancellationToken ct = default);

    // Invoices
    Task<GetInvoicesResponse> GetInvoicesAsync(Guid userId, GetInvoicesRequest request, CancellationToken ct = default);
    Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken ct = default);

    // Promo codes
    Task<ValidatePromoCodeResponse> ValidatePromoCodeAsync(Guid userId, ValidatePromoCodeRequest request, CancellationToken ct = default);
    Task<PromoCodeDto?> CreatePromoCodeAsync(CreatePromoCodeRequest request, CancellationToken ct = default);
    Task<List<PromoCodeDto>> GetPromoCodesAsync(CancellationToken ct = default);

    // Billing automation
    Task ProcessExpiredSubscriptionsAsync(CancellationToken ct = default);
    Task GenerateRenewalInvoicesAsync(CancellationToken ct = default);
}
