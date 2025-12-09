namespace DeliveryDost.Domain.Entities;

public class DeliveryPartnerProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? DPCMId { get; set; } // If onboarded by DPCM
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
    public DateTime DOB { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; } // JSON
    public string? VehicleType { get; set; }
    public string? Languages { get; set; } // JSON array
    public string? Availability { get; set; } // 24x7, Part-time, Weekends

    // ====== SERVICE AREA (Group 4 - Enhanced with radius from latlong) ======
    // Service area center point
    public decimal? ServiceAreaCenterLat { get; set; }
    public decimal? ServiceAreaCenterLng { get; set; }

    // Service area radius in km (selectable by DP)
    public decimal? ServiceAreaRadiusKm { get; set; }

    // Alternative: Service area as polygon (for complex areas)
    public string? ServiceAreaPolygonJson { get; set; }

    // Service area pincodes (if pincode-based)
    public string? ServiceAreaPincodesJson { get; set; }

    // Service direction preference (Group 4 - One-direction deliveries)
    public bool OneDirectionOnly { get; set; } = false;
    public string? PreferredDirection { get; set; } // NORTH, SOUTH, EAST, WEST, ANY
    public decimal? DirectionAngleDegrees { get; set; } // For fine-grained direction control

    // ====== PRICING ======
    public decimal? PerKmRate { get; set; }
    public decimal? PerKgRate { get; set; }
    public decimal? MinCharge { get; set; }
    public decimal? MaxDistanceKm { get; set; }

    // Max rate from registration (used to validate bids - Group 4)
    public decimal? MaxBidRate { get; set; }

    // ====== CONCURRENT DELIVERIES (Group 4) ======
    public int MaxConcurrentDeliveries { get; set; } = 3;
    public int CurrentActiveDeliveries { get; set; } = 0;

    // ====== STATUS ======
    public bool IsActive { get; set; } = false; // Activated only after KYC
    public bool IsOnline { get; set; } = false;
    public DateTime? LastOnlineAt { get; set; }
    public DateTime? ActivatedAt { get; set; }

    // ====== TIMESTAMPS ======
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ====== NAVIGATION ======
    public User User { get; set; } = null!;
    public DPCManager? DPCM { get; set; }
}
