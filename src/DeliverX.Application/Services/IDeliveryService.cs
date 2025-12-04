using System;
using System.Threading;
using System.Threading.Tasks;
using DeliverX.Application.DTOs.Delivery;

namespace DeliverX.Application.Services;

public interface IDeliveryService
{
    /// <summary>
    /// Create a new delivery order
    /// </summary>
    Task<CreateDeliveryResponse> CreateDeliveryAsync(
        CreateDeliveryRequest request,
        Guid requesterId,
        CancellationToken ct = default);

    /// <summary>
    /// Get delivery details by ID
    /// </summary>
    Task<DeliveryDetailsDto?> GetDeliveryAsync(
        Guid deliveryId,
        CancellationToken ct = default);

    /// <summary>
    /// Get list of deliveries with filters
    /// </summary>
    Task<DeliveryListResponse> GetDeliveriesAsync(
        Guid? requesterId,
        Guid? dpId,
        DeliveryListRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Cancel a delivery
    /// </summary>
    Task<bool> CancelDeliveryAsync(
        Guid deliveryId,
        Guid userId,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// Update delivery status
    /// </summary>
    Task<bool> UpdateDeliveryStatusAsync(
        Guid deliveryId,
        string newStatus,
        Guid actorId,
        string actorType,
        string? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get pending deliveries for a DP (notifications)
    /// </summary>
    Task<DeliveryListResponse> GetPendingDeliveriesForDPAsync(
        Guid dpId,
        CancellationToken ct = default);
}
