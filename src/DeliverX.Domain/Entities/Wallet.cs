using System;
using System.Collections.Generic;

namespace DeliverX.Domain.Entities;

/// <summary>
/// Wallet entity for user balance management
/// </summary>
public class Wallet
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string WalletType { get; set; } = string.Empty; // USER, DP, EC, BC, DPCM, PLATFORM
    public decimal Balance { get; set; }
    public decimal HoldBalance { get; set; } // Amount on hold for pending deliveries
    public string Currency { get; set; } = "INR";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}

/// <summary>
/// Wallet transaction entity
/// </summary>
public class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public string TransactionType { get; set; } = string.Empty; // CREDIT, DEBIT, HOLD, RELEASE, REFUND
    public string Category { get; set; } = string.Empty; // DELIVERY_PAYMENT, DELIVERY_EARNING, COMMISSION, PLATFORM_FEE, RECHARGE, WITHDRAWAL, REFUND
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? ReferenceId { get; set; } // DeliveryId, PaymentId, etc.
    public string? ReferenceType { get; set; } // DELIVERY, PAYMENT, SETTLEMENT
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "COMPLETED"; // PENDING, COMPLETED, FAILED, REVERSED
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Wallet? Wallet { get; set; }
}

/// <summary>
/// Payment entity for tracking payments from users
/// </summary>
public class Payment
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty; // PAY-YYYYMMDD-XXXX
    public Guid UserId { get; set; }
    public Guid? DeliveryId { get; set; }
    public string PaymentType { get; set; } = string.Empty; // DELIVERY, RECHARGE, SUBSCRIPTION
    public decimal Amount { get; set; }
    public decimal? PlatformFee { get; set; }
    public decimal? Tax { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // WALLET, UPI, CARD, NETBANKING, COD
    public string? PaymentGateway { get; set; } // RAZORPAY, PHONEPE, PAYTM
    public string? GatewayTransactionId { get; set; }
    public string? GatewayOrderId { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, PROCESSING, COMPLETED, FAILED, REFUNDED
    public string? FailureReason { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public Delivery? Delivery { get; set; }
}

/// <summary>
/// Settlement entity for DP/DPCM payouts
/// </summary>
public class Settlement
{
    public Guid Id { get; set; }
    public string SettlementNumber { get; set; } = string.Empty; // STL-YYYYMMDD-XXXX
    public Guid BeneficiaryId { get; set; }
    public string BeneficiaryType { get; set; } = string.Empty; // DP, DPCM
    public decimal GrossAmount { get; set; }
    public decimal TdsAmount { get; set; } // Tax Deducted at Source
    public decimal NetAmount { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankIfscCode { get; set; }
    public string? UpiId { get; set; }
    public string PayoutMethod { get; set; } = "BANK_TRANSFER"; // BANK_TRANSFER, UPI
    public string Status { get; set; } = "PENDING"; // PENDING, PROCESSING, COMPLETED, FAILED
    public string? PayoutReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime SettlementDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Beneficiary { get; set; }
    public ICollection<SettlementItem> Items { get; set; } = new List<SettlementItem>();
}

/// <summary>
/// Settlement item - individual earnings included in a settlement
/// </summary>
public class SettlementItem
{
    public Guid Id { get; set; }
    public Guid SettlementId { get; set; }
    public Guid DeliveryId { get; set; }
    public decimal EarningAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime EarnedAt { get; set; }

    // Navigation
    public Settlement? Settlement { get; set; }
    public Delivery? Delivery { get; set; }
}

/// <summary>
/// Commission record for tracking commissions
/// </summary>
public class CommissionRecord
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public Guid DPId { get; set; }
    public Guid? DPCMId { get; set; }
    public decimal DeliveryAmount { get; set; }
    public decimal DPEarning { get; set; }
    public decimal DPCMCommission { get; set; }
    public decimal PlatformFee { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, SETTLED
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Delivery? Delivery { get; set; }
    public User? DP { get; set; }
    public User? DPCM { get; set; }
}
