using System;
using System.Threading;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs.Wallet;

namespace DeliveryDost.Application.Services;

public interface IWalletService
{
    // Wallet operations
    Task<WalletDto> GetOrCreateWalletAsync(Guid userId, string walletType, CancellationToken ct = default);
    Task<WalletDto?> GetWalletAsync(Guid userId, CancellationToken ct = default);
    Task<GetTransactionsResponse> GetTransactionsAsync(Guid userId, GetTransactionsRequest request, CancellationToken ct = default);

    // Balance operations
    Task<bool> CreditWalletAsync(Guid userId, decimal amount, string category, string description, string? referenceId = null, string? referenceType = null, CancellationToken ct = default);
    Task<bool> DebitWalletAsync(Guid userId, decimal amount, string category, string description, string? referenceId = null, string? referenceType = null, CancellationToken ct = default);
    Task<bool> HoldBalanceAsync(Guid userId, decimal amount, string description, string? referenceId = null, CancellationToken ct = default);
    Task<bool> ReleaseHoldAsync(Guid userId, decimal amount, string description, string? referenceId = null, CancellationToken ct = default);

    // Recharge
    Task<RechargeWalletResponse> InitiateRechargeAsync(Guid userId, RechargeWalletRequest request, CancellationToken ct = default);
    Task<bool> ConfirmPaymentAsync(ConfirmPaymentRequest request, CancellationToken ct = default);

    // Delivery payment
    Task<InitiateDeliveryPaymentResponse> InitiateDeliveryPaymentAsync(Guid userId, InitiateDeliveryPaymentRequest request, CancellationToken ct = default);
    Task<bool> ProcessDeliveryCompletionAsync(Guid deliveryId, CancellationToken ct = default);

    // Settlements
    Task<GetSettlementsResponse> GetSettlementsAsync(Guid userId, GetSettlementsRequest request, CancellationToken ct = default);
    Task<SettlementDetailDto?> GetSettlementDetailAsync(Guid settlementId, CancellationToken ct = default);
    Task<SettlementDto?> CreateSettlementAsync(CreateSettlementRequest request, CancellationToken ct = default);
    Task<bool> ProcessSettlementAsync(Guid settlementId, CancellationToken ct = default);

    // Earnings
    Task<EarningsSummaryDto> GetEarningsSummaryAsync(Guid userId, CancellationToken ct = default);
}
