using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.DTOs.Master;
using DeliveryDost.Application.Services;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class PincodeService : IPincodeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PincodeService> _logger;

    public PincodeService(ApplicationDbContext context, ILogger<PincodeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PincodeLookupResponse> LookupPincodeAsync(string pincode, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pincode) || pincode.Length != 6)
            {
                return new PincodeLookupResponse
                {
                    Success = false,
                    Message = "Invalid pincode. Pincode must be 6 digits.",
                    Pincode = pincode
                };
            }

            var pincodeData = await _context.PincodeMasters
                .Where(p => p.Pincode == pincode && p.IsActive)
                .ToListAsync(cancellationToken);

            if (!pincodeData.Any())
            {
                return new PincodeLookupResponse
                {
                    Success = false,
                    Message = "Pincode not found in our database.",
                    Pincode = pincode
                };
            }

            var firstRecord = pincodeData.First();
            var avgLat = pincodeData.Where(p => p.Latitude.HasValue).Average(p => p.Latitude);
            var avgLng = pincodeData.Where(p => p.Longitude.HasValue).Average(p => p.Longitude);

            return new PincodeLookupResponse
            {
                Success = true,
                Pincode = pincode,
                StateName = firstRecord.StateName,
                StateCode = firstRecord.StateCode,
                DistrictName = firstRecord.DistrictName,
                TalukName = firstRecord.TalukName,
                Latitude = avgLat,
                Longitude = avgLng,
                Areas = pincodeData.Select(p => new AreaInfo
                {
                    AreaName = p.AreaName ?? p.OfficeName ?? "",
                    OfficeName = p.OfficeName,
                    OfficeType = p.OfficeType
                }).DistinctBy(a => a.AreaName).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up pincode {Pincode}", pincode);
            return new PincodeLookupResponse
            {
                Success = false,
                Message = "An error occurred while looking up pincode.",
                Pincode = pincode
            };
        }
    }

    public async Task<List<StateDto>> GetStatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.StateMasters
                .Where(s => s.IsActive)
                .OrderBy(s => s.StateName)
                .Select(s => new StateDto
                {
                    Id = s.Id,
                    StateCode = s.StateCode,
                    StateName = s.StateName
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching states");
            return new List<StateDto>();
        }
    }

    public async Task<List<DistrictDto>> GetDistrictsByStateAsync(string stateCode, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DistrictMasters
                .Where(d => d.StateCode == stateCode && d.IsActive)
                .OrderBy(d => d.DistrictName)
                .Select(d => new DistrictDto
                {
                    Id = d.Id,
                    StateCode = d.StateCode,
                    DistrictName = d.DistrictName
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching districts for state {StateCode}", stateCode);
            return new List<DistrictDto>();
        }
    }

    public async Task<List<PincodeDto>> GetPincodesByDistrictAsync(string stateCode, string districtName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.PincodeMasters
                .Where(p => p.StateCode == stateCode && p.DistrictName == districtName && p.IsActive)
                .GroupBy(p => p.Pincode)
                .Select(g => new PincodeDto
                {
                    Pincode = g.Key,
                    AreaName = g.First().AreaName
                })
                .OrderBy(p => p.Pincode)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pincodes for state {StateCode}, district {DistrictName}", stateCode, districtName);
            return new List<PincodeDto>();
        }
    }

    public async Task<bool> ValidatePincodeAsync(string pincode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pincode) || pincode.Length != 6)
            return false;

        return await _context.PincodeMasters
            .AnyAsync(p => p.Pincode == pincode && p.IsActive, cancellationToken);
    }

    public async Task<(decimal? Latitude, decimal? Longitude)?> GetPincodeCoordinatesAsync(string pincode, CancellationToken cancellationToken = default)
    {
        var coords = await _context.PincodeMasters
            .Where(p => p.Pincode == pincode && p.IsActive && p.Latitude.HasValue && p.Longitude.HasValue)
            .GroupBy(p => p.Pincode)
            .Select(g => new
            {
                Lat = g.Average(p => p.Latitude),
                Lng = g.Average(p => p.Longitude)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (coords == null)
            return null;

        return (coords.Lat, coords.Lng);
    }
}
