using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.Common;
using DeliveryDost.Application.DTOs.Courier;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

/// <summary>
/// Courier service implementation for traditional courier integration
/// Currently uses mock data; ready for real API integration
/// </summary>
public class CourierService : ICourierService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CourierService> _logger;
    private readonly IDistanceCalculatorService _distanceCalculator;

    private const decimal COURIER_THRESHOLD_KM = 15; // Use courier if distance > 15km

    public CourierService(
        ApplicationDbContext context,
        ILogger<CourierService> logger,
        IDistanceCalculatorService distanceCalculator)
    {
        _context = context;
        _logger = logger;
        _distanceCalculator = distanceCalculator;
    }

    public async Task<Result<CourierRateResponse>> GetRatesAsync(CourierRateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var partners = await _context.CourierPartners
                .Where(p => p.IsActive)
                .OrderBy(p => p.Priority)
                .ToListAsync(cancellationToken);

            if (!partners.Any())
            {
                return Result<CourierRateResponse>.Success(new CourierRateResponse
                {
                    Rates = new List<CourierRateDto>(),
                    Message = "No courier partners available"
                });
            }

            var rates = new List<CourierRateDto>();

            foreach (var partner in partners)
            {
                // In real implementation, call each courier's rate API
                // For now, generate mock rates based on weight and distance
                var mockRates = GenerateMockRates(partner, request);
                rates.AddRange(mockRates);
            }

            // Sort by final rate
            rates = rates.OrderBy(r => r.FinalRate).ToList();

            // Mark recommended (cheapest with good service)
            var recommended = rates.FirstOrDefault(r => r.EstimatedDays <= 3);
            if (recommended != null)
            {
                recommended.IsRecommended = true;
                recommended.RecommendationReason = "Best value with fast delivery";
            }

            return Result<CourierRateResponse>.Success(new CourierRateResponse
            {
                Rates = rates,
                RecommendedRate = recommended
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting courier rates");
            return Result<CourierRateResponse>.Failure("Failed to get courier rates");
        }
    }

    public async Task<Result<bool>> ShouldUseCourierAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        try
        {
            var delivery = await _context.Deliveries
                .FirstOrDefaultAsync(d => d.Id == deliveryId, cancellationToken);

            if (delivery == null)
                return Result<bool>.Failure("Delivery not found");

            // Check distance
            if (delivery.DistanceKm.HasValue && delivery.DistanceKm.Value > COURIER_THRESHOLD_KM)
            {
                return Result<bool>.Success(true);
            }

            // Calculate distance if not available
            var distance = _distanceCalculator.CalculateHaversineDistance(
                delivery.PickupLat, delivery.PickupLng,
                delivery.DropLat, delivery.DropLng);

            if (distance.DistanceKm > COURIER_THRESHOLD_KM)
            {
                return Result<bool>.Success(true);
            }

            // Check if pincodes are different (cross-city delivery)
            // This would require pincode lookup in real implementation
            // For now, assume if distance > threshold, use courier

            return Result<bool>.Success(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if should use courier for delivery {DeliveryId}", deliveryId);
            return Result<bool>.Failure("Failed to check courier eligibility");
        }
    }

    public async Task<Result<bool>> CheckServiceabilityAsync(string pickupPincode, string dropPincode, CancellationToken cancellationToken)
    {
        try
        {
            // In real implementation, check with each courier's serviceability API
            // For now, assume all 6-digit pincodes are serviceable
            if (pickupPincode.Length == 6 && dropPincode.Length == 6 &&
                int.TryParse(pickupPincode, out _) && int.TryParse(dropPincode, out _))
            {
                return Result<bool>.Success(true);
            }

            return Result<bool>.Success(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking serviceability for {PickupPincode} to {DropPincode}",
                pickupPincode, dropPincode);
            return Result<bool>.Failure("Failed to check serviceability");
        }
    }

    public async Task<Result<CourierShipmentDto>> CreateShipmentAsync(CreateCourierShipmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var partner = await _context.CourierPartners
                .FirstOrDefaultAsync(p => p.Id == request.CourierPartnerId && p.IsActive, cancellationToken);

            if (partner == null)
                return Result<CourierShipmentDto>.Failure("Courier partner not found or inactive");

            var delivery = await _context.Deliveries
                .FirstOrDefaultAsync(d => d.Id == request.DeliveryId, cancellationToken);

            if (delivery == null)
                return Result<CourierShipmentDto>.Failure("Delivery not found");

            // In real implementation, call courier API to create shipment
            // For now, generate mock AWB
            var awbNumber = GenerateMockAWB(partner.Code);

            // Calculate charges
            var courierCharge = CalculateMockCourierCharge(request);
            var platformCharge = courierCharge * (partner.PlatformMarginPercent / 100);
            var totalCharge = courierCharge + platformCharge;

            var shipment = new CourierShipment
            {
                Id = Guid.NewGuid(),
                DeliveryId = request.DeliveryId,
                CourierPartnerId = request.CourierPartnerId,
                AWBNumber = awbNumber,
                OrderId = $"DD{DateTime.UtcNow:yyyyMMdd}{Guid.NewGuid().ToString()[..6].ToUpper()}",
                ServiceType = request.ServiceType,
                WeightKg = request.WeightKg,
                Dimensions = request.Length.HasValue ?
                    System.Text.Json.JsonSerializer.Serialize(new { request.Length, request.Width, request.Height }) : null,
                CourierCharge = courierCharge,
                PlatformCharge = platformCharge,
                TotalCharge = totalCharge,
                IsCOD = request.IsCOD,
                CODAmount = request.CODAmount,
                Status = "CREATED",
                PickupScheduledAt = DateTime.UtcNow.AddHours(4), // Mock: pickup scheduled for 4 hours later
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CourierShipments.Add(shipment);

            // Update delivery with courier info
            delivery.Status = "COURIER_ASSIGNED";
            delivery.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created courier shipment {AWB} for delivery {DeliveryId} with partner {Partner}",
                awbNumber, request.DeliveryId, partner.Name);

            return Result<CourierShipmentDto>.Success(MapToDto(shipment, partner.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating courier shipment for delivery {DeliveryId}", request.DeliveryId);
            return Result<CourierShipmentDto>.Failure("Failed to create courier shipment");
        }
    }

    public async Task<Result<CourierShipmentDto>> GetShipmentAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _context.CourierShipments
                .Include(s => s.CourierPartner)
                .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

            if (shipment == null)
                return Result<CourierShipmentDto>.Failure("Shipment not found");

            return Result<CourierShipmentDto>.Success(MapToDto(shipment, shipment.CourierPartner.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shipment {ShipmentId}", shipmentId);
            return Result<CourierShipmentDto>.Failure("Failed to get shipment");
        }
    }

    public async Task<Result<CourierShipmentDto>> GetShipmentByAWBAsync(string awbNumber, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _context.CourierShipments
                .Include(s => s.CourierPartner)
                .FirstOrDefaultAsync(s => s.AWBNumber == awbNumber, cancellationToken);

            if (shipment == null)
                return Result<CourierShipmentDto>.Failure("Shipment not found");

            return Result<CourierShipmentDto>.Success(MapToDto(shipment, shipment.CourierPartner.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shipment by AWB {AWB}", awbNumber);
            return Result<CourierShipmentDto>.Failure("Failed to get shipment");
        }
    }

    public async Task<Result<List<CourierShipmentDto>>> GetShipmentsByDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        try
        {
            var shipments = await _context.CourierShipments
                .Include(s => s.CourierPartner)
                .Where(s => s.DeliveryId == deliveryId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            var dtos = shipments.Select(s => MapToDto(s, s.CourierPartner.Name)).ToList();

            return Result<List<CourierShipmentDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shipments for delivery {DeliveryId}", deliveryId);
            return Result<List<CourierShipmentDto>>.Failure("Failed to get shipments");
        }
    }

    public async Task<Result<CourierTrackingDto>> TrackShipmentAsync(string awbNumber, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _context.CourierShipments
                .Include(s => s.CourierPartner)
                .FirstOrDefaultAsync(s => s.AWBNumber == awbNumber, cancellationToken);

            if (shipment == null)
                return Result<CourierTrackingDto>.Failure("Shipment not found");

            // In real implementation, call courier's tracking API
            // For now, generate mock tracking events
            var tracking = GenerateMockTracking(shipment);

            return Result<CourierTrackingDto>.Success(tracking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking shipment {AWB}", awbNumber);
            return Result<CourierTrackingDto>.Failure("Failed to track shipment");
        }
    }

    public async Task<Result<bool>> SyncTrackingAsync(string awbNumber, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _context.CourierShipments
                .Include(s => s.CourierPartner)
                .FirstOrDefaultAsync(s => s.AWBNumber == awbNumber, cancellationToken);

            if (shipment == null)
                return Result<bool>.Failure("Shipment not found");

            // In real implementation, call courier API and update status
            // For now, just update last tracked timestamp
            shipment.LastTrackedAt = DateTime.UtcNow;
            shipment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing tracking for {AWB}", awbNumber);
            return Result<bool>.Failure("Failed to sync tracking");
        }
    }

    public async Task<Result<CancelCourierShipmentResponse>> CancelShipmentAsync(CancelCourierShipmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _context.CourierShipments
                .Include(s => s.CourierPartner)
                .FirstOrDefaultAsync(s => s.AWBNumber == request.AWBNumber, cancellationToken);

            if (shipment == null)
                return Result<CancelCourierShipmentResponse>.Failure("Shipment not found");

            if (shipment.Status == "DELIVERED" || shipment.Status == "CANCELLED")
                return Result<CancelCourierShipmentResponse>.Failure($"Cannot cancel shipment in {shipment.Status} status");

            // In real implementation, call courier API to cancel
            shipment.Status = "CANCELLED";
            shipment.CancelledAt = DateTime.UtcNow;
            shipment.CancellationReason = request.Reason;
            shipment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cancelled courier shipment {AWB}", request.AWBNumber);

            return Result<CancelCourierShipmentResponse>.Success(new CancelCourierShipmentResponse
            {
                Success = true,
                Message = "Shipment cancelled successfully",
                RefundStatus = "PROCESSING",
                RefundAmount = shipment.TotalCharge
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling shipment {AWB}", request.AWBNumber);
            return Result<CancelCourierShipmentResponse>.Failure("Failed to cancel shipment");
        }
    }

    public async Task<Result<List<CourierPartnerDto>>> GetCourierPartnersAsync(CancellationToken cancellationToken)
    {
        try
        {
            var partners = await _context.CourierPartners
                .OrderBy(p => p.Priority)
                .Select(p => new CourierPartnerDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    LogoUrl = p.LogoUrl,
                    SupportsExpress = p.SupportsExpress,
                    SupportsStandard = p.SupportsStandard,
                    SupportsCOD = p.SupportsCOD,
                    SupportsReverse = p.SupportsReverse,
                    PlatformMarginPercent = p.PlatformMarginPercent,
                    Priority = p.Priority,
                    IsActive = p.IsActive
                })
                .ToListAsync(cancellationToken);

            return Result<List<CourierPartnerDto>>.Success(partners);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting courier partners");
            return Result<List<CourierPartnerDto>>.Failure("Failed to get courier partners");
        }
    }

    public async Task<Result<CourierPartnerDto>> CreateCourierPartnerAsync(CreateCourierPartnerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _context.CourierPartners
                .AnyAsync(p => p.Code == request.Code, cancellationToken);

            if (existing)
                return Result<CourierPartnerDto>.Failure("Courier partner with this code already exists");

            var partner = new CourierPartner
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Code = request.Code.ToUpper(),
                LogoUrl = request.LogoUrl,
                ApiBaseUrl = request.ApiBaseUrl,
                ApiKey = request.ApiKey,
                ApiSecret = request.ApiSecret,
                AccountId = request.AccountId,
                PlatformMarginPercent = request.PlatformMarginPercent,
                Priority = request.Priority,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CourierPartners.Add(partner);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created courier partner {Name} ({Code})", request.Name, request.Code);

            return Result<CourierPartnerDto>.Success(new CourierPartnerDto
            {
                Id = partner.Id,
                Name = partner.Name,
                Code = partner.Code,
                LogoUrl = partner.LogoUrl,
                PlatformMarginPercent = partner.PlatformMarginPercent,
                Priority = partner.Priority,
                IsActive = partner.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating courier partner {Name}", request.Name);
            return Result<CourierPartnerDto>.Failure("Failed to create courier partner");
        }
    }

    public async Task<Result<bool>> UpdateCourierPartnerStatusAsync(Guid partnerId, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            var partner = await _context.CourierPartners
                .FirstOrDefaultAsync(p => p.Id == partnerId, cancellationToken);

            if (partner == null)
                return Result<bool>.Failure("Courier partner not found");

            partner.IsActive = isActive;
            partner.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated courier partner {Name} status to {Status}", partner.Name, isActive);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating courier partner {PartnerId} status", partnerId);
            return Result<bool>.Failure("Failed to update courier partner status");
        }
    }

    public async Task<Result<CourierRateDto>> AutoSelectCourierAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        try
        {
            var delivery = await _context.Deliveries
                .FirstOrDefaultAsync(d => d.Id == deliveryId, cancellationToken);

            if (delivery == null)
                return Result<CourierRateDto>.Failure("Delivery not found");

            // Get rates
            var rateRequest = new CourierRateRequest
            {
                PickupPincode = decimal.Parse(delivery.PickupAddress.Split(',').LastOrDefault()?.Trim() ?? "110001"),
                DropPincode = decimal.Parse(delivery.DropAddress.Split(',').LastOrDefault()?.Trim() ?? "110002"),
                WeightKg = delivery.WeightKg,
                DeclaredValue = delivery.PackageValue
            };

            var ratesResult = await GetRatesAsync(rateRequest, cancellationToken);
            if (!ratesResult.IsSuccess || ratesResult.Data?.Rates == null || !ratesResult.Data.Rates.Any())
                return Result<CourierRateDto>.Failure("No courier rates available");

            // Select best rate (cheapest with delivery within 3 days)
            var bestRate = ratesResult.Data.Rates
                .Where(r => r.EstimatedDays <= 3)
                .OrderBy(r => r.FinalRate)
                .FirstOrDefault() ?? ratesResult.Data.Rates.First();

            return Result<CourierRateDto>.Success(bestRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-selecting courier for delivery {DeliveryId}", deliveryId);
            return Result<CourierRateDto>.Failure("Failed to auto-select courier");
        }
    }

    #region Private Helpers

    private List<CourierRateDto> GenerateMockRates(CourierPartner partner, CourierRateRequest request)
    {
        var rates = new List<CourierRateDto>();

        // Base rate calculation (mock)
        var baseRate = 40 + (request.WeightKg * 15); // ₹40 base + ₹15/kg
        var fuelSurcharge = baseRate * 0.15m; // 15% fuel surcharge
        var codCharge = request.IsCOD ? 25 : 0; // ₹25 COD fee

        if (partner.SupportsStandard)
        {
            var standardTotal = baseRate + fuelSurcharge + codCharge;
            var platformMargin = standardTotal * (partner.PlatformMarginPercent / 100);

            rates.Add(new CourierRateDto
            {
                CourierPartnerId = partner.Id,
                CourierName = partner.Name,
                CourierCode = partner.Code,
                LogoUrl = partner.LogoUrl,
                ServiceType = "STANDARD",
                BaseRate = baseRate,
                FuelSurcharge = fuelSurcharge,
                CODCharge = codCharge,
                TotalCourierCharge = standardTotal,
                PlatformMargin = platformMargin,
                FinalRate = standardTotal + platformMargin,
                EstimatedDays = 3,
                ExpectedDeliveryDate = DateTime.UtcNow.AddDays(3)
            });
        }

        if (partner.SupportsExpress)
        {
            var expressMultiplier = 1.5m;
            var expressTotal = (baseRate + fuelSurcharge) * expressMultiplier + codCharge;
            var platformMargin = expressTotal * (partner.PlatformMarginPercent / 100);

            rates.Add(new CourierRateDto
            {
                CourierPartnerId = partner.Id,
                CourierName = partner.Name,
                CourierCode = partner.Code,
                LogoUrl = partner.LogoUrl,
                ServiceType = "EXPRESS",
                BaseRate = baseRate * expressMultiplier,
                FuelSurcharge = fuelSurcharge * expressMultiplier,
                CODCharge = codCharge,
                TotalCourierCharge = expressTotal,
                PlatformMargin = platformMargin,
                FinalRate = expressTotal + platformMargin,
                EstimatedDays = 1,
                ExpectedDeliveryDate = DateTime.UtcNow.AddDays(1)
            });
        }

        return rates;
    }

    private string GenerateMockAWB(string courierCode)
    {
        var random = new Random();
        return $"{courierCode}{DateTime.UtcNow:yyyyMMdd}{random.Next(100000, 999999)}";
    }

    private decimal CalculateMockCourierCharge(CreateCourierShipmentRequest request)
    {
        var baseRate = 40m;
        var weightCharge = request.WeightKg * 15;
        var serviceMultiplier = request.ServiceType == "EXPRESS" ? 1.5m : 1.0m;
        var codCharge = request.IsCOD ? 25m : 0m;

        return (baseRate + weightCharge) * serviceMultiplier + codCharge;
    }

    private CourierTrackingDto GenerateMockTracking(CourierShipment shipment)
    {
        var events = new List<TrackingEventDto>
        {
            new() { Timestamp = shipment.CreatedAt, Status = "CREATED", Description = "Shipment created", Location = "Origin Hub" }
        };

        if (shipment.PickupScheduledAt.HasValue)
        {
            events.Add(new TrackingEventDto
            {
                Timestamp = shipment.PickupScheduledAt.Value,
                Status = "PICKUP_SCHEDULED",
                Description = "Pickup scheduled",
                Location = "Origin Hub"
            });
        }

        if (shipment.PickedUpAt.HasValue)
        {
            events.Add(new TrackingEventDto
            {
                Timestamp = shipment.PickedUpAt.Value,
                Status = "PICKED_UP",
                Description = "Package picked up",
                Location = "Pickup Location"
            });
        }

        if (shipment.Status == "IN_TRANSIT")
        {
            events.Add(new TrackingEventDto
            {
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Status = "IN_TRANSIT",
                Description = "In transit to destination",
                Location = "Transit Hub"
            });
        }

        if (shipment.DeliveredAt.HasValue)
        {
            events.Add(new TrackingEventDto
            {
                Timestamp = shipment.DeliveredAt.Value,
                Status = "DELIVERED",
                Description = $"Delivered to {shipment.ReceiverName ?? "recipient"}",
                Location = "Delivery Location"
            });
        }

        return new CourierTrackingDto
        {
            AWBNumber = shipment.AWBNumber,
            Status = shipment.Status,
            StatusDescription = GetStatusDescription(shipment.Status),
            CurrentLocation = events.LastOrDefault()?.Location,
            ExpectedDeliveryDate = shipment.PickupScheduledAt?.AddDays(3),
            DeliveredAt = shipment.DeliveredAt,
            ReceiverName = shipment.ReceiverName,
            Events = events.OrderByDescending(e => e.Timestamp).ToList(),
            TrackingUrl = $"https://track.{shipment.CourierPartner?.Code?.ToLower() ?? "courier"}.com/{shipment.AWBNumber}"
        };
    }

    private string GetStatusDescription(string status)
    {
        return status switch
        {
            "CREATED" => "Shipment booked, awaiting pickup",
            "PICKUP_SCHEDULED" => "Pickup scheduled",
            "PICKED_UP" => "Package picked up from sender",
            "IN_TRANSIT" => "Package in transit",
            "OUT_FOR_DELIVERY" => "Out for delivery",
            "DELIVERED" => "Package delivered",
            "RTO" => "Return to origin",
            "CANCELLED" => "Shipment cancelled",
            _ => status
        };
    }

    private CourierShipmentDto MapToDto(CourierShipment shipment, string courierName)
    {
        return new CourierShipmentDto
        {
            Id = shipment.Id,
            DeliveryId = shipment.DeliveryId,
            CourierPartnerId = shipment.CourierPartnerId,
            CourierName = courierName,
            AWBNumber = shipment.AWBNumber,
            OrderId = shipment.OrderId,
            ServiceType = shipment.ServiceType,
            CourierCharge = shipment.CourierCharge,
            PlatformCharge = shipment.PlatformCharge,
            TotalCharge = shipment.TotalCharge,
            Status = shipment.Status,
            CourierStatus = shipment.CourierStatus,
            PickupScheduledAt = shipment.PickupScheduledAt,
            PickedUpAt = shipment.PickedUpAt,
            DeliveredAt = shipment.DeliveredAt,
            CreatedAt = shipment.CreatedAt,
            TrackingUrl = $"https://track.courier.com/{shipment.AWBNumber}"
        };
    }

    #endregion
}
