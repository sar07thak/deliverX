using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeliveryDost.Web.ViewModels.Wallet;

/// <summary>
/// ViewModel for Wallet dashboard
/// </summary>
public class WalletDashboardViewModel
{
    // Wallet Info
    public Guid WalletId { get; set; }
    public decimal Balance { get; set; }
    public decimal HoldBalance { get; set; }
    public decimal AvailableBalance { get; set; }
    public string Currency { get; set; } = "INR";
    public bool IsActive { get; set; }

    // Recent Transactions
    public List<TransactionItemViewModel> RecentTransactions { get; set; } = new();

    // Earnings Summary (for DP)
    public EarningsSummaryViewModel? Earnings { get; set; }

    // Quick Stats
    public decimal TodaySpent { get; set; }
    public decimal TodayEarned { get; set; }
    public decimal MonthSpent { get; set; }
    public decimal MonthEarned { get; set; }

    // User role for conditional display
    public string UserRole { get; set; } = string.Empty;
    public bool IsDP => UserRole == "DP";
    public bool IsRequester => UserRole is "EC" or "BC" or "DBC";
}

/// <summary>
/// ViewModel for earnings summary (DP view)
/// </summary>
public class EarningsSummaryViewModel
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

/// <summary>
/// ViewModel for transaction list page
/// </summary>
public class TransactionListViewModel
{
    public List<TransactionItemViewModel> Transactions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // Filters
    public string? TypeFilter { get; set; }
    public string? CategoryFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    // Summary
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }

    public static List<SelectOption> TypeOptions => new()
    {
        new SelectOption("", "All Types"),
        new SelectOption("CREDIT", "Credits"),
        new SelectOption("DEBIT", "Debits")
    };

    public static List<SelectOption> CategoryOptions => new()
    {
        new SelectOption("", "All Categories"),
        new SelectOption("RECHARGE", "Recharge"),
        new SelectOption("DELIVERY_PAYMENT", "Delivery Payment"),
        new SelectOption("DELIVERY_EARNING", "Delivery Earning"),
        new SelectOption("REFUND", "Refund"),
        new SelectOption("SETTLEMENT", "Settlement"),
        new SelectOption("BONUS", "Bonus")
    };
}

/// <summary>
/// Single transaction item
/// </summary>
public class TransactionItemViewModel
{
    public Guid Id { get; set; }
    public string TransactionType { get; set; } = string.Empty; // CREDIT, DEBIT
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public bool IsCredit => TransactionType == "CREDIT";
    public string AmountClass => IsCredit ? "text-success" : "text-danger";
    public string AmountPrefix => IsCredit ? "+" : "-";

    public string CategoryIcon => Category switch
    {
        "RECHARGE" => "bi-wallet2",
        "DELIVERY_PAYMENT" => "bi-truck",
        "DELIVERY_EARNING" => "bi-cash-coin",
        "REFUND" => "bi-arrow-return-left",
        "SETTLEMENT" => "bi-bank",
        "BONUS" => "bi-gift",
        _ => "bi-arrow-left-right"
    };

    public string CategoryBadgeClass => Category switch
    {
        "RECHARGE" => "bg-primary",
        "DELIVERY_PAYMENT" => "bg-info",
        "DELIVERY_EARNING" => "bg-success",
        "REFUND" => "bg-warning",
        "SETTLEMENT" => "bg-secondary",
        "BONUS" => "bg-danger",
        _ => "bg-secondary"
    };
}

/// <summary>
/// ViewModel for top-up page
/// </summary>
public class TopUpViewModel
{
    public decimal CurrentBalance { get; set; }

    [Required(ErrorMessage = "Please enter an amount")]
    [Range(10, 100000, ErrorMessage = "Amount must be between ₹10 and ₹1,00,000")]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; } = 100;

    [Required]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "UPI";

    // Quick amounts
    public static List<decimal> QuickAmounts => new() { 100, 200, 500, 1000, 2000, 5000 };

    public static List<PaymentMethodOption> PaymentMethods => new()
    {
        new PaymentMethodOption("UPI", "UPI", "bi-phone", "Pay using UPI apps"),
        new PaymentMethodOption("CARD", "Credit/Debit Card", "bi-credit-card", "Visa, Mastercard, RuPay"),
        new PaymentMethodOption("NETBANKING", "Net Banking", "bi-bank", "All major banks")
    };
}

/// <summary>
/// Payment method option
/// </summary>
public class PaymentMethodOption
{
    public string Value { get; set; }
    public string Label { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }

    public PaymentMethodOption(string value, string label, string icon, string description)
    {
        Value = value;
        Label = label;
        Icon = icon;
        Description = description;
    }
}

/// <summary>
/// ViewModel for payment processing page
/// </summary>
public class PaymentProcessingViewModel
{
    public Guid PaymentId { get; set; }
    public string? PaymentNumber { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? GatewayOrderId { get; set; }
    public string? GatewayPaymentUrl { get; set; }
    public string Status { get; set; } = "PENDING";
}

/// <summary>
/// ViewModel for payment result page
/// </summary>
public class PaymentResultViewModel
{
    public bool IsSuccess { get; set; }
    public string? PaymentNumber { get; set; }
    public decimal Amount { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public decimal? NewBalance { get; set; }
}

/// <summary>
/// ViewModel for settlements list (DP view)
/// </summary>
public class SettlementListViewModel
{
    public List<SettlementItemViewModel> Settlements { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // Summary
    public decimal TotalPendingAmount { get; set; }
    public decimal TotalSettledAmount { get; set; }

    // Filters
    public string? StatusFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public static List<SelectOption> StatusOptions => new()
    {
        new SelectOption("", "All Statuses"),
        new SelectOption("PENDING", "Pending"),
        new SelectOption("PROCESSING", "Processing"),
        new SelectOption("COMPLETED", "Completed"),
        new SelectOption("FAILED", "Failed")
    };
}

/// <summary>
/// Single settlement item
/// </summary>
public class SettlementItemViewModel
{
    public Guid Id { get; set; }
    public string SettlementNumber { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public decimal TdsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string PayoutMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SettlementDate { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int ItemCount { get; set; }

    public string StatusBadgeClass => Status switch
    {
        "PENDING" => "bg-warning",
        "PROCESSING" => "bg-info",
        "COMPLETED" => "bg-success",
        "FAILED" => "bg-danger",
        _ => "bg-secondary"
    };
}

/// <summary>
/// Helper class for select options
/// </summary>
public class SelectOption
{
    public string Value { get; set; }
    public string Text { get; set; }

    public SelectOption(string value, string text)
    {
        Value = value;
        Text = text;
    }
}
