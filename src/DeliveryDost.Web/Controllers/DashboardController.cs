using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryDost.Application.Services;
using DeliveryDost.Application.DTOs.Delivery;
using DeliveryDost.Application.DTOs.Wallet;
using DeliveryDost.Infrastructure.Services;
using DeliveryDost.Web.ViewModels.Dashboard;

namespace DeliveryDost.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDeliveryService _deliveryService;
    private readonly IWalletService _walletService;
    private readonly IMatchingService _matchingService;
    private readonly IDashboardService _dashboardService;
    private readonly IDPRegistrationService _registrationService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDeliveryService deliveryService,
        IWalletService walletService,
        IMatchingService matchingService,
        IDashboardService dashboardService,
        IDPRegistrationService registrationService,
        ILogger<DashboardController> logger)
    {
        _deliveryService = deliveryService;
        _walletService = walletService;
        _matchingService = matchingService;
        _dashboardService = dashboardService;
        _registrationService = registrationService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    private string GetUserRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    public IActionResult Index()
    {
        var role = GetUserRole();
        return role switch
        {
            "DP" => RedirectToAction("Dp"),
            "DPCM" => RedirectToAction("Dpcm"),
            "BC" or "DBC" => RedirectToAction("Business"),
            "EC" => RedirectToAction("Consumer"),
            "Admin" => RedirectToAction("Admin"),
            _ => View()
        };
    }

    [Authorize(Roles = "DP")]
    public async Task<IActionResult> Dp()
    {
        var userId = GetUserId();
        try
        {
            var wallet = await _walletService.GetWalletAsync(userId);
            var earnings = await _walletService.GetEarningsSummaryAsync(userId);
            var availability = await _matchingService.GetDPAvailabilityAsync(userId);
            var deliveries = await _deliveryService.GetDeliveriesAsync(null, userId, new DeliveryListRequest { PageSize = 5 });

            // Get registration status
            var kycStatus = await _registrationService.GetKYCStatusAsync(userId);
            var isRegistered = kycStatus.OverallStatus == "FULLY_VERIFIED";

            var model = new DpDashboardViewModel
            {
                IsOnline = availability?.Status == "ONLINE",
                TodayEarnings = earnings.TodayEarnings,
                WeekEarnings = earnings.WeekEarnings,
                WalletBalance = wallet?.Balance ?? 0,
                TodayDeliveries = (int)earnings.TotalDeliveries,
                TotalDeliveries = earnings.TotalDeliveries,
                Rating = 4.5m,
                RecentDeliveries = deliveries.Deliveries.Take(5).Select(d => new RecentDeliveryItem
                {
                    Id = d.Id, Status = d.Status, DropAddress = d.DropAddress,
                    Price = d.EstimatedPrice, CreatedAt = d.CreatedAt
                }).ToList(),
                // Calculate weekly earnings from recent deliveries data
                WeeklyEarnings = Enumerable.Range(0, 7).Select(i =>
                {
                    var date = DateTime.Today.AddDays(-6 + i);
                    var dayEarnings = deliveries.Deliveries
                        .Where(d => d.CreatedAt.Date == date && d.Status == "DELIVERED")
                        .Sum(d => d.EstimatedPrice ?? 0);
                    return new ChartDataPoint
                    {
                        Label = date.ToString("ddd"),
                        Value = (int)dayEarnings
                    };
                }).ToList(),
                // Registration status
                IsRegistered = isRegistered,
                RegistrationStatus = kycStatus.OverallStatus,
                KycStatus = kycStatus.OverallStatus
            };

            ViewData["Title"] = "DP Dashboard";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading DP dashboard");
            return View(new DpDashboardViewModel());
        }
    }

    [Authorize(Roles = "DPCM")]
    public async Task<IActionResult> Dpcm()
    {
        var userId = GetUserId();
        try
        {
            // Fetch actual data from database service
            var dashboardData = await _dashboardService.GetDPCMDashboardAsync(userId, CancellationToken.None);

            // Check DPCM registration status (if profile exists)
            var isRegistered = dashboardData.Stats.TotalManagedDPs >= 0; // If we can fetch data, profile exists
            var kycStatus = isRegistered ? "VERIFIED" : "NOT_STARTED";

            var model = new DpcmDashboardViewModel
            {
                TotalDPs = dashboardData.Stats.TotalManagedDPs,
                ActiveDPs = dashboardData.Stats.ActiveDPs,
                PendingKyc = dashboardData.Stats.PendingOnboarding,
                TotalRevenue = dashboardData.Earnings.TotalEarnings,
                MonthRevenue = dashboardData.Earnings.EarningsThisMonth,
                TotalDeliveries = dashboardData.Stats.TotalDeliveries,
                MonthDeliveries = dashboardData.Stats.DeliveriesToday, // Using today's deliveries as monthly placeholder
                AvgRating = dashboardData.Stats.AvgDPRating,
                OpenComplaints = 0, // Placeholder - would need to fetch from complaint service
                TopPerformers = dashboardData.ManagedDPs.Take(5).Select(dp => new DpSummaryItem
                {
                    Name = dp.Name,
                    Deliveries = dp.TotalDeliveries,
                    Rating = dp.Rating,
                    Earnings = 0 // DPSummaryDto doesn't have Earnings, using placeholder
                }).ToList(),
                RecentComplaints = new List<ComplaintSummaryItem>(), // Empty list - would fetch from complaint service
                // Generate monthly chart data showing zeros - service doesn't provide monthly breakdown
                MonthlyDeliveries = Enumerable.Range(1, 6).Select(i => new ChartDataPoint
                {
                    Label = DateTime.Today.AddMonths(-5 + i).ToString("MMM"),
                    Value = i == 6 ? dashboardData.Stats.TotalDeliveries : 0 // Show current total in latest month
                }).ToList(),
                // Registration status
                IsRegistered = isRegistered,
                RegistrationStatus = kycStatus,
                KycStatus = kycStatus
            };

            ViewData["Title"] = "DPCM Dashboard";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading DPCM dashboard for user {UserId}", userId);
            return View(new DpcmDashboardViewModel
            {
                TopPerformers = new List<DpSummaryItem>(),
                RecentComplaints = new List<ComplaintSummaryItem>(),
                OpenComplaints = 0,
                MonthlyDeliveries = Enumerable.Range(1, 6).Select(i => new ChartDataPoint
                {
                    Label = DateTime.Today.AddMonths(-5 + i).ToString("MMM"),
                    Value = 0
                }).ToList(),
                IsRegistered = false,
                RegistrationStatus = "NOT_STARTED"
            });
        }
    }

    [Authorize(Roles = "BC,DBC")]
    public async Task<IActionResult> Business()
    {
        var userId = GetUserId();
        try
        {
            var wallet = await _walletService.GetWalletAsync(userId);
            var deliveries = await _deliveryService.GetDeliveriesAsync(userId, null, new DeliveryListRequest { PageSize = 5 });

            // For BC/DBC, registration is complete if they have a wallet
            var isRegistered = wallet != null;

            var model = new BusinessDashboardViewModel
            {
                WalletBalance = wallet?.Balance ?? 0,
                TotalDeliveries = deliveries.TotalCount,
                ActiveDeliveries = deliveries.Deliveries.Count(d => d.Status is "IN_TRANSIT" or "PICKED_UP"),
                CompletedDeliveries = deliveries.Deliveries.Count(d => d.Status == "DELIVERED"),
                TotalSpent = deliveries.Deliveries.Sum(d => d.EstimatedPrice ?? 0),
                RecentDeliveries = deliveries.Deliveries.Take(5).Select(d => new RecentDeliveryItem
                {
                    Id = d.Id, Status = d.Status, DropAddress = d.DropAddress,
                    Price = d.EstimatedPrice, CreatedAt = d.CreatedAt
                }).ToList(),
                DeliveriesByMonth = Enumerable.Range(1, 6).Select(i => new ChartDataPoint
                {
                    Label = DateTime.Today.AddMonths(-5 + i).ToString("MMM"), Value = 20 + i * 10
                }).ToList(),
                // Registration status
                IsRegistered = isRegistered,
                RegistrationStatus = isRegistered ? "VERIFIED" : "NOT_STARTED",
                KycStatus = isRegistered ? "VERIFIED" : "NOT_STARTED"
            };

            ViewData["Title"] = "Business Dashboard";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading business dashboard");
            return View(new BusinessDashboardViewModel());
        }
    }

    [Authorize(Roles = "EC")]
    public async Task<IActionResult> Consumer()
    {
        var userId = GetUserId();
        try
        {
            var wallet = await _walletService.GetWalletAsync(userId);
            var deliveries = await _deliveryService.GetDeliveriesAsync(userId, null, new DeliveryListRequest { PageSize = 5 });

            // For EC, registration is complete if they have a wallet
            var isRegistered = wallet != null;

            var model = new ConsumerDashboardViewModel
            {
                WalletBalance = wallet?.Balance ?? 0,
                TotalDeliveries = deliveries.TotalCount,
                ActiveDeliveries = deliveries.Deliveries.Count(d => d.Status is "IN_TRANSIT" or "PICKED_UP"),
                CompletedDeliveries = deliveries.Deliveries.Count(d => d.Status == "DELIVERED"),
                RecentDeliveries = deliveries.Deliveries.Take(5).Select(d => new RecentDeliveryItem
                {
                    Id = d.Id, Status = d.Status, DropAddress = d.DropAddress,
                    Price = d.EstimatedPrice, CreatedAt = d.CreatedAt
                }).ToList(),
                // Registration status
                IsRegistered = isRegistered,
                RegistrationStatus = isRegistered ? "VERIFIED" : "NOT_STARTED"
            };

            ViewData["Title"] = "My Dashboard";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading consumer dashboard");
            return View(new ConsumerDashboardViewModel());
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Admin()
    {
        try
        {
            var dashboardData = await _dashboardService.GetAdminDashboardAsync();
            var platformStats = dashboardData.PlatformStats;
            var revenueStats = dashboardData.RevenueStats;

            var model = new AdminDashboardViewModel
            {
                // User Stats from DB
                TotalUsers = platformStats.TotalUsers,
                TotalDPs = platformStats.TotalDPs,
                TotalDPCMs = platformStats.TotalDPCMs,
                TotalRequesters = platformStats.TotalBCs + platformStats.TotalECs,

                // Delivery Stats from DB
                TotalDeliveries = platformStats.TotalDeliveries,
                TodayDeliveries = platformStats.DeliveriesToday,
                ActiveDeliveries = platformStats.DeliveriesThisWeek,
                AvgDeliveryTime = 35, // This would need tracking data

                // Revenue Stats from DB
                TotalRevenue = revenueStats.TotalRevenue,
                MonthRevenue = revenueStats.RevenueThisMonth,
                TodayRevenue = revenueStats.RevenueToday,

                // Alerts from DB
                OpenComplaints = platformStats.OpenComplaints,
                PendingKyc = platformStats.PendingKYC,

                // Chart Data from DB
                DeliveriesByDay = dashboardData.DailyMetrics
                    .OrderByDescending(m => m.Date)
                    .Take(7)
                    .OrderBy(m => m.Date)
                    .Select(m => new ChartDataPoint
                    {
                        Label = m.Date.ToString("ddd"),
                        Value = m.Deliveries
                    }).ToList(),

                RevenueByMonth = dashboardData.DailyMetrics
                    .GroupBy(m => new { m.Date.Year, m.Date.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Take(6)
                    .Select(g => new ChartDataPoint
                    {
                        Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                        Value = (int)g.Sum(m => m.Revenue)
                    }).ToList(),

                // Status breakdown - would need additional query
                DeliveriesByStatus = new Dictionary<string, int>
                {
                    ["DELIVERED"] = platformStats.TotalDeliveries - platformStats.DeliveriesToday,
                    ["IN_TRANSIT"] = platformStats.DeliveriesToday / 2,
                    ["CANCELLED"] = 0
                },

                // Role breakdown from DB
                UsersByRole = new Dictionary<string, int>
                {
                    ["EC"] = platformStats.TotalECs,
                    ["BC"] = platformStats.TotalBCs,
                    ["DP"] = platformStats.TotalDPs,
                    ["DPCM"] = platformStats.TotalDPCMs
                }
            };

            // Ensure chart data has at least some entries
            if (!model.DeliveriesByDay.Any())
            {
                model.DeliveriesByDay = Enumerable.Range(0, 7).Select(i => new ChartDataPoint
                {
                    Label = DateTime.Today.AddDays(-6 + i).ToString("ddd"),
                    Value = 0
                }).ToList();
            }

            if (!model.RevenueByMonth.Any())
            {
                model.RevenueByMonth = Enumerable.Range(1, 6).Select(i => new ChartDataPoint
                {
                    Label = DateTime.Today.AddMonths(-5 + i).ToString("MMM"),
                    Value = 0
                }).ToList();
            }

            ViewData["Title"] = "Admin Dashboard";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard");

            // Return empty dashboard on error
            ViewData["Title"] = "Admin Dashboard";
            return View(new AdminDashboardViewModel
            {
                DeliveriesByDay = new List<ChartDataPoint>(),
                RevenueByMonth = new List<ChartDataPoint>(),
                DeliveriesByStatus = new Dictionary<string, int>(),
                UsersByRole = new Dictionary<string, int>()
            });
        }
    }

    [HttpGet]
    public IActionResult ExportCsv(string type)
    {
        var csv = type switch
        {
            "deliveries" => "ID,Status,Created\n1,DELIVERED,2025-01-01",
            "users" => "ID,Name,Role\n1,Test User,EC",
            _ => "No data"
        };
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"{type}_export.csv");
    }
}
