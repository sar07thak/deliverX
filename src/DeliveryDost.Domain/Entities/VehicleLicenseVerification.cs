namespace DeliveryDost.Domain.Entities;

public class VehicleLicenseVerification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseDocumentUrl { get; set; }
    public DateTime? LicenseValidUpto { get; set; }
    public string? VehicleNumber { get; set; }
    public string? VehicleRCDocumentUrl { get; set; }
    public string? VehicleType { get; set; } // Two-wheeler, Four-wheeler, etc.
    public string? VehicleOwnerName { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
