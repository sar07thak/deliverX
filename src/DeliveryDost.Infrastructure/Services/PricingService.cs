using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.DTOs.Pricing;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class PricingService : IPricingService
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceAreaService _serviceAreaService;
    private readonly ILogger<PricingService> _logger;

    // Default platform configuration
    private const decimal DefaultPlatformFeePercentage = 15.0m;
    private const decimal DefaultGSTPercentage = 18.0m;
    private const decimal DefaultDPCMCommissionPercentage = 10.0m;

    public PricingService(
        ApplicationDbContext context,
        IServiceAreaService serviceAreaService,
        ILogger<PricingService> logger)
    {
        _context = context;
        _serviceAreaService = serviceAreaService;
        _logger = logger;
    }

    public async Task<CalculatePricingResponse> CalculatePricingAsync(
        CalculatePricingRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Calculating pricing for route ({PickupLat},{PickupLng}) -> ({DropLat},{DropLng})",
            request.PickupLat, request.PickupLng, request.DropLat, request.DropLng);

        // 1. Calculate distance using Haversine formula
        var distanceKm = _serviceAreaService.CalculateDistanceKm(
            (double)request.PickupLat, (double)request.PickupLng,
            (double)request.DropLat, (double)request.DropLng);

        // 2. Find eligible DPs using service area matching
        var eligibleDPsResult = await _serviceAreaService.FindEligibleDPsAsync(
            new Application.DTOs.ServiceArea.FindEligibleDPsRequest
            {
                PickupLat = request.PickupLat,
                PickupLng = request.PickupLng,
                DropLat = request.DropLat,
                DropLng = request.DropLng,
                MaxResults = 10
            }, ct);

        if (eligibleDPsResult.TotalMatches == 0)
        {
            return new CalculatePricingResponse
            {
                IsSuccess = false,
                ErrorMessage = "No delivery partners available for this route"
            };
        }

        // 3. Get pricing for each eligible DP
        var dpPricingEstimates = new List<DPPricingEstimate>();

        foreach (var matchedDP in eligibleDPsResult.MatchedDPs)
        {
            var pricingConfig = await GetOrCreatePricingConfigAsync(matchedDP.DPId, ct);

            var breakdown = CalculatePriceBreakdown(
                pricingConfig,
                (decimal)distanceKm,
                request.WeightKg,
                request.Priority);

            dpPricingEstimates.Add(new DPPricingEstimate
            {
                DPId = matchedDP.DPId,
                DPName = matchedDP.DPName,
                Phone = matchedDP.Phone,
                EstimatedPrice = breakdown.Total,
                Breakdown = breakdown,
                ETA = EstimateDeliveryTime(distanceKm)
            });
        }

        // 4. Sort by price (cheapest first)
        dpPricingEstimates = dpPricingEstimates
            .OrderBy(p => p.EstimatedPrice)
            .ToList();

        var cheapest = dpPricingEstimates.First();

        return new CalculatePricingResponse
        {
            IsSuccess = true,
            PricingBreakdown = cheapest.Breakdown,
            AvailableDPs = dpPricingEstimates,
            CheapestPrice = cheapest.EstimatedPrice,
            EstimatedDeliveryTime = cheapest.ETA
        };
    }

    public async Task<DPPricingConfigDto?> GetDPPricingConfigAsync(
        Guid dpId,
        CancellationToken ct = default)
    {
        var config = await _context.DPPricingConfigs
            .FirstOrDefaultAsync(c => c.DPId == dpId &&
                (c.EffectiveTo == null || c.EffectiveTo > DateTime.UtcNow), ct);

        if (config == null)
        {
            return null;
        }

        // Get DPCM commission if DP has a DPCM
        var dpProfile = await _context.DeliveryPartnerProfiles
            .FirstOrDefaultAsync(p => p.UserId == dpId, ct);

        CommissionInfo? dpcmCommission = null;
        if (dpProfile?.DPCMId != null)
        {
            var dpcmConfig = await _context.DPCMCommissionConfigs
                .FirstOrDefaultAsync(c => c.DPCMId == dpProfile.DPCMId, ct);

            if (dpcmConfig != null)
            {
                dpcmCommission = new CommissionInfo
                {
                    Type = dpcmConfig.CommissionType,
                    Value = dpcmConfig.CommissionValue
                };
            }
        }

        // Get platform fee
        var platformFee = await GetPlatformFeeAsync("PLATFORM_COMMISSION", ct);

        return new DPPricingConfigDto
        {
            DPId = dpId,
            PricingConfig = new PricingConfigDetail
            {
                PerKmRate = config.PerKmRate,
                PerKgRate = config.PerKgRate,
                MinCharge = config.MinCharge,
                MaxDistanceKm = config.MaxDistanceKm,
                AcceptsPriorityDelivery = config.AcceptsPriorityDelivery,
                PrioritySurcharge = config.PrioritySurcharge,
                PeakHourSurcharge = config.PeakHourSurcharge,
                Currency = config.Currency
            },
            DPCMCommission = dpcmCommission,
            PlatformFee = new CommissionInfo
            {
                Type = "PERCENTAGE",
                Value = platformFee
            }
        };
    }

    public async Task<UpdateDPPricingResponse> UpdateDPPricingAsync(
        Guid dpId,
        UpdateDPPricingRequest request,
        CancellationToken ct = default)
    {
        var config = await _context.DPPricingConfigs
            .FirstOrDefaultAsync(c => c.DPId == dpId &&
                (c.EffectiveTo == null || c.EffectiveTo > DateTime.UtcNow), ct);

        if (config == null)
        {
            // Create new config
            config = new DPPricingConfig
            {
                Id = Guid.NewGuid(),
                DPId = dpId,
                PerKmRate = request.PerKmRate ?? 10.0m,
                PerKgRate = request.PerKgRate ?? 5.0m,
                MinCharge = request.MinCharge ?? 30.0m,
                MaxDistanceKm = request.MaxDistanceKm ?? 20.0m,
                AcceptsPriorityDelivery = request.AcceptsPriorityDelivery ?? true,
                PrioritySurcharge = request.PrioritySurcharge ?? 10.0m,
                PeakHourSurcharge = request.PeakHourSurcharge ?? 5.0m,
                Currency = "INR",
                EffectiveFrom = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.DPPricingConfigs.Add(config);
        }
        else
        {
            // Update existing
            if (request.PerKmRate.HasValue) config.PerKmRate = request.PerKmRate.Value;
            if (request.PerKgRate.HasValue) config.PerKgRate = request.PerKgRate.Value;
            if (request.MinCharge.HasValue) config.MinCharge = request.MinCharge.Value;
            if (request.MaxDistanceKm.HasValue) config.MaxDistanceKm = request.MaxDistanceKm.Value;
            if (request.AcceptsPriorityDelivery.HasValue) config.AcceptsPriorityDelivery = request.AcceptsPriorityDelivery.Value;
            if (request.PrioritySurcharge.HasValue) config.PrioritySurcharge = request.PrioritySurcharge.Value;
            if (request.PeakHourSurcharge.HasValue) config.PeakHourSurcharge = request.PeakHourSurcharge.Value;
            config.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Updated pricing config for DP {DPId}", dpId);

        return new UpdateDPPricingResponse
        {
            Message = "Pricing updated successfully",
            EffectiveFrom = config.EffectiveFrom
        };
    }

    public async Task<UpdatePlatformFeesResponse> UpdatePlatformFeesAsync(
        UpdatePlatformFeesRequest request,
        CancellationToken ct = default)
    {
        var effectiveFrom = DateTime.UtcNow;

        if (request.PlatformFeePercentage.HasValue)
        {
            await UpsertPlatformFeeAsync("PLATFORM_COMMISSION", "PERCENTAGE",
                request.PlatformFeePercentage.Value, effectiveFrom, ct);
        }

        if (request.GSTPercentage.HasValue)
        {
            await UpsertPlatformFeeAsync("GST", "PERCENTAGE",
                request.GSTPercentage.Value, effectiveFrom, ct);
        }

        if (request.PeakHourSurcharge.HasValue)
        {
            var conditions = request.PeakHours != null
                ? JsonSerializer.Serialize(new { peakHours = request.PeakHours })
                : null;

            await UpsertPlatformFeeAsync("PEAK_SURCHARGE", "FLAT",
                request.PeakHourSurcharge.Value, effectiveFrom, ct, conditions);
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Updated platform fees");

        return new UpdatePlatformFeesResponse
        {
            Message = "Platform fees updated",
            EffectiveFrom = effectiveFrom
        };
    }

    public async Task<CommissionBreakdown> CalculateCommissionAsync(
        Guid dpId,
        decimal totalAmount,
        CancellationToken ct = default)
    {
        // Get platform fee percentage
        var platformFeePercentage = await GetPlatformFeeAsync("PLATFORM_COMMISSION", ct);

        // Get GST percentage
        var gstPercentage = await GetPlatformFeeAsync("GST", ct);

        // Get DPCM commission if applicable
        var dpProfile = await _context.DeliveryPartnerProfiles
            .FirstOrDefaultAsync(p => p.UserId == dpId, ct);

        decimal dpcmCommissionPercentage = 0;
        decimal dpcmCommission = 0;

        if (dpProfile?.DPCMId != null)
        {
            var dpcmConfig = await _context.DPCMCommissionConfigs
                .FirstOrDefaultAsync(c => c.DPCMId == dpProfile.DPCMId, ct);

            if (dpcmConfig != null)
            {
                dpcmCommissionPercentage = dpcmConfig.CommissionValue;
                dpcmCommission = CalculateDPCMCommission(totalAmount, dpcmConfig);
            }
        }

        // Calculate amounts
        var platformFee = totalAmount * platformFeePercentage / 100;
        var gstAmount = totalAmount * gstPercentage / 100;

        // DP earning = total - platform fee - DPCM commission - GST
        var dpEarning = totalAmount - platformFee - dpcmCommission - gstAmount;

        return new CommissionBreakdown
        {
            TotalAmount = totalAmount,
            DPEarning = dpEarning,
            DPCMCommission = dpcmCommission,
            PlatformFee = platformFee,
            GSTAmount = gstAmount,
            DPEarningPercentage = (dpEarning / totalAmount) * 100,
            DPCMCommissionPercentage = dpcmCommissionPercentage,
            PlatformFeePercentage = platformFeePercentage,
            GSTPercentage = gstPercentage
        };
    }

    public async Task InitializeDefaultPricingAsync(Guid dpId, CancellationToken ct = default)
    {
        var existingConfig = await _context.DPPricingConfigs
            .FirstOrDefaultAsync(c => c.DPId == dpId, ct);

        if (existingConfig == null)
        {
            var config = new DPPricingConfig
            {
                Id = Guid.NewGuid(),
                DPId = dpId,
                PerKmRate = 10.0m,
                PerKgRate = 5.0m,
                MinCharge = 30.0m,
                MaxDistanceKm = 20.0m,
                AcceptsPriorityDelivery = true,
                PrioritySurcharge = 10.0m,
                PeakHourSurcharge = 5.0m,
                Currency = "INR",
                EffectiveFrom = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DPPricingConfigs.Add(config);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Initialized default pricing for DP {DPId}", dpId);
        }
    }

    private PricingBreakdown CalculatePriceBreakdown(
        DPPricingConfig config,
        decimal distanceKm,
        decimal weightKg,
        string priority)
    {
        // Distance cost
        var distanceCost = config.PerKmRate * distanceKm;

        // Weight cost
        var weightCost = config.PerKgRate * weightKg;

        // Base cost (max of calculated or min charge)
        var baseCost = Math.Max(distanceCost + weightCost, config.MinCharge);

        // Surcharges
        var surcharges = new List<SurchargeItem>();

        // Priority surcharge
        if (priority == "ASAP" && config.AcceptsPriorityDelivery)
        {
            surcharges.Add(new SurchargeItem
            {
                Type = "PRIORITY",
                Amount = config.PrioritySurcharge,
                Reason = "ASAP delivery requested"
            });
        }

        // Peak hour surcharge
        if (IsPeakHour(DateTime.UtcNow))
        {
            surcharges.Add(new SurchargeItem
            {
                Type = "PEAK_HOUR",
                Amount = config.PeakHourSurcharge,
                Reason = $"Peak hour ({DateTime.UtcNow:HH:mm})"
            });
        }

        var totalSurcharges = surcharges.Sum(s => s.Amount);
        var subtotal = baseCost + totalSurcharges;

        // GST (default 18%)
        var gstPercentage = DefaultGSTPercentage;
        var gstAmount = subtotal * gstPercentage / 100;

        var total = Math.Round(subtotal + gstAmount, 2);

        return new PricingBreakdown
        {
            DistanceKm = distanceKm,
            DistanceCost = Math.Round(distanceCost, 2),
            WeightCost = Math.Round(weightCost, 2),
            MinCharge = config.MinCharge,
            Surcharges = surcharges,
            Subtotal = Math.Round(subtotal, 2),
            GSTPercentage = gstPercentage,
            GST = Math.Round(gstAmount, 2),
            Total = total
        };
    }

    private async Task<DPPricingConfig> GetOrCreatePricingConfigAsync(Guid dpId, CancellationToken ct)
    {
        var config = await _context.DPPricingConfigs
            .FirstOrDefaultAsync(c => c.DPId == dpId &&
                (c.EffectiveTo == null || c.EffectiveTo > DateTime.UtcNow), ct);

        if (config == null)
        {
            // Check if DP has custom pricing in profile
            var dpProfile = await _context.DeliveryPartnerProfiles
                .FirstOrDefaultAsync(p => p.UserId == dpId, ct);

            config = new DPPricingConfig
            {
                Id = Guid.NewGuid(),
                DPId = dpId,
                PerKmRate = dpProfile?.PerKmRate ?? 10.0m,
                PerKgRate = dpProfile?.PerKgRate ?? 5.0m,
                MinCharge = dpProfile?.MinCharge ?? 30.0m,
                MaxDistanceKm = 20.0m,
                AcceptsPriorityDelivery = true,
                PrioritySurcharge = 10.0m,
                PeakHourSurcharge = 5.0m,
                Currency = "INR",
                EffectiveFrom = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DPPricingConfigs.Add(config);
            await _context.SaveChangesAsync(ct);
        }

        return config;
    }

    private decimal CalculateDPCMCommission(decimal totalAmount, DPCMCommissionConfig config)
    {
        decimal commission;

        if (config.CommissionType == "PERCENTAGE")
        {
            commission = totalAmount * config.CommissionValue / 100;
        }
        else // FLAT_PER_DELIVERY
        {
            commission = config.CommissionValue;
        }

        // Apply min/max limits
        if (config.MinCommissionAmount > 0)
            commission = Math.Max(commission, config.MinCommissionAmount);

        if (config.MaxCommissionAmount.HasValue)
            commission = Math.Min(commission, config.MaxCommissionAmount.Value);

        return commission;
    }

    private async Task<decimal> GetPlatformFeeAsync(string feeType, CancellationToken ct)
    {
        var config = await _context.PlatformFeeConfigs
            .Where(c => c.FeeType == feeType &&
                (c.EffectiveTo == null || c.EffectiveTo > DateTime.UtcNow))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (config != null)
        {
            return config.FeeValue;
        }

        // Return defaults
        return feeType switch
        {
            "PLATFORM_COMMISSION" => DefaultPlatformFeePercentage,
            "GST" => DefaultGSTPercentage,
            "PEAK_SURCHARGE" => 5.0m,
            _ => 0
        };
    }

    private async Task UpsertPlatformFeeAsync(
        string feeType,
        string calculationType,
        decimal value,
        DateTime effectiveFrom,
        CancellationToken ct,
        string? conditions = null)
    {
        var existing = await _context.PlatformFeeConfigs
            .FirstOrDefaultAsync(c => c.FeeType == feeType &&
                (c.EffectiveTo == null || c.EffectiveTo > DateTime.UtcNow), ct);

        if (existing != null)
        {
            existing.EffectiveTo = effectiveFrom;
        }

        var newConfig = new PlatformFeeConfig
        {
            Id = Guid.NewGuid(),
            FeeType = feeType,
            FeeCalculationType = calculationType,
            FeeValue = value,
            Conditions = conditions,
            EffectiveFrom = effectiveFrom,
            CreatedAt = DateTime.UtcNow
        };

        _context.PlatformFeeConfigs.Add(newConfig);
    }

    private bool IsPeakHour(DateTime time)
    {
        var hour = time.Hour;
        // Peak hours: 8-10 AM and 6-9 PM
        return (hour >= 8 && hour < 10) || (hour >= 18 && hour < 21);
    }

    private string EstimateDeliveryTime(double distanceKm)
    {
        // Rough estimate: 20km/h average speed in city traffic
        var minutes = (int)(distanceKm / 20 * 60);

        if (minutes < 15) return "10-15 mins";
        if (minutes < 25) return "15-25 mins";
        if (minutes < 40) return "25-40 mins";
        if (minutes < 60) return "40-60 mins";
        return $"{minutes / 60}+ hours";
    }
}
