using DeliveryDost.Application.Common;

namespace DeliveryDost.Application.Services;

/// <summary>
/// Service for calculating distances between locations
/// Supports both Google Distance Matrix API and Haversine formula
/// </summary>
public interface IDistanceCalculatorService
{
    /// <summary>
    /// Calculate distance and duration between two points using Google Distance Matrix API
    /// Falls back to Haversine if Google API fails
    /// </summary>
    Task<Result<DistanceCalculationResult>> CalculateDistanceAsync(
        decimal originLat, decimal originLng,
        decimal destLat, decimal destLng,
        CancellationToken cancellationToken);

    /// <summary>
    /// Calculate distance using Haversine formula (straight-line distance)
    /// </summary>
    DistanceCalculationResult CalculateHaversineDistance(
        decimal originLat, decimal originLng,
        decimal destLat, decimal destLng);

    /// <summary>
    /// Calculate distance with route polyline (for map display)
    /// Uses Google Directions API
    /// </summary>
    Task<Result<RouteCalculationResult>> CalculateRouteAsync(
        decimal originLat, decimal originLng,
        decimal destLat, decimal destLng,
        CancellationToken cancellationToken);
}

/// <summary>
/// Result of distance calculation
/// </summary>
public class DistanceCalculationResult
{
    public decimal DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public string Source { get; set; } = "HAVERSINE"; // GOOGLE_API, HAVERSINE
    public string? ErrorMessage { get; set; }
    public bool IsSuccess { get; set; } = true;

    // For display
    public string DistanceDisplay => $"{DistanceKm:F1} km";
    public string DurationDisplay
    {
        get
        {
            if (DurationMinutes < 60)
                return $"{DurationMinutes} min";
            var hours = DurationMinutes / 60;
            var mins = DurationMinutes % 60;
            return mins > 0 ? $"{hours}h {mins}m" : $"{hours}h";
        }
    }
}

/// <summary>
/// Result of route calculation (includes polyline for map display)
/// </summary>
public class RouteCalculationResult : DistanceCalculationResult
{
    public string? EncodedPolyline { get; set; }
    public List<LatLngPoint>? RoutePoints { get; set; }
    public string? StartAddress { get; set; }
    public string? EndAddress { get; set; }
    public decimal? NorthEastLat { get; set; }
    public decimal? NorthEastLng { get; set; }
    public decimal? SouthWestLat { get; set; }
    public decimal? SouthWestLng { get; set; }
}

/// <summary>
/// Latitude/Longitude point
/// </summary>
public class LatLngPoint
{
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
}
