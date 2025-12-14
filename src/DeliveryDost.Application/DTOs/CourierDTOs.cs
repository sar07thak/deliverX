namespace DeliveryDost.Application.DTOs.Courier;

// ====== RATE COMPARISON ======

public class CourierRateRequest
{
    public decimal PickupPincode { get; set; }
    public decimal DropPincode { get; set; }
    public decimal WeightKg { get; set; }
    public string? Dimensions { get; set; } // JSON: {length, width, height}
    public decimal? DeclaredValue { get; set; }
    public bool IsCOD { get; set; } = false;
    public decimal? CODAmount { get; set; }
    public string? ServiceType { get; set; } // EXPRESS, STANDARD, null for all
}

public class CourierRateResponse
{
    public List<CourierRateDto> Rates { get; set; } = new();
    public CourierRateDto? RecommendedRate { get; set; }
    public string? Message { get; set; }
}

public class CourierRateDto
{
    public Guid CourierPartnerId { get; set; }
    public string CourierName { get; set; } = string.Empty;
    public string CourierCode { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }

    public string ServiceType { get; set; } = string.Empty;
    public decimal BaseRate { get; set; }
    public decimal FuelSurcharge { get; set; }
    public decimal CODCharge { get; set; }
    public decimal TotalCourierCharge { get; set; }
    public decimal PlatformMargin { get; set; }
    public decimal FinalRate { get; set; }

    public int EstimatedDays { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }

    public bool IsRecommended { get; set; }
    public string? RecommendationReason { get; set; }
}

// ====== SHIPMENT CREATION ======

public class CreateCourierShipmentRequest
{
    public Guid DeliveryId { get; set; }
    public Guid CourierPartnerId { get; set; }
    public string ServiceType { get; set; } = "STANDARD";

    // Pickup Details
    public string PickupName { get; set; } = string.Empty;
    public string PickupPhone { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public string PickupPincode { get; set; } = string.Empty;
    public string PickupCity { get; set; } = string.Empty;
    public string PickupState { get; set; } = string.Empty;

    // Drop Details
    public string DropName { get; set; } = string.Empty;
    public string DropPhone { get; set; } = string.Empty;
    public string DropAddress { get; set; } = string.Empty;
    public string DropPincode { get; set; } = string.Empty;
    public string DropCity { get; set; } = string.Empty;
    public string DropState { get; set; } = string.Empty;

    // Package Details
    public decimal WeightKg { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? DeclaredValue { get; set; }
    public string? ProductDescription { get; set; }

    // Payment
    public bool IsCOD { get; set; } = false;
    public decimal? CODAmount { get; set; }
}

public class CourierShipmentDto
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public Guid CourierPartnerId { get; set; }
    public string CourierName { get; set; } = string.Empty;

    public string AWBNumber { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public string ServiceType { get; set; } = string.Empty;

    public decimal CourierCharge { get; set; }
    public decimal PlatformCharge { get; set; }
    public decimal TotalCharge { get; set; }

    public string Status { get; set; } = string.Empty;
    public string? CourierStatus { get; set; }

    public DateTime? PickupScheduledAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? TrackingUrl { get; set; }
}

// ====== TRACKING ======

public class CourierTrackingDto
{
    public string AWBNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? StatusDescription { get; set; }
    public string? CurrentLocation { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? ReceiverName { get; set; }

    public List<TrackingEventDto> Events { get; set; } = new();
    public string? TrackingUrl { get; set; }
}

public class TrackingEventDto
{
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
}

// ====== CANCELLATION ======

public class CancelCourierShipmentRequest
{
    public string AWBNumber { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class CancelCourierShipmentResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? RefundStatus { get; set; }
    public decimal? RefundAmount { get; set; }
}

// ====== COURIER PARTNER MANAGEMENT ======

public class CourierPartnerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }

    public bool SupportsExpress { get; set; }
    public bool SupportsStandard { get; set; }
    public bool SupportsCOD { get; set; }
    public bool SupportsReverse { get; set; }

    public decimal PlatformMarginPercent { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCourierPartnerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? ApiBaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? AccountId { get; set; }
    public decimal PlatformMarginPercent { get; set; } = 10;
    public int Priority { get; set; } = 1;
}
