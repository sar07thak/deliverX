using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliverX.Application.DTOs.Wallet;
using DeliverX.Application.Services;
using DeliverX.Domain.Entities;
using DeliverX.Infrastructure.Data;

namespace DeliverX.Infrastructure.Services;

public class WalletService : IWalletService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WalletService> _logger;

    public WalletService(ApplicationDbContext context, ILogger<WalletService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WalletDto> GetOrCreateWalletAsync(Guid userId, string walletType, CancellationToken ct = default)
    {
        var wallet = await _context.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WalletType = walletType,
                Balance = 0,
                HoldBalance = 0,
                Currency = "INR",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<Wallet>().Add(wallet);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Created wallet for user {UserId}", userId);
        }

        return MapToDto(wallet);
    }

    public async Task<WalletDto?> GetWalletAsync(Guid userId, CancellationToken ct = default)
    {
        var wallet = await _context.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);

        return wallet != null ? MapToDto(wallet) : null;
    }

    public async Task<GetTransactionsResponse> GetTransactionsAsync(Guid userId, GetTransactionsRequest request, CancellationToken ct = default)
    {
        var wallet = await _context.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);

        if (wallet == null)
        {
            return new GetTransactionsResponse { Transactions = new List<WalletTransactionDto>() };
        }

        var query = _context.Set<WalletTransaction>()
            .Where(t => t.WalletId == wallet.Id);

        if (!string.IsNullOrEmpty(request.TransactionType))
        {
            query = query.Where(t => t.TransactionType == request.TransactionType);
        }

        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(t => t.Category == request.Category);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= request.ToDate.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new GetTransactionsResponse
        {
            Transactions = transactions.Select(MapTransactionToDto).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };
    }

    public async Task<bool> CreditWalletAsync(Guid userId, decimal amount, string category, string description, string? referenceId = null, string? referenceType = null, CancellationToken ct = default)
    {
        var wallet = await _context.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);

        if (wallet == null)
        {
            var user = await _context.Users.FindAsync(new object[] { userId }, ct);
            wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WalletType = user?.Role ?? "USER",
                Balance = 0,
                HoldBalance = 0,
                Currency = "INR",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Set<Wallet>().Add(wallet);
        }

        var balanceBefore = wallet.Balance;
        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var transaction = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            TransactionType = "CREDIT",
            Category = category,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = wallet.Balance,
            ReferenceId = referenceId,
            ReferenceType = referenceType,
            Description = description,
            Status = "COMPLETED",
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<WalletTransaction>().Add(transaction);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Credited {Amount} to wallet for user {UserId}", amount, userId);
        return true;
    }

    public async Task<bool> DebitWalletAsync(Guid userId, decimal amount, string category, string description, string? referenceId = null, string? referenceType = null, CancellationToken ct = default)
    {
        var wallet = await _context.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);

        if (wallet == null || wallet.Balance - wallet.HoldBalance < amount)
        {
            _logger.LogWarning("Insufficient balance for user {UserId}", userId);
            return false;
        }

        var balanceBefore = wallet.Balance;
        wallet.Balance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var transaction = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            TransactionType = "DEBIT",
            Category = category,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = wallet.Balance,
            ReferenceId = referenceId,
            ReferenceType = referenceType,
            Description = description,
            Status = "COMPLETED",
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<WalletTransaction>().Add(transaction);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Debited {Amount} from wallet for user {UserId}", amount, userId);
        return true;
    }

    public async Task<bool> HoldBalanceAsync(Guid userId, decimal amount, string description, string? referenceId = null, CancellationToken ct = default)
    {
        var wallet = await _context.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);

        if (wallet == null || wallet.Balance - wallet.HoldBalance < amount)
        {
            return false;
        }

        var balanceBefore = wallet.Balance;
        wallet.HoldBalance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var transaction = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            TransactionType = "HOLD",
            Category = "DELIVERY_HOLD",
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = wallet.Balance,
            ReferenceId = referenceId,
            ReferenceType = "DELIVERY",
            Description = description,
            Status = "COMPLETED",
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<WalletTransaction>().Add(transaction);
        await _context.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> ReleaseHoldAsync(Guid userId, decimal amount, string description, string? referenceId = null, CancellationToken ct = default)
    {
        var wallet = await _context.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);

        if (wallet == null || wallet.HoldBalance < amount)
        {
            return false;
        }

        wallet.HoldBalance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var transaction = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            TransactionType = "RELEASE",
            Category = "DELIVERY_RELEASE",
            Amount = amount,
            BalanceBefore = wallet.Balance,
            BalanceAfter = wallet.Balance,
            ReferenceId = referenceId,
            ReferenceType = "DELIVERY",
            Description = description,
            Status = "COMPLETED",
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<WalletTransaction>().Add(transaction);
        await _context.SaveChangesAsync(ct);

        return true;
    }

    public async Task<RechargeWalletResponse> InitiateRechargeAsync(Guid userId, RechargeWalletRequest request, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.Set<Payment>()
            .CountAsync(p => p.PaymentNumber.StartsWith($"PAY-{today}"), ct);
        var paymentNumber = $"PAY-{today}-{(count + 1):D4}";

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentNumber = paymentNumber,
            UserId = userId,
            PaymentType = "RECHARGE",
            Amount = request.Amount,
            TotalAmount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            PaymentGateway = "MOCK",
            GatewayOrderId = $"MOCK_{Guid.NewGuid():N}",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<Payment>().Add(payment);
        await _context.SaveChangesAsync(ct);

        // For MVP, simulate immediate success
        payment.Status = "COMPLETED";
        payment.GatewayTransactionId = $"TXN_{Guid.NewGuid():N}";
        payment.CompletedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        await CreditWalletAsync(userId, request.Amount, "RECHARGE",
            $"Wallet recharge - {paymentNumber}", payment.Id.ToString(), "PAYMENT", ct);

        await _context.SaveChangesAsync(ct);

        return new RechargeWalletResponse
        {
            IsSuccess = true,
            PaymentId = payment.Id,
            PaymentNumber = payment.PaymentNumber,
            GatewayOrderId = payment.GatewayOrderId,
            Message = "Recharge successful"
        };
    }

    public async Task<bool> ConfirmPaymentAsync(ConfirmPaymentRequest request, CancellationToken ct = default)
    {
        var payment = await _context.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, ct);

        if (payment == null || payment.Status != "PENDING")
        {
            return false;
        }

        payment.GatewayTransactionId = request.GatewayTransactionId;
        payment.UpdatedAt = DateTime.UtcNow;

        if (request.Status == "SUCCESS")
        {
            payment.Status = "COMPLETED";
            payment.CompletedAt = DateTime.UtcNow;

            if (payment.PaymentType == "RECHARGE")
            {
                await CreditWalletAsync(payment.UserId, payment.TotalAmount, "RECHARGE",
                    $"Wallet recharge - {payment.PaymentNumber}", payment.Id.ToString(), "PAYMENT", ct);
            }
        }
        else
        {
            payment.Status = "FAILED";
            payment.FailureReason = request.FailureReason;
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<InitiateDeliveryPaymentResponse> InitiateDeliveryPaymentAsync(Guid userId, InitiateDeliveryPaymentRequest request, CancellationToken ct = default)
    {
        var delivery = await _context.Deliveries.FindAsync(new object[] { request.DeliveryId }, ct);
        if (delivery == null)
        {
            return new InitiateDeliveryPaymentResponse
            {
                IsSuccess = false,
                ErrorCode = "DELIVERY_NOT_FOUND",
                Message = "Delivery not found"
            };
        }

        var pricing = await _context.DeliveryPricings
            .FirstOrDefaultAsync(p => p.DeliveryId == request.DeliveryId, ct);

        var totalAmount = pricing?.TotalAmount ?? delivery.EstimatedPrice ?? 0;

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.Set<Payment>()
            .CountAsync(p => p.PaymentNumber.StartsWith($"PAY-{today}"), ct);
        var paymentNumber = $"PAY-{today}-{(count + 1):D4}";

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentNumber = paymentNumber,
            UserId = userId,
            DeliveryId = request.DeliveryId,
            PaymentType = "DELIVERY",
            Amount = totalAmount,
            PlatformFee = pricing?.PlatformFee ?? 0,
            TotalAmount = totalAmount,
            PaymentMethod = request.PaymentMethod,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<Payment>().Add(payment);

        if (request.PaymentMethod == "WALLET")
        {
            var wallet = await _context.Set<Wallet>()
                .FirstOrDefaultAsync(w => w.UserId == userId, ct);

            if (wallet == null || wallet.Balance - wallet.HoldBalance < totalAmount)
            {
                return new InitiateDeliveryPaymentResponse
                {
                    IsSuccess = false,
                    ErrorCode = "INSUFFICIENT_BALANCE",
                    Message = "Insufficient wallet balance"
                };
            }

            // Debit from wallet
            await DebitWalletAsync(userId, totalAmount, "DELIVERY_PAYMENT",
                $"Payment for delivery - {delivery.Id}", request.DeliveryId.ToString(), "DELIVERY", ct);

            payment.Status = "COMPLETED";
            payment.CompletedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return new InitiateDeliveryPaymentResponse
            {
                IsSuccess = true,
                PaymentId = payment.Id,
                PaymentNumber = payment.PaymentNumber,
                TotalAmount = totalAmount,
                IsPaidViaWallet = true,
                Message = "Payment successful"
            };
        }

        // For other payment methods, return gateway URL
        payment.PaymentGateway = "MOCK";
        payment.GatewayOrderId = $"MOCK_{Guid.NewGuid():N}";
        await _context.SaveChangesAsync(ct);

        return new InitiateDeliveryPaymentResponse
        {
            IsSuccess = true,
            PaymentId = payment.Id,
            PaymentNumber = payment.PaymentNumber,
            TotalAmount = totalAmount,
            GatewayOrderId = payment.GatewayOrderId,
            GatewayPaymentUrl = $"https://mock-payment.com/pay/{payment.GatewayOrderId}",
            IsPaidViaWallet = false,
            Message = "Redirect to payment gateway"
        };
    }

    public async Task<bool> ProcessDeliveryCompletionAsync(Guid deliveryId, CancellationToken ct = default)
    {
        var delivery = await _context.Deliveries.FindAsync(new object[] { deliveryId }, ct);
        if (delivery == null || delivery.AssignedDPId == null)
        {
            return false;
        }

        var pricing = await _context.DeliveryPricings
            .FirstOrDefaultAsync(p => p.DeliveryId == deliveryId, ct);

        if (pricing == null) return false;

        var dpEarning = pricing.DPEarning;
        var platformFee = pricing.PlatformFee;
        var dpcmCommission = pricing.DPCMCommission;

        // Credit DP wallet
        await CreditWalletAsync(delivery.AssignedDPId.Value, dpEarning, "DELIVERY_EARNING",
            $"Earning for delivery - {delivery.Id}", deliveryId.ToString(), "DELIVERY", ct);

        // Create commission record
        var commission = new CommissionRecord
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            DPId = delivery.AssignedDPId.Value,
            DPCMId = null, // Would be set from DP profile
            DeliveryAmount = pricing.TotalAmount,
            DPEarning = dpEarning,
            DPCMCommission = dpcmCommission,
            PlatformFee = platformFee,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<CommissionRecord>().Add(commission);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Processed delivery completion for {DeliveryId}", deliveryId);
        return true;
    }

    public async Task<GetSettlementsResponse> GetSettlementsAsync(Guid userId, GetSettlementsRequest request, CancellationToken ct = default)
    {
        var query = _context.Set<Settlement>()
            .Where(s => s.BeneficiaryId == userId);

        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(s => s.Status == request.Status);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(s => s.SettlementDate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(s => s.SettlementDate <= request.ToDate.Value);
        }

        var totalCount = await query.CountAsync(ct);
        var totalPending = await query.Where(s => s.Status == "PENDING").SumAsync(s => s.NetAmount, ct);

        var settlements = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(s => s.Items)
            .ToListAsync(ct);

        return new GetSettlementsResponse
        {
            Settlements = settlements.Select(s => new SettlementDto
            {
                Id = s.Id,
                SettlementNumber = s.SettlementNumber,
                BeneficiaryId = s.BeneficiaryId,
                BeneficiaryType = s.BeneficiaryType,
                GrossAmount = s.GrossAmount,
                TdsAmount = s.TdsAmount,
                NetAmount = s.NetAmount,
                PayoutMethod = s.PayoutMethod,
                Status = s.Status,
                SettlementDate = s.SettlementDate,
                ProcessedAt = s.ProcessedAt,
                CreatedAt = s.CreatedAt,
                ItemCount = s.Items.Count
            }).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
            TotalPendingAmount = totalPending
        };
    }

    public async Task<SettlementDetailDto?> GetSettlementDetailAsync(Guid settlementId, CancellationToken ct = default)
    {
        var settlement = await _context.Set<Settlement>()
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == settlementId, ct);

        if (settlement == null) return null;

        return new SettlementDetailDto
        {
            Id = settlement.Id,
            SettlementNumber = settlement.SettlementNumber,
            BeneficiaryId = settlement.BeneficiaryId,
            BeneficiaryType = settlement.BeneficiaryType,
            GrossAmount = settlement.GrossAmount,
            TdsAmount = settlement.TdsAmount,
            NetAmount = settlement.NetAmount,
            PayoutMethod = settlement.PayoutMethod,
            Status = settlement.Status,
            SettlementDate = settlement.SettlementDate,
            ProcessedAt = settlement.ProcessedAt,
            CreatedAt = settlement.CreatedAt,
            ItemCount = settlement.Items.Count,
            Items = settlement.Items.Select(i => new SettlementItemDto
            {
                Id = i.Id,
                DeliveryId = i.DeliveryId,
                EarningAmount = i.EarningAmount,
                CommissionAmount = i.CommissionAmount,
                NetAmount = i.NetAmount,
                EarnedAt = i.EarnedAt
            }).ToList()
        };
    }

    public async Task<SettlementDto?> CreateSettlementAsync(CreateSettlementRequest request, CancellationToken ct = default)
    {
        var pendingCommissions = await _context.Set<CommissionRecord>()
            .Where(c => c.DPId == request.BeneficiaryId && c.Status == "PENDING")
            .ToListAsync(ct);

        if (!pendingCommissions.Any())
        {
            return null;
        }

        var grossAmount = pendingCommissions.Sum(c => c.DPEarning);
        var tdsAmount = grossAmount * 0.01m; // 1% TDS
        var netAmount = grossAmount - tdsAmount;

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.Set<Settlement>()
            .CountAsync(s => s.SettlementNumber.StartsWith($"STL-{today}"), ct);
        var settlementNumber = $"STL-{today}-{(count + 1):D4}";

        var settlement = new Settlement
        {
            Id = Guid.NewGuid(),
            SettlementNumber = settlementNumber,
            BeneficiaryId = request.BeneficiaryId,
            BeneficiaryType = "DP",
            GrossAmount = grossAmount,
            TdsAmount = tdsAmount,
            NetAmount = netAmount,
            PayoutMethod = "BANK_TRANSFER",
            Status = "PENDING",
            SettlementDate = request.SettlementDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var commission in pendingCommissions)
        {
            var item = new SettlementItem
            {
                Id = Guid.NewGuid(),
                SettlementId = settlement.Id,
                DeliveryId = commission.DeliveryId,
                EarningAmount = commission.DPEarning,
                CommissionAmount = commission.DPCMCommission,
                NetAmount = commission.DPEarning,
                EarnedAt = commission.CreatedAt
            };
            settlement.Items.Add(item);
            commission.Status = "SETTLED";
        }

        _context.Set<Settlement>().Add(settlement);
        await _context.SaveChangesAsync(ct);

        return new SettlementDto
        {
            Id = settlement.Id,
            SettlementNumber = settlement.SettlementNumber,
            BeneficiaryId = settlement.BeneficiaryId,
            BeneficiaryType = settlement.BeneficiaryType,
            GrossAmount = settlement.GrossAmount,
            TdsAmount = settlement.TdsAmount,
            NetAmount = settlement.NetAmount,
            PayoutMethod = settlement.PayoutMethod,
            Status = settlement.Status,
            SettlementDate = settlement.SettlementDate,
            CreatedAt = settlement.CreatedAt,
            ItemCount = settlement.Items.Count
        };
    }

    public async Task<bool> ProcessSettlementAsync(Guid settlementId, CancellationToken ct = default)
    {
        var settlement = await _context.Set<Settlement>()
            .FirstOrDefaultAsync(s => s.Id == settlementId, ct);

        if (settlement == null || settlement.Status != "PENDING")
        {
            return false;
        }

        settlement.Status = "PROCESSING";
        settlement.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        // Mock payout processing
        settlement.Status = "COMPLETED";
        settlement.ProcessedAt = DateTime.UtcNow;
        settlement.PayoutReference = $"PAYOUT_{Guid.NewGuid():N}";
        settlement.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Processed settlement {SettlementId}", settlementId);
        return true;
    }

    public async Task<EarningsSummaryDto> GetEarningsSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var commissions = await _context.Set<CommissionRecord>()
            .Where(c => c.DPId == userId)
            .ToListAsync(ct);

        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var totalEarnings = commissions.Sum(c => c.DPEarning);
        var todayEarnings = commissions.Where(c => c.CreatedAt.Date == today).Sum(c => c.DPEarning);
        var weekEarnings = commissions.Where(c => c.CreatedAt >= weekStart).Sum(c => c.DPEarning);
        var monthEarnings = commissions.Where(c => c.CreatedAt >= monthStart).Sum(c => c.DPEarning);

        var pendingSettlement = commissions.Where(c => c.Status == "PENDING").Sum(c => c.DPEarning);
        var totalSettled = commissions.Where(c => c.Status == "SETTLED").Sum(c => c.DPEarning);

        return new EarningsSummaryDto
        {
            TotalEarnings = totalEarnings,
            TodayEarnings = todayEarnings,
            WeekEarnings = weekEarnings,
            MonthEarnings = monthEarnings,
            PendingSettlement = pendingSettlement,
            TotalSettled = totalSettled,
            TotalDeliveries = commissions.Count,
            AveragePerDelivery = commissions.Any() ? totalEarnings / commissions.Count : 0
        };
    }

    private WalletDto MapToDto(Wallet wallet)
    {
        return new WalletDto
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            WalletType = wallet.WalletType,
            Balance = wallet.Balance,
            HoldBalance = wallet.HoldBalance,
            Currency = wallet.Currency,
            IsActive = wallet.IsActive,
            UpdatedAt = wallet.UpdatedAt
        };
    }

    private WalletTransactionDto MapTransactionToDto(WalletTransaction transaction)
    {
        return new WalletTransactionDto
        {
            Id = transaction.Id,
            TransactionType = transaction.TransactionType,
            Category = transaction.Category,
            Amount = transaction.Amount,
            BalanceBefore = transaction.BalanceBefore,
            BalanceAfter = transaction.BalanceAfter,
            ReferenceId = transaction.ReferenceId,
            ReferenceType = transaction.ReferenceType,
            Description = transaction.Description,
            Status = transaction.Status,
            CreatedAt = transaction.CreatedAt
        };
    }
}
