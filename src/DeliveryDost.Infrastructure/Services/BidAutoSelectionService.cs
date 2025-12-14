using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

/// <summary>
/// Background service for automatic bid processing:
/// 1. Expires bids that have passed their expiry time
/// 2. Auto-selects lowest bid for deliveries past the auto-selection window
/// </summary>
public class BidAutoSelectionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BidAutoSelectionService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public BidAutoSelectionService(
        IServiceProvider serviceProvider,
        ILogger<BidAutoSelectionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BidAutoSelectionService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredBidsAsync(stoppingToken);
                await ProcessAutoSelectionAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BidAutoSelectionService");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("BidAutoSelectionService stopped");
    }

    /// <summary>
    /// Expire bids that have passed their expiry time
    /// </summary>
    private async Task ProcessExpiredBidsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        // Get all pending bids that have expired
        var expiredBids = await context.DeliveryBids
            .Where(b => b.Status == "PENDING" && b.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expiredBids.Count == 0) return;

        foreach (var bid in expiredBids)
        {
            bid.Status = "EXPIRED";
            bid.UpdatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Expired {Count} bids", expiredBids.Count);
    }

    /// <summary>
    /// Auto-select lowest bid for deliveries that have passed the auto-selection window
    /// </summary>
    private async Task ProcessAutoSelectionAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        // Get bidding configuration
        var config = await context.BiddingConfigs
            .FirstOrDefaultAsync(c => c.IsActive, cancellationToken);

        if (config == null || !config.AutoSelectLowestBid)
        {
            return; // Auto-selection is disabled
        }

        var autoSelectThreshold = now.AddMinutes(-config.AutoSelectAfterMinutes);

        // Get deliveries eligible for auto-selection:
        // - Status is CREATED or MATCHING
        // - Created before the auto-select threshold
        // - Has at least one pending bid
        var eligibleDeliveries = await context.Deliveries
            .Where(d => (d.Status == "CREATED" || d.Status == "MATCHING"))
            .Where(d => d.CreatedAt <= autoSelectThreshold)
            .Where(d => d.AssignedDPId == null)
            .ToListAsync(cancellationToken);

        foreach (var delivery in eligibleDeliveries)
        {
            // Get the lowest pending bid for this delivery
            var lowestBid = await context.DeliveryBids
                .Where(b => b.DeliveryId == delivery.Id && b.Status == "PENDING" && b.ExpiresAt > now)
                .OrderBy(b => b.BidAmount)
                .FirstOrDefaultAsync(cancellationToken);

            if (lowestBid == null)
            {
                // No valid bids, mark delivery as unassignable if past bid window
                var bidWindowThreshold = now.AddMinutes(-config.DeliveryBidWindowMinutes);
                if (delivery.CreatedAt <= bidWindowThreshold)
                {
                    delivery.Status = "UNASSIGNABLE";
                    delivery.UpdatedAt = now;
                    _logger.LogInformation("Delivery {DeliveryId} marked as UNASSIGNABLE - no valid bids received",
                        delivery.Id);
                }
                continue;
            }

            // Auto-select this bid
            await AutoSelectBidAsync(context, delivery, lowestBid, now, cancellationToken);
            _logger.LogInformation("Auto-selected bid {BidId} (Amount: {Amount}) for delivery {DeliveryId}",
                lowestBid.Id, lowestBid.BidAmount, delivery.Id);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Auto-select a bid for a delivery
    /// </summary>
    private async Task AutoSelectBidAsync(
        ApplicationDbContext context,
        Domain.Entities.Delivery delivery,
        Domain.Entities.DeliveryBid selectedBid,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Accept the selected bid
        selectedBid.Status = "ACCEPTED";
        selectedBid.RespondedAt = now;
        selectedBid.UpdatedAt = now;

        // Reject all other pending bids for this delivery
        var otherBids = await context.DeliveryBids
            .Where(b => b.DeliveryId == delivery.Id && b.Id != selectedBid.Id && b.Status == "PENDING")
            .ToListAsync(cancellationToken);

        foreach (var bid in otherBids)
        {
            bid.Status = "REJECTED";
            bid.RejectionReason = "Auto-selection: Another bid was selected (lowest bid wins)";
            bid.RespondedAt = now;
            bid.UpdatedAt = now;
        }

        // Update delivery
        delivery.AssignedDPId = selectedBid.DPId;
        delivery.AssignedAt = now;
        delivery.FinalPrice = selectedBid.BidAmount;
        delivery.Status = "ASSIGNED";
        delivery.UpdatedAt = now;

        // Create delivery event
        var deliveryEvent = new Domain.Entities.DeliveryEvent
        {
            Id = Guid.NewGuid(),
            DeliveryId = delivery.Id,
            EventType = "AUTO_ASSIGNED",
            FromStatus = "MATCHING",
            ToStatus = "ASSIGNED",
            ActorType = "SYSTEM",
            Metadata = System.Text.Json.JsonSerializer.Serialize(new {
                BidId = selectedBid.Id,
                BidAmount = selectedBid.BidAmount,
                DPId = selectedBid.DPId,
                Reason = "Auto-selected lowest bid"
            }),
            Timestamp = now
        };
        context.DeliveryEvents.Add(deliveryEvent);
    }
}
