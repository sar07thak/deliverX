namespace DeliverX.Domain.Entities;

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
    public decimal? ServiceAreaCenterLat { get; set; }
    public decimal? ServiceAreaCenterLng { get; set; }
    public decimal? ServiceAreaRadiusKm { get; set; }
    public decimal? PerKmRate { get; set; }
    public decimal? PerKgRate { get; set; }
    public decimal? MinCharge { get; set; }
    public decimal? MaxDistanceKm { get; set; }
    public bool IsActive { get; set; } = false; // Activated only after KYC
    public DateTime? ActivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public DPCManager? DPCM { get; set; }
}
