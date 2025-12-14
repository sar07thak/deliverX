using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs.PoolRoute;

namespace DeliveryDost.Application.Services;

public interface IPoolRouteService
{
    // Pool Route Management
    Task<PoolRouteDto> CreatePoolRouteAsync(CreatePoolRouteRequest request, CancellationToken ct = default);
    Task<PoolRouteDto?> GetPoolRouteAsync(Guid routeId, CancellationToken ct = default);
    Task<List<PoolRouteDto>> GetAllPoolRoutesAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<List<PoolRouteDto>> SearchRoutesAsync(string startPincode, string endPincode, CancellationToken ct = default);
    Task<bool> UpdatePoolRouteAsync(Guid routeId, CreatePoolRouteRequest request, CancellationToken ct = default);
    Task<bool> DeactivatePoolRouteAsync(Guid routeId, CancellationToken ct = default);

    // Trip Management
    Task<PoolRouteTripDto> CreateTripAsync(CreateTripRequest request, CancellationToken ct = default);
    Task<PoolRouteTripDto?> GetTripAsync(Guid tripId, CancellationToken ct = default);
    Task<List<PoolRouteTripDto>> GetTripsForRouteAsync(Guid routeId, DateTime? fromDate = null, CancellationToken ct = default);
    Task<List<PoolRouteTripDto>> GetTripsForDPAsync(Guid dpId, DateTime? fromDate = null, CancellationToken ct = default);
    Task<List<PoolRouteTripDto>> GetUpcomingTripsAsync(DateTime date, CancellationToken ct = default);
    Task<bool> AssignDPToTripAsync(Guid tripId, Guid dpId, CancellationToken ct = default);
    Task<bool> StartTripAsync(Guid tripId, CancellationToken ct = default);
    Task<bool> CompleteTripAsync(Guid tripId, CancellationToken ct = default);
    Task<bool> CancelTripAsync(Guid tripId, string reason, CancellationToken ct = default);

    // Trip Deliveries
    Task<bool> AssignDeliveryToTripAsync(Guid tripId, AssignDeliveryToTripRequest request, CancellationToken ct = default);
    Task<bool> RemoveDeliveryFromTripAsync(Guid tripId, Guid deliveryId, CancellationToken ct = default);
    Task<bool> UpdateTripDeliveryStatusAsync(Guid tripDeliveryId, string status, string? failureReason = null, CancellationToken ct = default);

    // Fleet Management
    Task<FleetVehicleDto> CreateFleetVehicleAsync(Guid ownerId, CreateFleetVehicleRequest request, CancellationToken ct = default);
    Task<FleetVehicleDto?> GetFleetVehicleAsync(Guid vehicleId, CancellationToken ct = default);
    Task<List<FleetVehicleDto>> GetFleetVehiclesForOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<bool> UpdateFleetVehicleAsync(Guid vehicleId, CreateFleetVehicleRequest request, CancellationToken ct = default);
    Task<bool> DeactivateFleetVehicleAsync(Guid vehicleId, CancellationToken ct = default);
    Task<List<FleetVehicleDto>> GetVehiclesWithExpiringDocumentsAsync(int daysAhead = 30, CancellationToken ct = default);

    // Location Tracking
    Task UpdateDPLocationAsync(Guid dpId, UpdateLocationRequest request, CancellationToken ct = default);
    Task<DPLocationDto?> GetDPCurrentLocationAsync(Guid dpId, CancellationToken ct = default);
    Task<List<DPLocationDto>> GetNearbyDPsAsync(decimal latitude, decimal longitude, decimal radiusKm = 5, CancellationToken ct = default);
    Task<List<DPLocationDto>> GetDPLocationHistoryAsync(Guid dpId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    // Route Optimization
    Task<OptimizedRouteDto> OptimizeRouteAsync(Guid requestedById, OptimizeRouteRequest request, CancellationToken ct = default);

    // Statistics
    Task<PoolRouteStatsDto> GetPoolRouteStatsAsync(CancellationToken ct = default);
}
