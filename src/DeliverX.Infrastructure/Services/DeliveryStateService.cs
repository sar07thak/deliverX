using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliverX.Application.DTOs.POD;
using DeliverX.Application.Services;
using DeliverX.Domain.Entities;
using DeliverX.Infrastructure.Data;

namespace DeliverX.Infrastructure.Services;

public class DeliveryStateService : IDeliveryStateService
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceAreaService _serviceAreaService;
    private readonly ILogger<DeliveryStateService> _logger;

    // State Machine Definition
    // Flow: CREATED → MATCHING → ASSIGNED → ACCEPTED → PICKED_UP → IN_TRANSIT → DELIVERED → CLOSED
    private static readonly Dictionary<string, string[]> StateTransitions = new()
    {
        { "CREATED", new[] { "MATCHING", "CANCELLED" } },
        { "MATCHING", new[] { "ASSIGNED", "ACCEPTED", "UNASSIGNABLE", "CANCELLED" } },
        { "ASSIGNED", new[] { "ACCEPTED", "PICKED_UP", "CANCELLED" } }, // Allow PICKED_UP for backward compat
        { "ACCEPTED", new[] { "PICKED_UP", "CANCELLED" } },
        { "PICKED_UP", new[] { "IN_TRANSIT", "CANCELLED" } },
        { "IN_TRANSIT", new[] { "DELIVERED", "CANCELLED" } },
        { "DELIVERED", new[] { "CLOSED" } },
        { "CLOSED", Array.Empty<string>() },
        { "CANCELLED", Array.Empty<string>() },
        { "UNASSIGNABLE", new[] { "MATCHING", "CANCELLED" } }
    };

    public DeliveryStateService(
        ApplicationDbContext context,
        IServiceAreaService serviceAreaService,
        ILogger<DeliveryStateService> logger)
    {
        _context = context;
        _serviceAreaService = serviceAreaService;
        _logger = logger;
    }

    public async Task<DeliveryStateInfo?> GetStateInfoAsync(
        Guid deliveryId,
        CancellationToken ct = default)
    {
        var delivery = await _context.Deliveries.FindAsync(new object[] { deliveryId }, ct);
        if (delivery == null) return null;

        var allowedTransitions = GetAllowedTransitions(delivery.Status);

        return new DeliveryStateInfo
        {
            DeliveryId = deliveryId,
            CurrentStatus = delivery.Status,
            AllowedTransitions = allowedTransitions,
            CanMatch = delivery.Status == "CREATED",  // EC/BC can start matching from CREATED
            CanAccept = delivery.Status == "MATCHING" || delivery.Status == "ASSIGNED",  // DP can accept
            CanPickup = delivery.Status == "ASSIGNED" || delivery.Status == "ACCEPTED",
            CanTransit = delivery.Status == "PICKED_UP",
            CanDeliver = delivery.Status == "IN_TRANSIT",
            CanCancel = delivery.Status != "DELIVERED" && delivery.Status != "CLOSED" && delivery.Status != "CANCELLED",
            CanClose = delivery.Status == "DELIVERED",
            LastUpdatedAt = delivery.UpdatedAt
        };
    }

    public bool IsValidTransition(string fromStatus, string toStatus)
    {
        if (!StateTransitions.TryGetValue(fromStatus, out var allowedTransitions))
        {
            return false;
        }

        return Array.Exists(allowedTransitions, t => t == toStatus);
    }

    public string[] GetAllowedTransitions(string currentStatus)
    {
        return StateTransitions.TryGetValue(currentStatus, out var transitions)
            ? transitions
            : Array.Empty<string>();
    }

    public async Task<StateTransitionResponse> MarkAsPickedUpAsync(
        Guid deliveryId,
        Guid dpId,
        PickupRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("DP {DPId} marking delivery {DeliveryId} as PICKED_UP", dpId, deliveryId);

        var delivery = await _context.Deliveries.FindAsync(new object[] { deliveryId }, ct);
        if (delivery == null)
        {
            return CreateErrorResponse(deliveryId, "NOT_FOUND", "Delivery not found");
        }

        // Verify DP is assigned to this delivery
        if (delivery.AssignedDPId != dpId)
        {
            return CreateErrorResponse(deliveryId, "UNAUTHORIZED", "You are not assigned to this delivery");
        }

        // Validate state transition
        if (!IsValidTransition(delivery.Status, "PICKED_UP"))
        {
            return CreateErrorResponse(deliveryId, "INVALID_TRANSITION",
                $"Cannot transition from {delivery.Status} to PICKED_UP");
        }

        var previousStatus = delivery.Status;
        delivery.Status = "PICKED_UP";
        delivery.UpdatedAt = DateTime.UtcNow;

        // Get or create POD record
        var pod = await _context.ProofOfDeliveries
            .FirstOrDefaultAsync(p => p.DeliveryId == deliveryId, ct);

        if (pod == null)
        {
            pod = new ProofOfDelivery
            {
                Id = Guid.NewGuid(),
                DeliveryId = deliveryId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ProofOfDeliveries.Add(pod);
        }

        pod.PickedUpAt = DateTime.UtcNow;
        pod.PackagePhotoUrl = request.PackagePhotoUrl;
        pod.Notes = request.Notes;
        pod.UpdatedAt = DateTime.UtcNow;

        // Log event
        _context.DeliveryEvents.Add(new DeliveryEvent
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            EventType = "PICKED_UP",
            FromStatus = previousStatus,
            ToStatus = "PICKED_UP",
            ActorId = dpId,
            ActorType = "DP",
            Metadata = JsonSerializer.Serialize(new
            {
                lat = request.Lat,
                lng = request.Lng,
                packagePhotoUrl = request.PackagePhotoUrl
            }),
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);

        // Send OTP to recipient (simulated)
        await SendDeliveryOTPAsync(deliveryId, ct);

        _logger.LogInformation("Delivery {DeliveryId} marked as PICKED_UP", deliveryId);

        return new StateTransitionResponse
        {
            IsSuccess = true,
            DeliveryId = deliveryId,
            Status = "PICKED_UP",
            PreviousStatus = previousStatus,
            Timestamp = DateTime.UtcNow,
            Message = "Package picked up. Delivery OTP has been sent to recipient.",
            NextAction = "Proceed to drop location and mark as IN_TRANSIT when on the way"
        };
    }

    public async Task<StateTransitionResponse> MarkAsInTransitAsync(
        Guid deliveryId,
        Guid dpId,
        TransitRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("DP {DPId} marking delivery {DeliveryId} as IN_TRANSIT", dpId, deliveryId);

        var delivery = await _context.Deliveries.FindAsync(new object[] { deliveryId }, ct);
        if (delivery == null)
        {
            return CreateErrorResponse(deliveryId, "NOT_FOUND", "Delivery not found");
        }

        if (delivery.AssignedDPId != dpId)
        {
            return CreateErrorResponse(deliveryId, "UNAUTHORIZED", "You are not assigned to this delivery");
        }

        if (!IsValidTransition(delivery.Status, "IN_TRANSIT"))
        {
            return CreateErrorResponse(deliveryId, "INVALID_TRANSITION",
                $"Cannot transition from {delivery.Status} to IN_TRANSIT");
        }

        var previousStatus = delivery.Status;
        delivery.Status = "IN_TRANSIT";
        delivery.UpdatedAt = DateTime.UtcNow;

        // Update POD
        var pod = await _context.ProofOfDeliveries
            .FirstOrDefaultAsync(p => p.DeliveryId == deliveryId, ct);

        if (pod != null)
        {
            pod.InTransitAt = DateTime.UtcNow;
            pod.UpdatedAt = DateTime.UtcNow;
        }

        // Log event
        _context.DeliveryEvents.Add(new DeliveryEvent
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            EventType = "IN_TRANSIT",
            FromStatus = previousStatus,
            ToStatus = "IN_TRANSIT",
            ActorId = dpId,
            ActorType = "DP",
            Metadata = JsonSerializer.Serialize(new
            {
                lat = request.Lat,
                lng = request.Lng
            }),
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Delivery {DeliveryId} marked as IN_TRANSIT", deliveryId);

        return new StateTransitionResponse
        {
            IsSuccess = true,
            DeliveryId = deliveryId,
            Status = "IN_TRANSIT",
            PreviousStatus = previousStatus,
            Timestamp = DateTime.UtcNow,
            Message = "Delivery is now in transit. Customer has been notified.",
            NextAction = "Complete delivery by uploading POD at drop location"
        };
    }

    public async Task<StateTransitionResponse> MarkAsDeliveredAsync(
        Guid deliveryId,
        Guid dpId,
        DeliverRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("DP {DPId} marking delivery {DeliveryId} as DELIVERED", dpId, deliveryId);

        var delivery = await _context.Deliveries.FindAsync(new object[] { deliveryId }, ct);
        if (delivery == null)
        {
            return CreateErrorResponse(deliveryId, "NOT_FOUND", "Delivery not found");
        }

        if (delivery.AssignedDPId != dpId)
        {
            return CreateErrorResponse(deliveryId, "UNAUTHORIZED", "You are not assigned to this delivery");
        }

        if (!IsValidTransition(delivery.Status, "DELIVERED"))
        {
            return CreateErrorResponse(deliveryId, "INVALID_TRANSITION",
                $"Cannot transition from {delivery.Status} to DELIVERED");
        }

        // Get or create POD
        var pod = await _context.ProofOfDeliveries
            .FirstOrDefaultAsync(p => p.DeliveryId == deliveryId, ct);

        if (pod == null)
        {
            pod = new ProofOfDelivery
            {
                Id = Guid.NewGuid(),
                DeliveryId = deliveryId,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProofOfDeliveries.Add(pod);
        }

        // Calculate distance from original drop location
        var distanceFromDrop = _serviceAreaService.CalculateDistanceKm(
            (double)request.DeliveredLat, (double)request.DeliveredLng,
            (double)delivery.DropLat, (double)delivery.DropLng);

        // Verify OTP if provided
        bool otpVerified = false;
        if (!string.IsNullOrEmpty(request.OTP) && pod.RecipientOTP == request.OTP)
        {
            otpVerified = true;
            pod.OTPVerified = true;
            pod.OTPVerifiedAt = DateTime.UtcNow;
        }

        // Update POD
        pod.RecipientName = request.RecipientName;
        pod.RecipientRelation = request.RecipientRelation;
        pod.PODPhotoUrl = request.PODPhotoUrl;
        pod.SignatureUrl = request.SignatureUrl;
        pod.DeliveredLat = request.DeliveredLat;
        pod.DeliveredLng = request.DeliveredLng;
        pod.DistanceFromDropLocation = (decimal)(distanceFromDrop * 1000); // Convert to meters
        pod.DeliveredAt = DateTime.UtcNow;
        pod.DeliveryCondition = request.DeliveryCondition ?? "Good";
        pod.Notes = request.Notes;
        pod.UpdatedAt = DateTime.UtcNow;

        // Update delivery status
        var previousStatus = delivery.Status;
        delivery.Status = "DELIVERED";
        delivery.FinalPrice = delivery.EstimatedPrice; // TODO: Calculate final price with any adjustments
        delivery.UpdatedAt = DateTime.UtcNow;

        // Update DP availability
        var dpAvailability = await _context.DPAvailabilities
            .FirstOrDefaultAsync(a => a.DPId == dpId, ct);

        if (dpAvailability != null)
        {
            dpAvailability.Status = "AVAILABLE";
            dpAvailability.CurrentDeliveryId = null;
            dpAvailability.UpdatedAt = DateTime.UtcNow;
        }

        // Log event
        _context.DeliveryEvents.Add(new DeliveryEvent
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            EventType = "DELIVERED",
            FromStatus = previousStatus,
            ToStatus = "DELIVERED",
            ActorId = dpId,
            ActorType = "DP",
            Metadata = JsonSerializer.Serialize(new
            {
                recipientName = request.RecipientName,
                otpVerified,
                deliveredLat = request.DeliveredLat,
                deliveredLng = request.DeliveredLng,
                distanceFromDropMeters = distanceFromDrop * 1000
            }),
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Delivery {DeliveryId} marked as DELIVERED with POD", deliveryId);

        return new StateTransitionResponse
        {
            IsSuccess = true,
            DeliveryId = deliveryId,
            Status = "DELIVERED",
            PreviousStatus = previousStatus,
            Timestamp = DateTime.UtcNow,
            Message = otpVerified
                ? "Delivery completed with OTP verification. Payment will be credited to your wallet."
                : "Delivery completed. OTP was not verified. Payment will be credited after 24-hour review period.",
            NextAction = "You are now available for new deliveries"
        };
    }

    public async Task<StateTransitionResponse> CloseDeliveryAsync(
        Guid deliveryId,
        Guid? actorId,
        CloseDeliveryRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Closing delivery {DeliveryId}", deliveryId);

        var delivery = await _context.Deliveries.FindAsync(new object[] { deliveryId }, ct);
        if (delivery == null)
        {
            return CreateErrorResponse(deliveryId, "NOT_FOUND", "Delivery not found");
        }

        if (!IsValidTransition(delivery.Status, "CLOSED"))
        {
            return CreateErrorResponse(deliveryId, "INVALID_TRANSITION",
                $"Cannot transition from {delivery.Status} to CLOSED");
        }

        var previousStatus = delivery.Status;
        delivery.Status = "CLOSED";
        delivery.UpdatedAt = DateTime.UtcNow;

        // Update POD
        var pod = await _context.ProofOfDeliveries
            .FirstOrDefaultAsync(p => p.DeliveryId == deliveryId, ct);

        if (pod != null)
        {
            pod.ClosedAt = DateTime.UtcNow;
            pod.UpdatedAt = DateTime.UtcNow;

            if (actorId.HasValue)
            {
                pod.VerifiedBy = actorId;
                pod.VerifiedAt = DateTime.UtcNow;
            }
        }

        // Log event
        _context.DeliveryEvents.Add(new DeliveryEvent
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            EventType = "CLOSED",
            FromStatus = previousStatus,
            ToStatus = "CLOSED",
            ActorId = actorId,
            ActorType = actorId.HasValue ? "USER" : "SYSTEM",
            Metadata = JsonSerializer.Serialize(new { reason = request.Reason }),
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Delivery {DeliveryId} closed", deliveryId);

        return new StateTransitionResponse
        {
            IsSuccess = true,
            DeliveryId = deliveryId,
            Status = "CLOSED",
            PreviousStatus = previousStatus,
            Timestamp = DateTime.UtcNow,
            Message = "Delivery closed successfully."
        };
    }

    public async Task<SendDeliveryOTPResponse> SendDeliveryOTPAsync(
        Guid deliveryId,
        CancellationToken ct = default)
    {
        var delivery = await _context.Deliveries.FindAsync(new object[] { deliveryId }, ct);
        if (delivery == null)
        {
            return new SendDeliveryOTPResponse
            {
                IsSuccess = false,
                Message = "Delivery not found"
            };
        }

        // Generate 4-digit OTP
        var otp = new Random().Next(1000, 9999).ToString();

        // Get or create POD
        var pod = await _context.ProofOfDeliveries
            .FirstOrDefaultAsync(p => p.DeliveryId == deliveryId, ct);

        if (pod == null)
        {
            pod = new ProofOfDelivery
            {
                Id = Guid.NewGuid(),
                DeliveryId = deliveryId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ProofOfDeliveries.Add(pod);
        }

        pod.RecipientOTP = otp;
        pod.OTPSentAt = DateTime.UtcNow;
        pod.OTPVerified = false;
        pod.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        // In production, send OTP via SMS to delivery.DropContactPhone
        _logger.LogInformation("Delivery OTP for {DeliveryId}: {OTP} (would be sent to {Phone})",
            deliveryId, otp, delivery.DropContactPhone);

        return new SendDeliveryOTPResponse
        {
            IsSuccess = true,
            Message = $"OTP sent to recipient. (Dev: OTP is {otp})",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };
    }

    public async Task<VerifyDeliveryOTPResponse> VerifyDeliveryOTPAsync(
        Guid deliveryId,
        VerifyDeliveryOTPRequest request,
        CancellationToken ct = default)
    {
        var pod = await _context.ProofOfDeliveries
            .FirstOrDefaultAsync(p => p.DeliveryId == deliveryId, ct);

        if (pod == null)
        {
            return new VerifyDeliveryOTPResponse
            {
                IsSuccess = false,
                IsVerified = false,
                Message = "No POD record found for this delivery"
            };
        }

        if (string.IsNullOrEmpty(pod.RecipientOTP))
        {
            return new VerifyDeliveryOTPResponse
            {
                IsSuccess = false,
                IsVerified = false,
                Message = "No OTP has been sent for this delivery"
            };
        }

        if (pod.RecipientOTP == request.OTP)
        {
            pod.OTPVerified = true;
            pod.OTPVerifiedAt = DateTime.UtcNow;
            pod.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return new VerifyDeliveryOTPResponse
            {
                IsSuccess = true,
                IsVerified = true,
                Message = "OTP verified successfully"
            };
        }

        return new VerifyDeliveryOTPResponse
        {
            IsSuccess = true,
            IsVerified = false,
            Message = "Invalid OTP"
        };
    }

    public async Task<PODDetailsDto?> GetPODAsync(
        Guid deliveryId,
        CancellationToken ct = default)
    {
        var pod = await _context.ProofOfDeliveries
            .FirstOrDefaultAsync(p => p.DeliveryId == deliveryId, ct);

        if (pod == null) return null;

        return new PODDetailsDto
        {
            Id = pod.Id,
            DeliveryId = pod.DeliveryId,
            RecipientName = pod.RecipientName,
            RecipientRelation = pod.RecipientRelation,
            OTPVerified = pod.OTPVerified,
            OTPVerifiedAt = pod.OTPVerifiedAt,
            PODPhotoUrl = pod.PODPhotoUrl,
            PackagePhotoUrl = pod.PackagePhotoUrl,
            SignatureUrl = pod.SignatureUrl,
            DeliveredLat = pod.DeliveredLat,
            DeliveredLng = pod.DeliveredLng,
            DistanceFromDropLocation = pod.DistanceFromDropLocation,
            PickedUpAt = pod.PickedUpAt,
            InTransitAt = pod.InTransitAt,
            DeliveredAt = pod.DeliveredAt,
            ClosedAt = pod.ClosedAt,
            Notes = pod.Notes,
            DeliveryCondition = pod.DeliveryCondition,
            IsVerified = pod.VerifiedBy.HasValue,
            VerifiedBy = pod.VerifiedBy,
            VerifiedAt = pod.VerifiedAt
        };
    }

    private static StateTransitionResponse CreateErrorResponse(Guid deliveryId, string errorCode, string message)
    {
        return new StateTransitionResponse
        {
            IsSuccess = false,
            DeliveryId = deliveryId,
            ErrorCode = errorCode,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }
}
