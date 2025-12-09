using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.Common;
using DeliveryDost.Application.Services;

namespace DeliveryDost.Infrastructure.Services;

public class DistanceCalculatorService : IDistanceCalculatorService
{
    private readonly ILogger<DistanceCalculatorService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _googleApiKey;
    private const double EARTH_RADIUS_KM = 6371.0;

    public DistanceCalculatorService(
        ILogger<DistanceCalculatorService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _googleApiKey = configuration["Google:MapsApiKey"];
    }

    public async Task<Result<DistanceCalculationResult>> CalculateDistanceAsync(
        decimal originLat, decimal originLng,
        decimal destLat, decimal destLng,
        CancellationToken cancellationToken)
    {
        // Try Google Distance Matrix API first if API key is configured
        if (!string.IsNullOrEmpty(_googleApiKey))
        {
            try
            {
                var googleResult = await CalculateDistanceWithGoogleApiAsync(
                    originLat, originLng, destLat, destLng, cancellationToken);

                if (googleResult.IsSuccess && googleResult.Data != null && googleResult.Data.IsSuccess)
                {
                    return googleResult;
                }

                _logger.LogWarning("Google Distance API failed, falling back to Haversine. Error: {Error}",
                    googleResult.Data?.ErrorMessage ?? googleResult.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception calling Google Distance API, falling back to Haversine");
            }
        }

        // Fallback to Haversine formula
        var haversineResult = CalculateHaversineDistance(originLat, originLng, destLat, destLng);
        return Result<DistanceCalculationResult>.Success(haversineResult);
    }

    public DistanceCalculationResult CalculateHaversineDistance(
        decimal originLat, decimal originLng,
        decimal destLat, decimal destLng)
    {
        var lat1 = (double)originLat;
        var lon1 = (double)originLng;
        var lat2 = (double)destLat;
        var lon2 = (double)destLng;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distanceKm = EARTH_RADIUS_KM * c;

        // Estimate duration based on average speed (30 km/h in city traffic)
        var estimatedMinutes = (int)Math.Ceiling(distanceKm / 30 * 60);

        return new DistanceCalculationResult
        {
            DistanceKm = Math.Round((decimal)distanceKm, 2),
            DurationMinutes = estimatedMinutes,
            Source = "HAVERSINE",
            IsSuccess = true
        };
    }

    public async Task<Result<RouteCalculationResult>> CalculateRouteAsync(
        decimal originLat, decimal originLng,
        decimal destLat, decimal destLng,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_googleApiKey))
        {
            // Fallback to Haversine-based result without polyline
            var haversine = CalculateHaversineDistance(originLat, originLng, destLat, destLng);
            return Result<RouteCalculationResult>.Success(new RouteCalculationResult
            {
                DistanceKm = haversine.DistanceKm,
                DurationMinutes = haversine.DurationMinutes,
                Source = haversine.Source,
                IsSuccess = true
            });
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://maps.googleapis.com/maps/api/directions/json" +
                      $"?origin={originLat},{originLng}" +
                      $"&destination={destLat},{destLng}" +
                      $"&mode=driving" +
                      $"&key={_googleApiKey}";

            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<GoogleDirectionsResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Status != "OK" || result.Routes == null || result.Routes.Count == 0)
            {
                _logger.LogWarning("Google Directions API returned status: {Status}", result?.Status);
                var fallback = CalculateHaversineDistance(originLat, originLng, destLat, destLng);
                return Result<RouteCalculationResult>.Success(new RouteCalculationResult
                {
                    DistanceKm = fallback.DistanceKm,
                    DurationMinutes = fallback.DurationMinutes,
                    Source = "HAVERSINE",
                    IsSuccess = true
                });
            }

            var route = result.Routes[0];
            var leg = route.Legs?[0];

            return Result<RouteCalculationResult>.Success(new RouteCalculationResult
            {
                DistanceKm = Math.Round((decimal)(leg?.Distance?.Value ?? 0) / 1000, 2),
                DurationMinutes = (int)Math.Ceiling((double)(leg?.Duration?.Value ?? 0) / 60),
                Source = "GOOGLE_API",
                EncodedPolyline = route.Overview_Polyline?.Points,
                StartAddress = leg?.Start_Address,
                EndAddress = leg?.End_Address,
                NorthEastLat = (decimal?)(route.Bounds?.Northeast?.Lat),
                NorthEastLng = (decimal?)(route.Bounds?.Northeast?.Lng),
                SouthWestLat = (decimal?)(route.Bounds?.Southwest?.Lat),
                SouthWestLng = (decimal?)(route.Bounds?.Southwest?.Lng),
                IsSuccess = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating route with Google Directions API");
            var fallback = CalculateHaversineDistance(originLat, originLng, destLat, destLng);
            return Result<RouteCalculationResult>.Success(new RouteCalculationResult
            {
                DistanceKm = fallback.DistanceKm,
                DurationMinutes = fallback.DurationMinutes,
                Source = "HAVERSINE",
                IsSuccess = true,
                ErrorMessage = "Fallback to Haversine due to API error"
            });
        }
    }

    #region Private Helpers

    private async Task<Result<DistanceCalculationResult>> CalculateDistanceWithGoogleApiAsync(
        decimal originLat, decimal originLng,
        decimal destLat, decimal destLng,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://maps.googleapis.com/maps/api/distancematrix/json" +
                  $"?origins={originLat},{originLng}" +
                  $"&destinations={destLat},{destLng}" +
                  $"&mode=driving" +
                  $"&key={_googleApiKey}";

        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<GoogleDistanceMatrixResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result?.Status != "OK")
        {
            return Result<DistanceCalculationResult>.Success(new DistanceCalculationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Google API returned status: {result?.Status}"
            });
        }

        var element = result.Rows?.FirstOrDefault()?.Elements?.FirstOrDefault();
        if (element?.Status != "OK")
        {
            return Result<DistanceCalculationResult>.Success(new DistanceCalculationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Distance calculation failed: {element?.Status}"
            });
        }

        return Result<DistanceCalculationResult>.Success(new DistanceCalculationResult
        {
            DistanceKm = Math.Round((decimal)(element.Distance?.Value ?? 0) / 1000, 2),
            DurationMinutes = (int)Math.Ceiling((double)(element.Duration?.Value ?? 0) / 60),
            Source = "GOOGLE_API",
            IsSuccess = true
        });
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    #endregion

    #region Google API Response Models

    private class GoogleDistanceMatrixResponse
    {
        public string? Status { get; set; }
        public List<GoogleDistanceMatrixRow>? Rows { get; set; }
    }

    private class GoogleDistanceMatrixRow
    {
        public List<GoogleDistanceMatrixElement>? Elements { get; set; }
    }

    private class GoogleDistanceMatrixElement
    {
        public string? Status { get; set; }
        public GoogleDistanceValue? Distance { get; set; }
        public GoogleDistanceValue? Duration { get; set; }
    }

    private class GoogleDistanceValue
    {
        public int Value { get; set; }
        public string? Text { get; set; }
    }

    private class GoogleDirectionsResponse
    {
        public string? Status { get; set; }
        public List<GoogleDirectionsRoute>? Routes { get; set; }
    }

    private class GoogleDirectionsRoute
    {
        public List<GoogleDirectionsLeg>? Legs { get; set; }
        public GooglePolyline? Overview_Polyline { get; set; }
        public GoogleBounds? Bounds { get; set; }
    }

    private class GoogleDirectionsLeg
    {
        public GoogleDistanceValue? Distance { get; set; }
        public GoogleDistanceValue? Duration { get; set; }
        public string? Start_Address { get; set; }
        public string? End_Address { get; set; }
    }

    private class GooglePolyline
    {
        public string? Points { get; set; }
    }

    private class GoogleBounds
    {
        public GoogleLatLng? Northeast { get; set; }
        public GoogleLatLng? Southwest { get; set; }
    }

    private class GoogleLatLng
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    #endregion
}
