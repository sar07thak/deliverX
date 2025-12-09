using Microsoft.AspNetCore.Mvc;
using DeliveryDost.Application.Services;
using DeliveryDost.Application.DTOs.Master;

namespace DeliveryDost.Web.Controllers;

/// <summary>
/// API Controller for Master Data (Pincode, State, District)
/// Used for auto-populating address fields
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class MasterController : ControllerBase
{
    private readonly IPincodeService _pincodeService;
    private readonly ILogger<MasterController> _logger;

    public MasterController(IPincodeService pincodeService, ILogger<MasterController> logger)
    {
        _pincodeService = pincodeService;
        _logger = logger;
    }

    /// <summary>
    /// Lookup pincode to get State, District, and Area details
    /// </summary>
    /// <param name="pincode">6-digit Indian pincode</param>
    /// <returns>Pincode details including state, district, and areas</returns>
    [HttpGet("pincode/{pincode}")]
    [ProducesResponseType(typeof(PincodeLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PincodeLookupResponse>> LookupPincode(string pincode)
    {
        if (string.IsNullOrWhiteSpace(pincode) || pincode.Length != 6 || !pincode.All(char.IsDigit))
        {
            return BadRequest(new PincodeLookupResponse
            {
                Success = false,
                Message = "Invalid pincode. Please enter a 6-digit pincode.",
                Pincode = pincode
            });
        }

        var result = await _pincodeService.LookupPincodeAsync(pincode);
        return Ok(result);
    }

    /// <summary>
    /// Get all states for dropdown
    /// </summary>
    [HttpGet("states")]
    [ProducesResponseType(typeof(List<StateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StateDto>>> GetStates()
    {
        var states = await _pincodeService.GetStatesAsync();
        return Ok(states);
    }

    /// <summary>
    /// Get districts by state code
    /// </summary>
    /// <param name="stateCode">State code (e.g., RJ for Rajasthan)</param>
    [HttpGet("districts/{stateCode}")]
    [ProducesResponseType(typeof(List<DistrictDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DistrictDto>>> GetDistricts(string stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
        {
            return BadRequest("State code is required");
        }

        var districts = await _pincodeService.GetDistrictsByStateAsync(stateCode);
        return Ok(districts);
    }

    /// <summary>
    /// Get pincodes by state and district
    /// </summary>
    [HttpGet("pincodes")]
    [ProducesResponseType(typeof(List<PincodeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PincodeDto>>> GetPincodes(
        [FromQuery] string stateCode,
        [FromQuery] string districtName)
    {
        if (string.IsNullOrWhiteSpace(stateCode) || string.IsNullOrWhiteSpace(districtName))
        {
            return BadRequest("State code and district name are required");
        }

        var pincodes = await _pincodeService.GetPincodesByDistrictAsync(stateCode, districtName);
        return Ok(pincodes);
    }

    /// <summary>
    /// Validate if pincode exists
    /// </summary>
    [HttpGet("pincode/{pincode}/validate")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> ValidatePincode(string pincode)
    {
        var isValid = await _pincodeService.ValidatePincodeAsync(pincode);
        return Ok(isValid);
    }

    /// <summary>
    /// Get coordinates for a pincode
    /// </summary>
    [HttpGet("pincode/{pincode}/coordinates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetPincodeCoordinates(string pincode)
    {
        var coords = await _pincodeService.GetPincodeCoordinatesAsync(pincode);
        if (coords == null)
        {
            return NotFound(new { message = "Coordinates not available for this pincode" });
        }

        return Ok(new { latitude = coords.Value.Latitude, longitude = coords.Value.Longitude });
    }
}
