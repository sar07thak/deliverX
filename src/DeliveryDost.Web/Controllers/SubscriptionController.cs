using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliverX.Application.Services;
using DeliveryDost.Web.ViewModels.Business;

namespace DeliveryDost.Web.Controllers;

[Authorize(Roles = "BC,DBC")]
public class SubscriptionController : Controller
{
    private readonly IWalletService _walletService;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        IWalletService walletService,
        ILogger<SubscriptionController> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    /// <summary>
    /// View available subscription plans
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();

        try
        {
            // Define available plans (in production, this would come from database)
            var plans = GetAvailablePlans();

            // Check if user has an active subscription (mock for now)
            SubscriptionViewModel? currentSubscription = null;

            // In production, fetch from database
            // var currentSubscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

            var model = new SubscriptionListViewModel
            {
                Plans = plans,
                CurrentSubscription = currentSubscription
            };

            ViewData["Title"] = "Subscription Plans";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading subscription plans for user {UserId}", userId);
            TempData["Error"] = "Failed to load subscription plans";
            return RedirectToAction("Business", "Dashboard");
        }
    }

    /// <summary>
    /// Subscribe to a plan
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Subscribe(string planId, string cycle = "MONTHLY")
    {
        var userId = GetUserId();

        try
        {
            var plans = GetAvailablePlans();
            var plan = plans.FirstOrDefault(p => p.PlanId == planId);

            if (plan == null)
            {
                TempData["Error"] = "Invalid plan selected";
                return RedirectToAction("Index");
            }

            var wallet = await _walletService.GetWalletAsync(userId);
            var price = cycle == "ANNUAL" ? plan.AnnualPrice : plan.MonthlyPrice;

            var model = new SubscribeViewModel
            {
                PlanId = plan.PlanId,
                PlanName = plan.Name,
                BillingCycle = cycle,
                Price = price,
                WalletBalance = wallet?.Balance ?? 0,
                AutoRenew = true
            };

            ViewData["Title"] = $"Subscribe to {plan.Name}";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading subscribe page for user {UserId}", userId);
            TempData["Error"] = "Failed to load subscription page";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Confirm subscription
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(SubscribeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = $"Subscribe to {model.PlanName}";
            return View(model);
        }

        var userId = GetUserId();

        try
        {
            var wallet = await _walletService.GetWalletAsync(userId);
            if (wallet == null || wallet.Balance < model.Price)
            {
                TempData["Error"] = "Insufficient wallet balance. Please add funds first.";
                return RedirectToAction("TopUp", "Wallet");
            }

            // In production, create subscription record and deduct from wallet
            // await _subscriptionService.CreateSubscriptionAsync(userId, model.PlanId, model.BillingCycle, model.AutoRenew);
            // await _walletService.DeductAsync(userId, model.Price, "Subscription", $"Subscription to {model.PlanName}");

            _logger.LogInformation("User {UserId} subscribed to plan {PlanId} ({Cycle})",
                userId, model.PlanId, model.BillingCycle);

            TempData["Success"] = $"Successfully subscribed to {model.PlanName}!";
            return RedirectToAction("MySubscription");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing subscription for user {UserId}", userId);
            TempData["Error"] = "Failed to process subscription. Please try again.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// View current subscription
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> MySubscription()
    {
        var userId = GetUserId();

        try
        {
            // In production, fetch from database
            // var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

            // For now, show a message that no subscription is active
            ViewData["Title"] = "My Subscription";
            return View((SubscriptionViewModel?)null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading subscription for user {UserId}", userId);
            TempData["Error"] = "Failed to load subscription details";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel()
    {
        var userId = GetUserId();

        try
        {
            // In production, cancel the subscription
            // await _subscriptionService.CancelSubscriptionAsync(userId);

            _logger.LogInformation("User {UserId} cancelled subscription", userId);

            TempData["Success"] = "Your subscription has been cancelled. It will remain active until the end of the current billing period.";
            return RedirectToAction("MySubscription");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for user {UserId}", userId);
            TempData["Error"] = "Failed to cancel subscription. Please try again.";
            return RedirectToAction("MySubscription");
        }
    }

    /// <summary>
    /// Toggle auto-renew
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAutoRenew(bool autoRenew)
    {
        var userId = GetUserId();

        try
        {
            // In production, update the subscription
            // await _subscriptionService.UpdateAutoRenewAsync(userId, autoRenew);

            TempData["Success"] = autoRenew
                ? "Auto-renewal has been enabled"
                : "Auto-renewal has been disabled";
            return RedirectToAction("MySubscription");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating auto-renew for user {UserId}", userId);
            TempData["Error"] = "Failed to update settings. Please try again.";
            return RedirectToAction("MySubscription");
        }
    }

    /// <summary>
    /// View subscription history
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> History(int page = 1)
    {
        var userId = GetUserId();

        try
        {
            // In production, fetch from database
            var model = new SubscriptionHistoryViewModel
            {
                History = new List<SubscriptionHistoryItem>(),
                TotalCount = 0,
                Page = page,
                PageSize = 20
            };

            ViewData["Title"] = "Subscription History";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading subscription history for user {UserId}", userId);
            TempData["Error"] = "Failed to load subscription history";
            return RedirectToAction("Index");
        }
    }

    private List<SubscriptionPlanViewModel> GetAvailablePlans()
    {
        return new List<SubscriptionPlanViewModel>
        {
            new()
            {
                PlanId = "STARTER",
                Name = "Starter",
                Description = "Perfect for small businesses just getting started",
                MonthlyPrice = 999,
                AnnualPrice = 9990,
                DiscountPercent = 17,
                MaxDeliveriesPerMonth = 100,
                DiscountOnDeliveries = 5,
                HasPrioritySupport = false,
                HasDedicatedManager = false,
                HasApiAccess = false,
                Features = new List<string>
                {
                    "Up to 100 deliveries/month",
                    "5% discount on delivery fees",
                    "Basic analytics dashboard",
                    "Email support"
                }
            },
            new()
            {
                PlanId = "GROWTH",
                Name = "Growth",
                Description = "For growing businesses with increasing delivery needs",
                MonthlyPrice = 2499,
                AnnualPrice = 24990,
                DiscountPercent = 17,
                MaxDeliveriesPerMonth = 500,
                DiscountOnDeliveries = 10,
                HasPrioritySupport = true,
                HasDedicatedManager = false,
                HasApiAccess = true,
                IsPopular = true,
                Features = new List<string>
                {
                    "Up to 500 deliveries/month",
                    "10% discount on delivery fees",
                    "Advanced analytics & reports",
                    "Priority phone & email support",
                    "API access for integrations",
                    "Bulk upload feature"
                }
            },
            new()
            {
                PlanId = "ENTERPRISE",
                Name = "Enterprise",
                Description = "Full-featured plan for high-volume businesses",
                MonthlyPrice = 4999,
                AnnualPrice = 49990,
                DiscountPercent = 17,
                MaxDeliveriesPerMonth = -1, // Unlimited
                DiscountOnDeliveries = 15,
                HasPrioritySupport = true,
                HasDedicatedManager = true,
                HasApiAccess = true,
                Features = new List<string>
                {
                    "Unlimited deliveries",
                    "15% discount on delivery fees",
                    "Custom analytics & reporting",
                    "24/7 dedicated support",
                    "Dedicated account manager",
                    "Full API access",
                    "Custom integrations",
                    "SLA guarantees"
                }
            }
        };
    }
}
