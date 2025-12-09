using DeliveryDost.Application.DTOs.Master;

namespace DeliveryDost.Application.Services;

/// <summary>
/// Service for Pincode master data operations
/// </summary>
public interface IPincodeService
{
    /// <summary>
    /// Lookup pincode details - returns state, district, areas
    /// </summary>
    Task<PincodeLookupResponse> LookupPincodeAsync(string pincode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all states for dropdown
    /// </summary>
    Task<List<StateDto>> GetStatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get districts by state code
    /// </summary>
    Task<List<DistrictDto>> GetDistrictsByStateAsync(string stateCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pincodes by state and district
    /// </summary>
    Task<List<PincodeDto>> GetPincodesByDistrictAsync(string stateCode, string districtName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if pincode exists and is active
    /// </summary>
    Task<bool> ValidatePincodeAsync(string pincode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get coordinates for a pincode (average of all areas)
    /// </summary>
    Task<(decimal? Latitude, decimal? Longitude)?> GetPincodeCoordinatesAsync(string pincode, CancellationToken cancellationToken = default);
}
