namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Pincode master table for auto-populating State/City/District based on pincode
/// One Pincode can map to multiple areas/localities
/// </summary>
public class PincodeMaster
{
    public int Id { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public string DistrictName { get; set; } = string.Empty;
    public string? TalukName { get; set; }
    public string? AreaName { get; set; }
    public string? OfficeName { get; set; } // Post Office Name
    public string? OfficeType { get; set; } // BO/SO/HO
    public string? Delivery { get; set; } // Delivery/Non-Delivery
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// State Master for dropdown
/// </summary>
public class StateMaster
{
    public int Id { get; set; }
    public string StateCode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// District Master for dropdown
/// </summary>
public class DistrictMaster
{
    public int Id { get; set; }
    public string StateCode { get; set; } = string.Empty;
    public string DistrictName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
