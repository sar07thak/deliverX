using System;
using System.Threading;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs.POD;

namespace DeliveryDost.Application.Services;

/// <summary>
/// Delivery State Machine Service
/// Manages state transitions: CREATED → MATCHING → ASSIGNED → ACCEPTED → PICKED_UP → IN_TRANSIT → DELIVERED → CLOSED
/// </summary>
public interface IDeliveryStateService
{
    /// <summary>
    /// Get current state info for a delivery
    /// </summary>
    Task<DeliveryStateInfo?> GetStateInfoAsync(
        Guid deliveryId,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a state transition is valid
    /// </summary>
    bool IsValidTransition(string fromStatus, string toStatus);

    /// <summary>
    /// Get allowed transitions from current state
    /// </summary>
    string[] GetAllowedTransitions(string currentStatus);

    /// <summary>
    /// Mark delivery as picked up by DP
    /// Transition: ASSIGNED/ACCEPTED → PICKED_UP
    /// </summary>
    Task<StateTransitionResponse> MarkAsPickedUpAsync(
        Guid deliveryId,
        Guid dpId,
        PickupRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Mark delivery as in transit
    /// Transition: PICKED_UP → IN_TRANSIT
    /// </summary>
    Task<StateTransitionResponse> MarkAsInTransitAsync(
        Guid deliveryId,
        Guid dpId,
        TransitRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Mark delivery as delivered with POD
    /// Transition: IN_TRANSIT → DELIVERED
    /// </summary>
    Task<StateTransitionResponse> MarkAsDeliveredAsync(
        Guid deliveryId,
        Guid dpId,
        DeliverRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Close a delivered order (auto or manual)
    /// Transition: DELIVERED → CLOSED
    /// </summary>
    Task<StateTransitionResponse> CloseDeliveryAsync(
        Guid deliveryId,
        Guid? actorId,
        CloseDeliveryRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Send OTP to recipient before delivery
    /// </summary>
    Task<SendDeliveryOTPResponse> SendDeliveryOTPAsync(
        Guid deliveryId,
        CancellationToken ct = default);

    /// <summary>
    /// Verify OTP entered by DP
    /// </summary>
    Task<VerifyDeliveryOTPResponse> VerifyDeliveryOTPAsync(
        Guid deliveryId,
        VerifyDeliveryOTPRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get POD details for a delivery
    /// </summary>
    Task<PODDetailsDto?> GetPODAsync(
        Guid deliveryId,
        CancellationToken ct = default);
}
