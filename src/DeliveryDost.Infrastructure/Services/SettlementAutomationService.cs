using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

/// <summary>
/// Background service for automated settlement processing
/// Runs daily to process pending settlements for DPs and DPCMs
/// </summary>
public class SettlementAutomationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SettlementAutomationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour
    private readonly TimeSpan _settlementTime = new(2, 0, 0); // 2 AM daily

    public SettlementAutomationService(
        IServiceProvider serviceProvider,
        ILogger<SettlementAutomationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SettlementAutomationService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check if it's time to run settlement (around 2 AM)
                var now = DateTime.UtcNow;
                if (now.Hour == _settlementTime.Hours && now.Minute < 5)
                {
                    _logger.LogInformation("Starting daily settlement processing at {Time}", now);
                    await ProcessDailySettlementsAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SettlementAutomationService");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("SettlementAutomationService stopped");
    }

    /// <summary>
    /// Process all pending settlements for DPs and DPCMs
    /// </summary>
    private async Task ProcessDailySettlementsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        // Process DP settlements
        await ProcessDPSettlementsAsync(context, yesterday, cancellationToken);

        // Process DPCM settlements
        await ProcessDPCMSettlementsAsync(context, yesterday, cancellationToken);

        _logger.LogInformation("Daily settlement processing completed for {Date}", yesterday);
    }

    /// <summary>
    /// Create settlements for all DPs with pending earnings
    /// </summary>
    private async Task ProcessDPSettlementsAsync(ApplicationDbContext context, DateTime forDate, CancellationToken cancellationToken)
    {
        // Get all commission records that haven't been settled
        var pendingCommissions = await context.CommissionRecords
            .Include(c => c.DP)
            .Where(c => c.Status == "PENDING")
            .Where(c => c.CreatedAt.Date <= forDate)
            .GroupBy(c => c.DPId)
            .ToListAsync(cancellationToken);

        foreach (var dpGroup in pendingCommissions)
        {
            var dpId = dpGroup.Key;
            var commissions = dpGroup.ToList();

            if (!commissions.Any()) continue;

            var dp = commissions.First().DP;
            if (dp == null) continue;

            // Get DP's bank details from BankVerification
            var bankVerification = await context.BankVerifications
                .FirstOrDefaultAsync(b => b.UserId == dpId && b.VerifiedAt != null, cancellationToken);

            var grossAmount = commissions.Sum(c => c.DPEarning);
            var tdsRate = 0.01m; // 1% TDS for example
            var tdsAmount = Math.Round(grossAmount * tdsRate, 2);
            var netAmount = grossAmount - tdsAmount;

            if (netAmount < 100) // Minimum settlement threshold
            {
                _logger.LogInformation("DP {DPId} has net amount {Amount} below threshold, skipping", dpId, netAmount);
                continue;
            }

            // Create settlement
            var settlement = new Settlement
            {
                Id = Guid.NewGuid(),
                SettlementNumber = GenerateSettlementNumber(),
                BeneficiaryId = dpId,
                BeneficiaryType = "DP",
                GrossAmount = grossAmount,
                TdsAmount = tdsAmount,
                NetAmount = netAmount,
                BankAccountNumber = bankVerification != null ? $"XXXX-{bankVerification.BankName}" : null,
                BankIfscCode = bankVerification?.IFSCCode,
                PayoutMethod = "BANK_TRANSFER",
                Status = "PENDING",
                SettlementDate = forDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Settlements.Add(settlement);

            // Create settlement items
            foreach (var commission in commissions)
            {
                var item = new SettlementItem
                {
                    Id = Guid.NewGuid(),
                    SettlementId = settlement.Id,
                    DeliveryId = commission.DeliveryId,
                    EarningAmount = commission.DPEarning,
                    CommissionAmount = commission.PlatformFee,
                    NetAmount = commission.DPEarning,
                    EarnedAt = commission.CreatedAt
                };
                context.SettlementItems.Add(item);

                // Mark commission as settled
                commission.Status = "SETTLED";
            }

            _logger.LogInformation("Created settlement {Number} for DP {DPId}: Gross={Gross}, TDS={TDS}, Net={Net}",
                settlement.SettlementNumber, dpId, grossAmount, tdsAmount, netAmount);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Create settlements for all DPCMs with pending commissions
    /// </summary>
    private async Task ProcessDPCMSettlementsAsync(ApplicationDbContext context, DateTime forDate, CancellationToken cancellationToken)
    {
        // Get all commission records with DPCM commission that haven't been settled
        var pendingCommissions = await context.CommissionRecords
            .Include(c => c.DPCM)
            .Where(c => c.Status == "SETTLED" && c.DPCMId != null && c.DPCMCommission > 0)
            .ToListAsync(cancellationToken);

        // Group by DPCM
        var dpcmGroups = pendingCommissions.GroupBy(c => c.DPCMId).ToList();

        foreach (var dpcmGroup in dpcmGroups)
        {
            var dpcmId = dpcmGroup.Key;
            if (!dpcmId.HasValue) continue;

            var commissions = dpcmGroup.ToList();
            if (!commissions.Any()) continue;

            var dpcm = commissions.First().DPCM;
            if (dpcm == null) continue;

            // Get DPCM's bank details from BankVerification
            var bankVerification = await context.BankVerifications
                .FirstOrDefaultAsync(b => b.UserId == dpcmId.Value && b.VerifiedAt != null, cancellationToken);

            var grossAmount = commissions.Sum(c => c.DPCMCommission);
            var tdsRate = 0.05m; // 5% TDS for DPCM
            var tdsAmount = Math.Round(grossAmount * tdsRate, 2);
            var netAmount = grossAmount - tdsAmount;

            if (netAmount < 500) // Higher threshold for DPCM
            {
                _logger.LogInformation("DPCM {DPCMId} has net amount {Amount} below threshold, skipping", dpcmId, netAmount);
                continue;
            }

            // Create settlement
            var settlement = new Settlement
            {
                Id = Guid.NewGuid(),
                SettlementNumber = GenerateSettlementNumber(),
                BeneficiaryId = dpcmId.Value,
                BeneficiaryType = "DPCM",
                GrossAmount = grossAmount,
                TdsAmount = tdsAmount,
                NetAmount = netAmount,
                BankAccountNumber = bankVerification != null ? $"XXXX-{bankVerification.BankName}" : null,
                BankIfscCode = bankVerification?.IFSCCode,
                PayoutMethod = "BANK_TRANSFER",
                Status = "PENDING",
                SettlementDate = forDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Settlements.Add(settlement);

            _logger.LogInformation("Created settlement {Number} for DPCM {DPCMId}: Gross={Gross}, TDS={TDS}, Net={Net}",
                settlement.SettlementNumber, dpcmId, grossAmount, tdsAmount, netAmount);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Generate unique settlement number
    /// </summary>
    private string GenerateSettlementNumber()
    {
        return $"STL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
    }
}

/// <summary>
/// Background service for tracking shipments and syncing status
/// </summary>
public class ShipmentTrackingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ShipmentTrackingService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

    public ShipmentTrackingService(
        IServiceProvider serviceProvider,
        ILogger<ShipmentTrackingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ShipmentTrackingService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncPendingShipmentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShipmentTrackingService");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("ShipmentTrackingService stopped");
    }

    private async Task SyncPendingShipmentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Get all shipments that need tracking updates
        var activeStatuses = new[] { "CREATED", "PICKUP_SCHEDULED", "PICKED_UP", "IN_TRANSIT", "OUT_FOR_DELIVERY" };

        var shipmentsToTrack = await context.CourierShipments
            .Where(s => activeStatuses.Contains(s.Status))
            .Where(s => s.LastTrackedAt == null || s.LastTrackedAt < DateTime.UtcNow.AddMinutes(-30))
            .Take(100) // Batch of 100
            .ToListAsync(cancellationToken);

        foreach (var shipment in shipmentsToTrack)
        {
            // In real implementation, call courier tracking API here
            // For now, just update the last tracked timestamp
            shipment.LastTrackedAt = DateTime.UtcNow;
            shipment.UpdatedAt = DateTime.UtcNow;
        }

        if (shipmentsToTrack.Any())
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Synced tracking for {Count} shipments", shipmentsToTrack.Count);
        }
    }
}
