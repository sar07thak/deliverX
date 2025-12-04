namespace DeliverX.Application.DTOs.Registration;

public class ProfileCompleteRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime DOB { get; set; }
    public string? Gender { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public AddressDto? Address { get; set; }
    public string? VehicleType { get; set; }
    public List<string>? Languages { get; set; }
    public string? Availability { get; set; }
    public ServiceAreaDto? ServiceArea { get; set; }
    public PricingDto? Pricing { get; set; }
}

public class AddressDto
{
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
}

public class ServiceAreaDto
{
    public decimal CenterLat { get; set; }
    public decimal CenterLng { get; set; }
    public decimal RadiusKm { get; set; }
}

public class PricingDto
{
    public decimal PerKmRate { get; set; }
    public decimal PerKgRate { get; set; }
    public decimal MinCharge { get; set; }
    public decimal? MaxDistanceKm { get; set; }
}

public class ProfileCompleteResponse
{
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string NextStep { get; set; } = string.Empty;
}
