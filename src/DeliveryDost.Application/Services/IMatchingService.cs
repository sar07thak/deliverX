using System;
using System.Threading;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs.Delivery;

namespace DeliveryDost.Application.Services;

public interface IMatchingService
{
    /// <summary>
    /// Find and notify eligible DPs for a delivery
    /// </summary>
    Task<MatchingResultDto> MatchDeliveryAsync(
        Guid deliveryId,
        int attempt = 1,
        CancellationToken ct = default);

    /// <summary>
    /// DP accepts a delivery
    /// </summary>
    Task<AcceptDeliveryResponse> AcceptDeliveryAsync(
        Guid deliveryId,
        Guid dpId,
        CancellationToken ct = default);

    /// <summary>
    /// DP rejects a delivery
    /// </summary>
    Task<bool> RejectDeliveryAsync(
        Guid deliveryId,
        Guid dpId,
        RejectDeliveryRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Update DP availability status
    /// </summary>
    Task<DPAvailabilityDto> UpdateDPAvailabilityAsync(
        Guid dpId,
        UpdateDPAvailabilityRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get DP availability status
    /// </summary>
    Task<DPAvailabilityDto?> GetDPAvailabilityAsync(
        Guid dpId,
        CancellationToken ct = default);
}
