namespace DeliveryDost.Domain.Entities;

/// <summary>
/// External courier partner configuration (Delhivery, XpressBees, BlueDart, etc.)
/// Used for deliveries >15km or cross-pincode
/// </summary>
public class CourierPartner
{
    public Guid Id { get; set; }

    // Partner Details
    public string Name { get; set; } = string.Empty; // Delhivery, XpressBees, BlueDart, DTDC
    public string Code { get; set; } = string.Empty; // DELHIVERY, XPRESSBEES, BLUEDART
    public string? LogoUrl { get; set; }

    // API Configuration
    public string? ApiBaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? AccountId { get; set; }

    // Service Coverage
    public bool SupportsExpress { get; set; } = true;
    public bool SupportsStandard { get; set; } = true;
    public bool SupportsCOD { get; set; } = true;
    public bool SupportsReverse { get; set; } = false;
    public decimal? MaxWeightKg { get; set; }
    public decimal? MaxValueAmount { get; set; }

    // Pricing
    public decimal PlatformMarginPercent { get; set; } = 10; // Platform's markup on courier rates
    public decimal? MinChargeAmount { get; set; }

    // Priority (for rate comparison)
    public int Priority { get; set; } = 1; // Lower = higher priority

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<CourierShipment> Shipments { get; set; } = new List<CourierShipment>();
}

/// <summary>
/// Courier shipment created via external courier API
/// </summary>
public class CourierShipment
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public Guid CourierPartnerId { get; set; }

    // AWB/Tracking
    public string AWBNumber { get; set; } = string.Empty;
    public string? OrderId { get; set; } // Our order ID sent to courier
    public string? CourierOrderId { get; set; } // Courier's internal order ID

    // Shipment Details
    public string ServiceType { get; set; } = "STANDARD"; // EXPRESS, STANDARD, ECONOMY
    public decimal WeightKg { get; set; }
    public string? Dimensions { get; set; } // JSON: {length, width, height}

    // Pricing
    public decimal CourierCharge { get; set; } // Rate from courier
    public decimal PlatformCharge { get; set; } // Our markup
    public decimal TotalCharge { get; set; } // Final price to customer
    public bool IsCOD { get; set; } = false;
    public decimal? CODAmount { get; set; }

    // Status
    // CREATED, PICKUP_SCHEDULED, PICKED_UP, IN_TRANSIT, OUT_FOR_DELIVERY, DELIVERED, RTO, CANCELLED
    public string Status { get; set; } = "CREATED";
    public string? CourierStatus { get; set; } // Raw status from courier
    public string? StatusReason { get; set; }

    // Tracking
    public DateTime? PickupScheduledAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Proof
    public string? DeliveryProofUrl { get; set; }
    public string? ReceiverName { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // API Response Storage
    public string? CreateResponse { get; set; } // JSON: Full API response
    public string? LastTrackingResponse { get; set; } // JSON: Last tracking response
    public DateTime? LastTrackedAt { get; set; }

    // Navigation
    public Delivery Delivery { get; set; } = null!;
    public CourierPartner CourierPartner { get; set; } = null!;
}

/// <summary>
/// Courier rate quote for comparison
/// </summary>
public class CourierRateQuote
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public Guid CourierPartnerId { get; set; }

    // Rate Details
    public string ServiceType { get; set; } = "STANDARD";
    public decimal BaseRate { get; set; }
    public decimal FuelSurcharge { get; set; }
    public decimal CODCharge { get; set; }
    public decimal TotalCourierCharge { get; set; }
    public decimal PlatformMargin { get; set; }
    public decimal FinalRate { get; set; }

    // Estimated Delivery
    public int EstimatedDays { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }

    // Validity
    public DateTime QuotedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsSelected { get; set; } = false;

    // Navigation
    public Delivery? Delivery { get; set; }
    public CourierPartner? CourierPartner { get; set; }
}
