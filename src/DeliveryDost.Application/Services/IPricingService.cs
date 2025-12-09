using System;
using System.Threading;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs.Pricing;

namespace DeliveryDost.Application.Services;

public interface IPricingService
{
    /// <summary>
    /// Calculate delivery pricing for given pickup/drop locations
    /// </summary>
    Task<CalculatePricingResponse> CalculatePricingAsync(
        CalculatePricingRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get DP pricing configuration
    /// </summary>
    Task<DPPricingConfigDto?> GetDPPricingConfigAsync(
        Guid dpId,
        CancellationToken ct = default);

    /// <summary>
    /// Update DP pricing configuration
    /// </summary>
    Task<UpdateDPPricingResponse> UpdateDPPricingAsync(
        Guid dpId,
        UpdateDPPricingRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Update platform fee configuration (Admin only)
    /// </summary>
    Task<UpdatePlatformFeesResponse> UpdatePlatformFeesAsync(
        UpdatePlatformFeesRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Calculate commission breakdown for a delivery
    /// </summary>
    Task<CommissionBreakdown> CalculateCommissionAsync(
        Guid dpId,
        decimal totalAmount,
        CancellationToken ct = default);

    /// <summary>
    /// Initialize default pricing config for a new DP
    /// </summary>
    Task InitializeDefaultPricingAsync(
        Guid dpId,
        CancellationToken ct = default);
}
