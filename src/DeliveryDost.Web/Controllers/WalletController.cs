using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliverX.Application.DTOs.Wallet;
using DeliverX.Application.Services;
using DeliveryDost.Web.ViewModels.Wallet;

namespace DeliveryDost.Web.Controllers;

/// <summary>
/// MVC Controller for wallet management
/// </summary>
[Authorize]
public class WalletController : Controller
{
    private readonly IWalletService _walletService;
    private readonly ILogger<WalletController> _logger;

    public WalletController(
        IWalletService walletService,
        ILogger<WalletController> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    private string GetUserRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    #region Dashboard

    /// <summary>
    /// Wallet dashboard
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        try
        {
            // Get or create wallet
            var wallet = await _walletService.GetOrCreateWalletAsync(userId, userRole);

            // Get recent transactions
            var transactionsResponse = await _walletService.GetTransactionsAsync(userId, new GetTransactionsRequest
            {
                Page = 1,
                PageSize = 5
            });

            var model = new WalletDashboardViewModel
            {
                WalletId = wallet.Id,
                Balance = wallet.Balance,
                HoldBalance = wallet.HoldBalance,
                AvailableBalance = wallet.AvailableBalance,
                Currency = wallet.Currency,
                IsActive = wallet.IsActive,
                UserRole = userRole,
                RecentTransactions = transactionsResponse.Transactions.Select(t => new TransactionItemViewModel
                {
                    Id = t.Id,
                    TransactionType = t.TransactionType,
                    Category = t.Category,
                    Amount = t.Amount,
                    BalanceBefore = t.BalanceBefore,
                    BalanceAfter = t.BalanceAfter,
                    ReferenceId = t.ReferenceId,
                    ReferenceType = t.ReferenceType,
                    Description = t.Description,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt
                }).ToList()
            };

            // Get earnings summary for DP
            if (userRole == "DP")
            {
                var earnings = await _walletService.GetEarningsSummaryAsync(userId);
                model.Earnings = new EarningsSummaryViewModel
                {
                    TotalEarnings = earnings.TotalEarnings,
                    TodayEarnings = earnings.TodayEarnings,
                    WeekEarnings = earnings.WeekEarnings,
                    MonthEarnings = earnings.MonthEarnings,
                    PendingSettlement = earnings.PendingSettlement,
                    TotalSettled = earnings.TotalSettled,
                    TotalDeliveries = earnings.TotalDeliveries,
                    AveragePerDelivery = earnings.AveragePerDelivery
                };
            }

            ViewData["Title"] = "Wallet";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading wallet for user {UserId}", userId);
            TempData["Error"] = "Failed to load wallet";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    #endregion

    #region Transactions

    /// <summary>
    /// Transaction history
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Transactions(string? type, string? category, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var userId = GetUserId();

        try
        {
            var request = new GetTransactionsRequest
            {
                TransactionType = type,
                Category = category,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = 20
            };

            var response = await _walletService.GetTransactionsAsync(userId, request);

            var model = new TransactionListViewModel
            {
                Transactions = response.Transactions.Select(t => new TransactionItemViewModel
                {
                    Id = t.Id,
                    TransactionType = t.TransactionType,
                    Category = t.Category,
                    Amount = t.Amount,
                    BalanceBefore = t.BalanceBefore,
                    BalanceAfter = t.BalanceAfter,
                    ReferenceId = t.ReferenceId,
                    ReferenceType = t.ReferenceType,
                    Description = t.Description,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt
                }).ToList(),
                TotalCount = response.TotalCount,
                Page = response.Page,
                PageSize = response.PageSize,
                TypeFilter = type,
                CategoryFilter = category,
                FromDate = fromDate,
                ToDate = toDate,
                TotalCredits = response.Transactions.Where(t => t.TransactionType == "CREDIT").Sum(t => t.Amount),
                TotalDebits = response.Transactions.Where(t => t.TransactionType == "DEBIT").Sum(t => t.Amount)
            };

            ViewData["Title"] = "Transactions";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading transactions for user {UserId}", userId);
            TempData["Error"] = "Failed to load transactions";
            return RedirectToAction("Index");
        }
    }

    #endregion

    #region Top-up

    /// <summary>
    /// Top-up form
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TopUp()
    {
        var userId = GetUserId();

        try
        {
            var wallet = await _walletService.GetWalletAsync(userId);

            var model = new TopUpViewModel
            {
                CurrentBalance = wallet?.Balance ?? 0
            };

            ViewData["Title"] = "Top Up Wallet";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading top-up page for user {UserId}", userId);
            TempData["Error"] = "Failed to load page";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Initiate top-up payment
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TopUp(TopUpViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var wallet = await _walletService.GetWalletAsync(GetUserId());
            model.CurrentBalance = wallet?.Balance ?? 0;
            ViewData["Title"] = "Top Up Wallet";
            return View(model);
        }

        var userId = GetUserId();

        try
        {
            var request = new RechargeWalletRequest
            {
                Amount = model.Amount,
                PaymentMethod = model.PaymentMethod
            };

            var response = await _walletService.InitiateRechargeAsync(userId, request);

            if (response.IsSuccess && response.PaymentId.HasValue)
            {
                // In production, redirect to payment gateway
                // For now, simulate success
                return RedirectToAction("ProcessPayment", new
                {
                    paymentId = response.PaymentId,
                    gatewayUrl = response.GatewayPaymentUrl
                });
            }
            else
            {
                TempData["Error"] = response.Message ?? "Failed to initiate payment";
                return RedirectToAction("TopUp");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating top-up for user {UserId}", userId);
            TempData["Error"] = "Payment initiation failed";
            return RedirectToAction("TopUp");
        }
    }

    /// <summary>
    /// Process payment page (simulates payment gateway)
    /// </summary>
    [HttpGet]
    public IActionResult ProcessPayment(Guid paymentId, string? gatewayUrl)
    {
        var model = new PaymentProcessingViewModel
        {
            PaymentId = paymentId,
            GatewayPaymentUrl = gatewayUrl,
            Status = "PROCESSING"
        };

        ViewData["Title"] = "Processing Payment";
        return View(model);
    }

    /// <summary>
    /// Simulate payment completion (for dev/testing)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulatePayment(Guid paymentId, bool success = true)
    {
        var userId = GetUserId();

        try
        {
            var confirmRequest = new ConfirmPaymentRequest
            {
                PaymentId = paymentId,
                GatewayTransactionId = $"SIM_{DateTime.UtcNow.Ticks}",
                Status = success ? "SUCCESS" : "FAILED",
                FailureReason = success ? null : "Simulated failure"
            };

            var result = await _walletService.ConfirmPaymentAsync(confirmRequest);

            if (result)
            {
                TempData["Success"] = "Payment completed successfully!";
            }
            else
            {
                TempData["Error"] = "Payment verification failed";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment {PaymentId}", paymentId);
            TempData["Error"] = "Payment confirmation failed";
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Payment callback from gateway
    /// </summary>
    [HttpGet]
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback(Guid paymentId, string status, string? transactionId, string? error)
    {
        try
        {
            var confirmRequest = new ConfirmPaymentRequest
            {
                PaymentId = paymentId,
                GatewayTransactionId = transactionId ?? "",
                Status = status.ToUpper() == "SUCCESS" ? "SUCCESS" : "FAILED",
                FailureReason = error
            };

            await _walletService.ConfirmPaymentAsync(confirmRequest);

            if (status.ToUpper() == "SUCCESS")
            {
                TempData["Success"] = "Wallet recharged successfully!";
            }
            else
            {
                TempData["Error"] = error ?? "Payment failed";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment callback for {PaymentId}", paymentId);
            TempData["Error"] = "Payment processing error";
        }

        return RedirectToAction("Index");
    }

    #endregion

    #region Settlements (DP only)

    /// <summary>
    /// Settlements list for DP
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "DP")]
    public async Task<IActionResult> Settlements(string? status, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var userId = GetUserId();

        try
        {
            var request = new GetSettlementsRequest
            {
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = 20
            };

            var response = await _walletService.GetSettlementsAsync(userId, request);

            var model = new SettlementListViewModel
            {
                Settlements = response.Settlements.Select(s => new SettlementItemViewModel
                {
                    Id = s.Id,
                    SettlementNumber = s.SettlementNumber,
                    GrossAmount = s.GrossAmount,
                    TdsAmount = s.TdsAmount,
                    NetAmount = s.NetAmount,
                    PayoutMethod = s.PayoutMethod,
                    Status = s.Status,
                    SettlementDate = s.SettlementDate,
                    ProcessedAt = s.ProcessedAt,
                    ItemCount = s.ItemCount
                }).ToList(),
                TotalCount = response.TotalCount,
                Page = response.Page,
                PageSize = response.PageSize,
                TotalPendingAmount = response.TotalPendingAmount,
                StatusFilter = status,
                FromDate = fromDate,
                ToDate = toDate
            };

            ViewData["Title"] = "Settlements";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settlements for user {UserId}", userId);
            TempData["Error"] = "Failed to load settlements";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Settlement detail
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "DP")]
    public async Task<IActionResult> SettlementDetail(Guid id)
    {
        try
        {
            var settlement = await _walletService.GetSettlementDetailAsync(id);

            if (settlement == null)
            {
                TempData["Error"] = "Settlement not found";
                return RedirectToAction("Settlements");
            }

            ViewData["Title"] = $"Settlement #{settlement.SettlementNumber}";
            return View(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settlement detail {SettlementId}", id);
            TempData["Error"] = "Failed to load settlement";
            return RedirectToAction("Settlements");
        }
    }

    #endregion

    #region AJAX Endpoints

    /// <summary>
    /// Get current balance (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBalance()
    {
        var userId = GetUserId();

        try
        {
            var wallet = await _walletService.GetWalletAsync(userId);

            return Json(new
            {
                success = true,
                balance = wallet?.Balance ?? 0,
                availableBalance = wallet?.AvailableBalance ?? 0,
                holdBalance = wallet?.HoldBalance ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for user {UserId}", userId);
            return Json(new { success = false });
        }
    }

    #endregion
}
