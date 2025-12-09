namespace DeliveryDost.Application.DTOs.Master;

/// <summary>
/// Request to lookup pincode details
/// </summary>
public class PincodeLookupRequest
{
    public string Pincode { get; set; } = string.Empty;
}

/// <summary>
/// Response for pincode lookup - returns state, district, and area details
/// </summary>
public class PincodeLookupResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public string DistrictName { get; set; } = string.Empty;
    public string? TalukName { get; set; }
    public List<AreaInfo> Areas { get; set; } = new();
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class AreaInfo
{
    public string AreaName { get; set; } = string.Empty;
    public string? OfficeName { get; set; }
    public string? OfficeType { get; set; }
}

/// <summary>
/// State dropdown item
/// </summary>
public class StateDto
{
    public int Id { get; set; }
    public string StateCode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
}

/// <summary>
/// District dropdown item
/// </summary>
public class DistrictDto
{
    public int Id { get; set; }
    public string StateCode { get; set; } = string.Empty;
    public string DistrictName { get; set; } = string.Empty;
}

/// <summary>
/// Request for getting districts by state
/// </summary>
public class GetDistrictsRequest
{
    public string StateCode { get; set; } = string.Empty;
}

/// <summary>
/// Request for getting pincodes by district
/// </summary>
public class GetPincodesRequest
{
    public string StateCode { get; set; } = string.Empty;
    public string DistrictName { get; set; } = string.Empty;
}

/// <summary>
/// Pincode dropdown item
/// </summary>
public class PincodeDto
{
    public string Pincode { get; set; } = string.Empty;
    public string? AreaName { get; set; }
}
