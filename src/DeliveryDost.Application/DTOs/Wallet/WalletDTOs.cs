using System;
using System.Collections.Generic;

namespace DeliveryDost.Application.DTOs.Wallet;

// Wallet DTOs
public class WalletDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string WalletType { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal HoldBalance { get; set; }
    public decimal AvailableBalance => Balance - HoldBalance;
    public string Currency { get; set; } = "INR";
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class WalletTransactionDto
{
    public Guid Id { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GetTransactionsRequest
{
    public string? TransactionType { get; set; }
    public string? Category { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetTransactionsResponse
{
    public List<WalletTransactionDto> Transactions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class RechargeWalletRequest
{
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "UPI"; // UPI, CARD, NETBANKING
}

public class RechargeWalletResponse
{
    public bool IsSuccess { get; set; }
    public Guid? PaymentId { get; set; }
    public string? PaymentNumber { get; set; }
    public string? GatewayOrderId { get; set; }
    public string? GatewayPaymentUrl { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
}

public class ConfirmPaymentRequest
{
    public Guid PaymentId { get; set; }
    public string GatewayTransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // SUCCESS, FAILED
    public string? FailureReason { get; set; }
}

// Payment DTOs
public class PaymentDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid? DeliveryId { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? PlatformFee { get; set; }
    public decimal? Tax { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InitiateDeliveryPaymentRequest
{
    public Guid DeliveryId { get; set; }
    public string PaymentMethod { get; set; } = "WALLET"; // WALLET, UPI, CARD
}

public class InitiateDeliveryPaymentResponse
{
    public bool IsSuccess { get; set; }
    public Guid? PaymentId { get; set; }
    public string? PaymentNumber { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? GatewayOrderId { get; set; }
    public string? GatewayPaymentUrl { get; set; }
    public bool? IsPaidViaWallet { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
}

// Settlement DTOs
public class SettlementDto
{
    public Guid Id { get; set; }
    public string SettlementNumber { get; set; } = string.Empty;
    public Guid BeneficiaryId { get; set; }
    public string BeneficiaryType { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public decimal TdsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string PayoutMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SettlementDate { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}

public class SettlementDetailDto : SettlementDto
{
    public List<SettlementItemDto> Items { get; set; } = new();
}

public class SettlementItemDto
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public string? DeliveryTrackingNumber { get; set; }
    public decimal EarningAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime EarnedAt { get; set; }
}

public class GetSettlementsRequest
{
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetSettlementsResponse
{
    public List<SettlementDto> Settlements { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public decimal TotalPendingAmount { get; set; }
}

public class CreateSettlementRequest
{
    public Guid BeneficiaryId { get; set; }
    public DateTime SettlementDate { get; set; }
}

// Earnings summary
public class EarningsSummaryDto
{
    public decimal TotalEarnings { get; set; }
    public decimal TodayEarnings { get; set; }
    public decimal WeekEarnings { get; set; }
    public decimal MonthEarnings { get; set; }
    public decimal PendingSettlement { get; set; }
    public decimal TotalSettled { get; set; }
    public int TotalDeliveries { get; set; }
    public decimal AveragePerDelivery { get; set; }
}
