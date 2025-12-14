using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.DTOs.PoolRoute;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class PoolRouteService : IPoolRouteService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PoolRouteService> _logger;
    private readonly IDistanceCalculatorService _distanceCalculator;

    public PoolRouteService(
        ApplicationDbContext context,
        ILogger<PoolRouteService> logger,
        IDistanceCalculatorService distanceCalculator)
    {
        _context = context;
        _logger = logger;
        _distanceCalculator = distanceCalculator;
    }

    // ========== POOL ROUTE MANAGEMENT ==========

    public async Task<PoolRouteDto> CreatePoolRouteAsync(CreatePoolRouteRequest request, CancellationToken ct = default)
    {
        var count = await _context.PoolRoutes.CountAsync(ct);
        var routeCode = $"PLR-{(count + 1):D4}";

        var distanceResult = _distanceCalculator.CalculateHaversineDistance(
            request.StartLat, request.StartLng,
            request.EndLat, request.EndLng);
        var distance = distanceResult.DistanceKm;

        var route = new PoolRoute
        {
            Id = Guid.NewGuid(),
            RouteCode = routeCode,
            Name = request.Name,
            Description = request.Description,
            StartPincode = request.StartPincode,
            EndPincode = request.EndPincode,
            StartLat = request.StartLat,
            StartLng = request.StartLng,
            EndLat = request.EndLat,
            EndLng = request.EndLng,
            DistanceKm = distance,
            EstimatedDurationMinutes = (int)(distance * 3), // Rough estimate: 20 km/h average
            MaxDeliveries = request.MaxDeliveries,
            BasePrice = request.BasePrice,
            PricePerKm = request.PricePerKm,
            ScheduleType = request.ScheduleType,
            ScheduleDays = request.ScheduleDays != null ? string.Join(",", request.ScheduleDays) : null,
            DepartureTimes = request.DepartureTimes != null ? string.Join(",", request.DepartureTimes) : null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PoolRoutes.Add(route);

        // Add stops
        foreach (var stopReq in request.Stops)
        {
            var stop = new PoolRouteStop
            {
                Id = Guid.NewGuid(),
                RouteId = route.Id,
                StopOrder = stopReq.StopOrder,
                Name = stopReq.Name,
                Pincode = stopReq.Pincode,
                Latitude = stopReq.Latitude,
                Longitude = stopReq.Longitude,
                EstimatedArrivalMinutes = stopReq.EstimatedArrivalMinutes,
                IsPickupPoint = stopReq.IsPickupPoint,
                IsDropPoint = stopReq.IsDropPoint,
                CreatedAt = DateTime.UtcNow
            };
            _context.PoolRouteStops.Add(stop);
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created pool route {RouteCode}: {Name}", routeCode, request.Name);

        return await GetPoolRouteAsync(route.Id, ct) ?? throw new InvalidOperationException("Failed to create route");
    }

    public async Task<PoolRouteDto?> GetPoolRouteAsync(Guid routeId, CancellationToken ct = default)
    {
        var route = await _context.PoolRoutes
            .Include(r => r.Stops.OrderBy(s => s.StopOrder))
            .FirstOrDefaultAsync(r => r.Id == routeId, ct);

        if (route == null) return null;

        return MapRouteToDto(route);
    }

    public async Task<List<PoolRouteDto>> GetAllPoolRoutesAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _context.PoolRoutes
            .Include(r => r.Stops.OrderBy(s => s.StopOrder))
            .AsQueryable();

        if (activeOnly)
            query = query.Where(r => r.IsActive);

        var routes = await query.OrderBy(r => r.Name).ToListAsync(ct);
        return routes.Select(MapRouteToDto).ToList();
    }

    public async Task<List<PoolRouteDto>> SearchRoutesAsync(string startPincode, string endPincode, CancellationToken ct = default)
    {
        var routes = await _context.PoolRoutes
            .Include(r => r.Stops.OrderBy(s => s.StopOrder))
            .Where(r => r.IsActive)
            .Where(r => r.StartPincode == startPincode || r.Stops.Any(s => s.Pincode == startPincode && s.IsPickupPoint))
            .Where(r => r.EndPincode == endPincode || r.Stops.Any(s => s.Pincode == endPincode && s.IsDropPoint))
            .ToListAsync(ct);

        return routes.Select(MapRouteToDto).ToList();
    }

    public async Task<bool> UpdatePoolRouteAsync(Guid routeId, CreatePoolRouteRequest request, CancellationToken ct = default)
    {
        var route = await _context.PoolRoutes.FindAsync(new object[] { routeId }, ct);
        if (route == null) return false;

        route.Name = request.Name;
        route.Description = request.Description;
        route.MaxDeliveries = request.MaxDeliveries;
        route.BasePrice = request.BasePrice;
        route.PricePerKm = request.PricePerKm;
        route.ScheduleType = request.ScheduleType;
        route.ScheduleDays = request.ScheduleDays != null ? string.Join(",", request.ScheduleDays) : null;
        route.DepartureTimes = request.DepartureTimes != null ? string.Join(",", request.DepartureTimes) : null;
        route.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeactivatePoolRouteAsync(Guid routeId, CancellationToken ct = default)
    {
        var route = await _context.PoolRoutes.FindAsync(new object[] { routeId }, ct);
        if (route == null) return false;

        route.IsActive = false;
        route.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    // ========== TRIP MANAGEMENT ==========

    public async Task<PoolRouteTripDto> CreateTripAsync(CreateTripRequest request, CancellationToken ct = default)
    {
        var route = await _context.PoolRoutes.FindAsync(new object[] { request.RouteId }, ct);
        if (route == null) throw new ArgumentException("Route not found");

        var tripNumber = $"TRP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var trip = new PoolRouteTrip
        {
            Id = Guid.NewGuid(),
            RouteId = request.RouteId,
            TripNumber = tripNumber,
            ScheduledDeparture = request.ScheduledDeparture,
            AssignedDPId = request.AssignedDPId,
            Status = "SCHEDULED",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PoolRouteTrips.Add(trip);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created trip {TripNumber} for route {RouteCode}", tripNumber, route.RouteCode);

        return await GetTripAsync(trip.Id, ct) ?? throw new InvalidOperationException("Failed to create trip");
    }

    public async Task<PoolRouteTripDto?> GetTripAsync(Guid tripId, CancellationToken ct = default)
    {
        var trip = await _context.PoolRouteTrips
            .Include(t => t.Route)
            .Include(t => t.AssignedDP)
            .Include(t => t.Deliveries)
                .ThenInclude(d => d.Delivery)
            .FirstOrDefaultAsync(t => t.Id == tripId, ct);

        if (trip == null) return null;

        return MapTripToDto(trip);
    }

    public async Task<List<PoolRouteTripDto>> GetTripsForRouteAsync(Guid routeId, DateTime? fromDate = null, CancellationToken ct = default)
    {
        var query = _context.PoolRouteTrips
            .Include(t => t.Route)
            .Include(t => t.AssignedDP)
            .Include(t => t.Deliveries)
            .Where(t => t.RouteId == routeId);

        if (fromDate.HasValue)
            query = query.Where(t => t.ScheduledDeparture >= fromDate.Value);

        var trips = await query.OrderByDescending(t => t.ScheduledDeparture).ToListAsync(ct);
        return trips.Select(MapTripToDto).ToList();
    }

    public async Task<List<PoolRouteTripDto>> GetTripsForDPAsync(Guid dpId, DateTime? fromDate = null, CancellationToken ct = default)
    {
        var query = _context.PoolRouteTrips
            .Include(t => t.Route)
            .Include(t => t.Deliveries)
            .Where(t => t.AssignedDPId == dpId);

        if (fromDate.HasValue)
            query = query.Where(t => t.ScheduledDeparture >= fromDate.Value);

        var trips = await query.OrderByDescending(t => t.ScheduledDeparture).ToListAsync(ct);
        return trips.Select(MapTripToDto).ToList();
    }

    public async Task<List<PoolRouteTripDto>> GetUpcomingTripsAsync(DateTime date, CancellationToken ct = default)
    {
        var trips = await _context.PoolRouteTrips
            .Include(t => t.Route)
            .Include(t => t.AssignedDP)
            .Include(t => t.Deliveries)
            .Where(t => t.ScheduledDeparture.Date == date.Date)
            .Where(t => t.Status == "SCHEDULED")
            .OrderBy(t => t.ScheduledDeparture)
            .ToListAsync(ct);

        return trips.Select(MapTripToDto).ToList();
    }

    public async Task<bool> AssignDPToTripAsync(Guid tripId, Guid dpId, CancellationToken ct = default)
    {
        var trip = await _context.PoolRouteTrips.FindAsync(new object[] { tripId }, ct);
        if (trip == null) return false;

        trip.AssignedDPId = dpId;
        trip.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> StartTripAsync(Guid tripId, CancellationToken ct = default)
    {
        var trip = await _context.PoolRouteTrips.FindAsync(new object[] { tripId }, ct);
        if (trip == null || trip.Status != "SCHEDULED") return false;

        trip.Status = "IN_PROGRESS";
        trip.ActualDeparture = DateTime.UtcNow;
        trip.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Trip {TripNumber} started", trip.TripNumber);
        return true;
    }

    public async Task<bool> CompleteTripAsync(Guid tripId, CancellationToken ct = default)
    {
        var trip = await _context.PoolRouteTrips
            .Include(t => t.Deliveries)
            .FirstOrDefaultAsync(t => t.Id == tripId, ct);

        if (trip == null || trip.Status != "IN_PROGRESS") return false;

        trip.Status = "COMPLETED";
        trip.ActualArrival = DateTime.UtcNow;
        trip.CompletedDeliveries = trip.Deliveries.Count(d => d.Status == "DELIVERED");
        trip.FailedDeliveries = trip.Deliveries.Count(d => d.Status == "FAILED");
        trip.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Trip {TripNumber} completed: {Completed} delivered, {Failed} failed",
            trip.TripNumber, trip.CompletedDeliveries, trip.FailedDeliveries);
        return true;
    }

    public async Task<bool> CancelTripAsync(Guid tripId, string reason, CancellationToken ct = default)
    {
        var trip = await _context.PoolRouteTrips.FindAsync(new object[] { tripId }, ct);
        if (trip == null || trip.Status == "COMPLETED") return false;

        trip.Status = "CANCELLED";
        trip.Notes = reason;
        trip.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Trip {TripNumber} cancelled: {Reason}", trip.TripNumber, reason);
        return true;
    }

    // ========== TRIP DELIVERIES ==========

    public async Task<bool> AssignDeliveryToTripAsync(Guid tripId, AssignDeliveryToTripRequest request, CancellationToken ct = default)
    {
        var trip = await _context.PoolRouteTrips.FindAsync(new object[] { tripId }, ct);
        if (trip == null) return false;

        var delivery = await _context.Deliveries.FindAsync(new object[] { request.DeliveryId }, ct);
        if (delivery == null) return false;

        var tripDelivery = new PoolTripDelivery
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            DeliveryId = request.DeliveryId,
            PickupStopOrder = request.PickupStopOrder,
            DropStopOrder = request.DropStopOrder,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        _context.PoolTripDeliveries.Add(tripDelivery);
        trip.TotalDeliveries++;
        trip.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveDeliveryFromTripAsync(Guid tripId, Guid deliveryId, CancellationToken ct = default)
    {
        var tripDelivery = await _context.PoolTripDeliveries
            .FirstOrDefaultAsync(td => td.TripId == tripId && td.DeliveryId == deliveryId, ct);

        if (tripDelivery == null) return false;

        var trip = await _context.PoolRouteTrips.FindAsync(new object[] { tripId }, ct);
        if (trip != null)
        {
            trip.TotalDeliveries = Math.Max(0, trip.TotalDeliveries - 1);
            trip.UpdatedAt = DateTime.UtcNow;
        }

        _context.PoolTripDeliveries.Remove(tripDelivery);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateTripDeliveryStatusAsync(Guid tripDeliveryId, string status, string? failureReason = null, CancellationToken ct = default)
    {
        var tripDelivery = await _context.PoolTripDeliveries.FindAsync(new object[] { tripDeliveryId }, ct);
        if (tripDelivery == null) return false;

        tripDelivery.Status = status;
        if (status == "PICKED_UP")
            tripDelivery.PickedUpAt = DateTime.UtcNow;
        else if (status == "DELIVERED")
            tripDelivery.DeliveredAt = DateTime.UtcNow;
        else if (status == "FAILED")
            tripDelivery.FailureReason = failureReason;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    // ========== FLEET MANAGEMENT ==========

    public async Task<FleetVehicleDto> CreateFleetVehicleAsync(Guid ownerId, CreateFleetVehicleRequest request, CancellationToken ct = default)
    {
        var vehicle = new FleetVehicle
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            VehicleNumber = request.VehicleNumber.ToUpper(),
            VehicleType = request.VehicleType,
            Make = request.Make,
            Model = request.Model,
            Year = request.Year,
            Color = request.Color,
            MaxWeightKg = request.MaxWeightKg,
            MaxVolumeCubicFt = request.MaxVolumeCubicFt,
            InsuranceNumber = request.InsuranceNumber,
            InsuranceExpiryDate = request.InsuranceExpiryDate,
            PermitNumber = request.PermitNumber,
            PermitExpiryDate = request.PermitExpiryDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.FleetVehicles.Add(vehicle);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created fleet vehicle {VehicleNumber} for owner {OwnerId}", vehicle.VehicleNumber, ownerId);

        return await GetFleetVehicleAsync(vehicle.Id, ct) ?? throw new InvalidOperationException("Failed to create vehicle");
    }

    public async Task<FleetVehicleDto?> GetFleetVehicleAsync(Guid vehicleId, CancellationToken ct = default)
    {
        var vehicle = await _context.FleetVehicles
            .Include(v => v.Owner)
            .FirstOrDefaultAsync(v => v.Id == vehicleId, ct);

        if (vehicle == null) return null;

        return MapVehicleToDto(vehicle);
    }

    public async Task<List<FleetVehicleDto>> GetFleetVehiclesForOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        var vehicles = await _context.FleetVehicles
            .Include(v => v.Owner)
            .Where(v => v.OwnerId == ownerId && v.IsActive)
            .ToListAsync(ct);

        return vehicles.Select(MapVehicleToDto).ToList();
    }

    public async Task<bool> UpdateFleetVehicleAsync(Guid vehicleId, CreateFleetVehicleRequest request, CancellationToken ct = default)
    {
        var vehicle = await _context.FleetVehicles.FindAsync(new object[] { vehicleId }, ct);
        if (vehicle == null) return false;

        vehicle.VehicleNumber = request.VehicleNumber.ToUpper();
        vehicle.VehicleType = request.VehicleType;
        vehicle.Make = request.Make;
        vehicle.Model = request.Model;
        vehicle.Year = request.Year;
        vehicle.Color = request.Color;
        vehicle.MaxWeightKg = request.MaxWeightKg;
        vehicle.MaxVolumeCubicFt = request.MaxVolumeCubicFt;
        vehicle.InsuranceNumber = request.InsuranceNumber;
        vehicle.InsuranceExpiryDate = request.InsuranceExpiryDate;
        vehicle.PermitNumber = request.PermitNumber;
        vehicle.PermitExpiryDate = request.PermitExpiryDate;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeactivateFleetVehicleAsync(Guid vehicleId, CancellationToken ct = default)
    {
        var vehicle = await _context.FleetVehicles.FindAsync(new object[] { vehicleId }, ct);
        if (vehicle == null) return false;

        vehicle.IsActive = false;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<FleetVehicleDto>> GetVehiclesWithExpiringDocumentsAsync(int daysAhead = 30, CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        var vehicles = await _context.FleetVehicles
            .Include(v => v.Owner)
            .Where(v => v.IsActive)
            .Where(v =>
                (v.InsuranceExpiryDate != null && v.InsuranceExpiryDate <= cutoffDate) ||
                (v.PermitExpiryDate != null && v.PermitExpiryDate <= cutoffDate) ||
                (v.FitnessExpiryDate != null && v.FitnessExpiryDate <= cutoffDate))
            .ToListAsync(ct);

        return vehicles.Select(MapVehicleToDto).ToList();
    }

    // ========== LOCATION TRACKING ==========

    public async Task UpdateDPLocationAsync(Guid dpId, UpdateLocationRequest request, CancellationToken ct = default)
    {
        var location = new DPLocationHistory
        {
            Id = Guid.NewGuid(),
            DPId = dpId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Speed = request.Speed,
            Heading = request.Heading,
            Accuracy = request.Accuracy,
            Source = request.Source,
            CurrentDeliveryId = request.CurrentDeliveryId,
            RecordedAt = DateTime.UtcNow
        };

        _context.DPLocationHistories.Add(location);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<DPLocationDto?> GetDPCurrentLocationAsync(Guid dpId, CancellationToken ct = default)
    {
        var location = await _context.DPLocationHistories
            .Include(l => l.DP)
            .Where(l => l.DPId == dpId)
            .OrderByDescending(l => l.RecordedAt)
            .FirstOrDefaultAsync(ct);

        if (location == null) return null;

        return new DPLocationDto
        {
            DPId = location.DPId,
            DPName = location.DP?.FullName,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Speed = location.Speed,
            Heading = location.Heading,
            CurrentDeliveryId = location.CurrentDeliveryId,
            RecordedAt = location.RecordedAt
        };
    }

    public async Task<List<DPLocationDto>> GetNearbyDPsAsync(decimal latitude, decimal longitude, decimal radiusKm = 5, CancellationToken ct = default)
    {
        // Get latest location for each DP within the last 15 minutes
        var cutoff = DateTime.UtcNow.AddMinutes(-15);

        var latestLocations = await _context.DPLocationHistories
            .Include(l => l.DP)
            .Where(l => l.RecordedAt >= cutoff)
            .GroupBy(l => l.DPId)
            .Select(g => g.OrderByDescending(l => l.RecordedAt).First())
            .ToListAsync(ct);

        // Filter by distance
        var nearbyDPs = new List<DPLocationDto>();
        foreach (var loc in latestLocations)
        {
            var distanceCalc = _distanceCalculator.CalculateHaversineDistance(latitude, longitude, loc.Latitude, loc.Longitude);
            if (distanceCalc.DistanceKm <= radiusKm)
            {
                nearbyDPs.Add(new DPLocationDto
                {
                    DPId = loc.DPId,
                    DPName = loc.DP?.FullName,
                    Latitude = loc.Latitude,
                    Longitude = loc.Longitude,
                    Speed = loc.Speed,
                    Heading = loc.Heading,
                    CurrentDeliveryId = loc.CurrentDeliveryId,
                    RecordedAt = loc.RecordedAt
                });
            }
        }

        return nearbyDPs;
    }

    public async Task<List<DPLocationDto>> GetDPLocationHistoryAsync(Guid dpId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var locations = await _context.DPLocationHistories
            .Where(l => l.DPId == dpId)
            .Where(l => l.RecordedAt >= fromDate && l.RecordedAt <= toDate)
            .OrderBy(l => l.RecordedAt)
            .ToListAsync(ct);

        return locations.Select(l => new DPLocationDto
        {
            DPId = l.DPId,
            Latitude = l.Latitude,
            Longitude = l.Longitude,
            Speed = l.Speed,
            Heading = l.Heading,
            CurrentDeliveryId = l.CurrentDeliveryId,
            RecordedAt = l.RecordedAt
        }).ToList();
    }

    // ========== ROUTE OPTIMIZATION ==========

    public async Task<OptimizedRouteDto> OptimizeRouteAsync(Guid requestedById, OptimizeRouteRequest request, CancellationToken ct = default)
    {
        // Create optimization request record
        var optRequest = new RouteOptimizationRequest
        {
            Id = Guid.NewGuid(),
            RequestedById = requestedById,
            Status = "PROCESSING",
            StartLat = request.StartLat,
            StartLng = request.StartLng,
            DeliveryIds = string.Join(",", request.DeliveryIds),
            RequestedAt = DateTime.UtcNow
        };

        _context.RouteOptimizationRequests.Add(optRequest);
        await _context.SaveChangesAsync(ct);

        // Get deliveries
        var deliveries = await _context.Deliveries
            .Where(d => request.DeliveryIds.Contains(d.Id))
            .ToListAsync(ct);

        // Simple nearest-neighbor optimization
        var optimizedStops = new List<OptimizedStopDto>();
        var remainingDeliveries = deliveries.ToList();
        var currentLat = request.StartLat;
        var currentLng = request.StartLng;
        var totalDistance = 0m;
        var totalDuration = 0;
        var order = 1;

        while (remainingDeliveries.Any())
        {
            // Find nearest pickup or drop
            var nearest = remainingDeliveries
                .SelectMany(d => new[]
                {
                    new { Delivery = d, Type = "PICKUP", Lat = d.PickupLat, Lng = d.PickupLng, Address = d.PickupAddress },
                    new { Delivery = d, Type = "DROP", Lat = d.DropLat, Lng = d.DropLng, Address = d.DropAddress }
                })
                .Select(x => new
                {
                    x.Delivery,
                    x.Type,
                    x.Lat,
                    x.Lng,
                    x.Address,
                    Distance = _distanceCalculator.CalculateHaversineDistance(currentLat, currentLng, x.Lat, x.Lng).DistanceKm
                })
                .OrderBy(x => x.Distance)
                .First();

            var duration = (int)(nearest.Distance * 3); // Rough: 20km/h average

            optimizedStops.Add(new OptimizedStopDto
            {
                Order = order++,
                DeliveryId = nearest.Delivery.Id,
                TrackingNumber = nearest.Delivery.Id.ToString()[..8].ToUpper(),
                StopType = nearest.Type,
                Latitude = nearest.Lat,
                Longitude = nearest.Lng,
                Address = nearest.Address,
                DistanceFromPrevious = nearest.Distance,
                DurationFromPrevious = duration
            });

            totalDistance += nearest.Distance;
            totalDuration += duration;
            currentLat = nearest.Lat;
            currentLng = nearest.Lng;

            if (nearest.Type == "DROP")
                remainingDeliveries.Remove(nearest.Delivery);
        }

        // Update request
        optRequest.Status = "COMPLETED";
        optRequest.OptimizedOrder = string.Join(",", optimizedStops.Select(s => s.DeliveryId));
        optRequest.TotalDistanceKm = totalDistance;
        optRequest.TotalDurationMinutes = totalDuration;
        optRequest.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return new OptimizedRouteDto
        {
            RequestId = optRequest.Id,
            OptimizedDeliveryOrder = optimizedStops.Where(s => s.StopType == "PICKUP").Select(s => s.DeliveryId).Distinct().ToList(),
            TotalDistanceKm = totalDistance,
            TotalDurationMinutes = totalDuration,
            Stops = optimizedStops
        };
    }

    // ========== STATISTICS ==========

    public async Task<PoolRouteStatsDto> GetPoolRouteStatsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        var stats = new PoolRouteStatsDto
        {
            TotalRoutes = await _context.PoolRoutes.CountAsync(ct),
            ActiveRoutes = await _context.PoolRoutes.CountAsync(r => r.IsActive, ct),
            TotalTripsToday = await _context.PoolRouteTrips.CountAsync(t => t.ScheduledDeparture.Date == today, ct),
            CompletedTripsToday = await _context.PoolRouteTrips.CountAsync(t => t.ScheduledDeparture.Date == today && t.Status == "COMPLETED", ct),
            TotalDeliveriesOnRoutes = await _context.PoolTripDeliveries.CountAsync(td => td.Trip != null && td.Trip.ScheduledDeparture.Date == today, ct),
            TotalRevenueToday = await _context.PoolRouteTrips.Where(t => t.ScheduledDeparture.Date == today && t.Status == "COMPLETED").SumAsync(t => t.TotalRevenue, ct)
        };

        var tripsByStatus = await _context.PoolRouteTrips
            .Where(t => t.ScheduledDeparture.Date == today)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        stats.TripsByStatus = tripsByStatus.ToDictionary(x => x.Status, x => x.Count);

        return stats;
    }

    // ========== HELPER METHODS ==========

    private PoolRouteDto MapRouteToDto(PoolRoute route)
    {
        return new PoolRouteDto
        {
            Id = route.Id,
            RouteCode = route.RouteCode,
            Name = route.Name,
            Description = route.Description,
            StartPincode = route.StartPincode,
            EndPincode = route.EndPincode,
            StartLat = route.StartLat,
            StartLng = route.StartLng,
            EndLat = route.EndLat,
            EndLng = route.EndLng,
            DistanceKm = route.DistanceKm,
            EstimatedDurationMinutes = route.EstimatedDurationMinutes,
            MaxDeliveries = route.MaxDeliveries,
            BasePrice = route.BasePrice,
            PricePerKm = route.PricePerKm,
            ScheduleType = route.ScheduleType,
            ScheduleDays = route.ScheduleDays?.Split(',').ToList() ?? new List<string>(),
            DepartureTimes = route.DepartureTimes?.Split(',').ToList() ?? new List<string>(),
            IsActive = route.IsActive,
            CreatedAt = route.CreatedAt,
            Stops = route.Stops.Select(s => new PoolRouteStopDto
            {
                Id = s.Id,
                StopOrder = s.StopOrder,
                Name = s.Name,
                Pincode = s.Pincode,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                EstimatedArrivalMinutes = s.EstimatedArrivalMinutes,
                IsPickupPoint = s.IsPickupPoint,
                IsDropPoint = s.IsDropPoint
            }).ToList()
        };
    }

    private PoolRouteTripDto MapTripToDto(PoolRouteTrip trip)
    {
        return new PoolRouteTripDto
        {
            Id = trip.Id,
            RouteId = trip.RouteId,
            RouteName = trip.Route?.Name,
            TripNumber = trip.TripNumber,
            AssignedDPId = trip.AssignedDPId,
            AssignedDPName = trip.AssignedDP?.FullName,
            ScheduledDeparture = trip.ScheduledDeparture,
            ActualDeparture = trip.ActualDeparture,
            ActualArrival = trip.ActualArrival,
            Status = trip.Status,
            TotalDeliveries = trip.TotalDeliveries,
            CompletedDeliveries = trip.CompletedDeliveries,
            FailedDeliveries = trip.FailedDeliveries,
            TotalRevenue = trip.TotalRevenue,
            DPEarning = trip.DPEarning,
            CreatedAt = trip.CreatedAt,
            Deliveries = trip.Deliveries.Select(d => new PoolTripDeliveryDto
            {
                Id = d.Id,
                DeliveryId = d.DeliveryId,
                DeliveryTrackingNumber = d.Delivery?.Id.ToString()[..8].ToUpper(),
                PickupStopOrder = d.PickupStopOrder,
                DropStopOrder = d.DropStopOrder,
                Status = d.Status,
                PickedUpAt = d.PickedUpAt,
                DeliveredAt = d.DeliveredAt
            }).ToList()
        };
    }

    private FleetVehicleDto MapVehicleToDto(FleetVehicle vehicle)
    {
        var now = DateTime.UtcNow;
        return new FleetVehicleDto
        {
            Id = vehicle.Id,
            OwnerId = vehicle.OwnerId,
            OwnerName = vehicle.Owner?.FullName,
            VehicleNumber = vehicle.VehicleNumber,
            VehicleType = vehicle.VehicleType,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            Color = vehicle.Color,
            MaxWeightKg = vehicle.MaxWeightKg,
            MaxVolumeCubicFt = vehicle.MaxVolumeCubicFt,
            InsuranceNumber = vehicle.InsuranceNumber,
            InsuranceExpiryDate = vehicle.InsuranceExpiryDate,
            IsInsuranceValid = vehicle.InsuranceExpiryDate.HasValue && vehicle.InsuranceExpiryDate.Value > now,
            PermitNumber = vehicle.PermitNumber,
            PermitExpiryDate = vehicle.PermitExpiryDate,
            IsPermitValid = vehicle.PermitExpiryDate.HasValue && vehicle.PermitExpiryDate.Value > now,
            IsActive = vehicle.IsActive,
            CreatedAt = vehicle.CreatedAt
        };
    }
}
