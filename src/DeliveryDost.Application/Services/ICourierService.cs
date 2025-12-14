using DeliveryDost.Application.Common;
using DeliveryDost.Application.DTOs.Courier;

namespace DeliveryDost.Application.Services;

/// <summary>
/// Service for traditional courier integration (>15km or cross-pincode deliveries)
/// Supports Delhivery, XpressBees, BlueDart, DTDC, etc.
/// </summary>
public interface ICourierService
{
    // ====== RATE COMPARISON ======

    /// <summary>
    /// Get rates from all active courier partners for comparison
    /// </summary>
    Task<Result<CourierRateResponse>> GetRatesAsync(CourierRateRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Check if a delivery should use courier (based on distance/pincode)
    /// </summary>
    Task<Result<bool>> ShouldUseCourierAsync(Guid deliveryId, CancellationToken cancellationToken);

    /// <summary>
    /// Check serviceability for a pincode pair
    /// </summary>
    Task<Result<bool>> CheckServiceabilityAsync(string pickupPincode, string dropPincode, CancellationToken cancellationToken);

    // ====== SHIPMENT MANAGEMENT ======

    /// <summary>
    /// Create a shipment with selected courier partner
    /// </summary>
    Task<Result<CourierShipmentDto>> CreateShipmentAsync(CreateCourierShipmentRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Get shipment details
    /// </summary>
    Task<Result<CourierShipmentDto>> GetShipmentAsync(Guid shipmentId, CancellationToken cancellationToken);

    /// <summary>
    /// Get shipment by AWB number
    /// </summary>
    Task<Result<CourierShipmentDto>> GetShipmentByAWBAsync(string awbNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Get all shipments for a delivery
    /// </summary>
    Task<Result<List<CourierShipmentDto>>> GetShipmentsByDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken);

    // ====== TRACKING ======

    /// <summary>
    /// Track shipment by AWB number
    /// </summary>
    Task<Result<CourierTrackingDto>> TrackShipmentAsync(string awbNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Sync tracking status from courier API
    /// </summary>
    Task<Result<bool>> SyncTrackingAsync(string awbNumber, CancellationToken cancellationToken);

    // ====== CANCELLATION ======

    /// <summary>
    /// Cancel a shipment
    /// </summary>
    Task<Result<CancelCourierShipmentResponse>> CancelShipmentAsync(CancelCourierShipmentRequest request, CancellationToken cancellationToken);

    // ====== COURIER PARTNER MANAGEMENT (Admin) ======

    /// <summary>
    /// Get all courier partners
    /// </summary>
    Task<Result<List<CourierPartnerDto>>> GetCourierPartnersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Create a new courier partner
    /// </summary>
    Task<Result<CourierPartnerDto>> CreateCourierPartnerAsync(CreateCourierPartnerRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Update courier partner status
    /// </summary>
    Task<Result<bool>> UpdateCourierPartnerStatusAsync(Guid partnerId, bool isActive, CancellationToken cancellationToken);

    // ====== AUTO-SELECTION ======

    /// <summary>
    /// Auto-select best courier for a delivery based on price, speed, and reliability
    /// </summary>
    Task<Result<CourierRateDto>> AutoSelectCourierAsync(Guid deliveryId, CancellationToken cancellationToken);
}
