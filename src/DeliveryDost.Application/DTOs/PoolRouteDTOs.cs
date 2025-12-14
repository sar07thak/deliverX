using System;
using System.Collections.Generic;

namespace DeliveryDost.Application.DTOs.PoolRoute;

// Pool Route DTOs
public class CreatePoolRouteRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StartPincode { get; set; } = string.Empty;
    public string EndPincode { get; set; } = string.Empty;
    public decimal StartLat { get; set; }
    public decimal StartLng { get; set; }
    public decimal EndLat { get; set; }
    public decimal EndLng { get; set; }
    public int MaxDeliveries { get; set; } = 10;
    public decimal BasePrice { get; set; }
    public decimal PricePerKm { get; set; }
    public string ScheduleType { get; set; } = "DAILY";
    public List<string>? ScheduleDays { get; set; }
    public List<string>? DepartureTimes { get; set; }
    public List<CreatePoolRouteStopRequest> Stops { get; set; } = new();
}

public class CreatePoolRouteStopRequest
{
    public int StopOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int EstimatedArrivalMinutes { get; set; }
    public bool IsPickupPoint { get; set; } = true;
    public bool IsDropPoint { get; set; } = true;
}

public class PoolRouteDto
{
    public Guid Id { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StartPincode { get; set; } = string.Empty;
    public string EndPincode { get; set; } = string.Empty;
    public decimal StartLat { get; set; }
    public decimal StartLng { get; set; }
    public decimal EndLat { get; set; }
    public decimal EndLng { get; set; }
    public decimal DistanceKm { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public int MaxDeliveries { get; set; }
    public decimal BasePrice { get; set; }
    public decimal PricePerKm { get; set; }
    public string ScheduleType { get; set; } = string.Empty;
    public List<string> ScheduleDays { get; set; } = new();
    public List<string> DepartureTimes { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PoolRouteStopDto> Stops { get; set; } = new();
}

public class PoolRouteStopDto
{
    public Guid Id { get; set; }
    public int StopOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int EstimatedArrivalMinutes { get; set; }
    public bool IsPickupPoint { get; set; }
    public bool IsDropPoint { get; set; }
}

// Trip DTOs
public class CreateTripRequest
{
    public Guid RouteId { get; set; }
    public DateTime ScheduledDeparture { get; set; }
    public Guid? AssignedDPId { get; set; }
}

public class PoolRouteTripDto
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; }
    public string? RouteName { get; set; }
    public string TripNumber { get; set; } = string.Empty;
    public Guid? AssignedDPId { get; set; }
    public string? AssignedDPName { get; set; }
    public DateTime ScheduledDeparture { get; set; }
    public DateTime? ActualDeparture { get; set; }
    public DateTime? ActualArrival { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalDeliveries { get; set; }
    public int CompletedDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal DPEarning { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PoolTripDeliveryDto> Deliveries { get; set; } = new();
}

public class PoolTripDeliveryDto
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public string? DeliveryTrackingNumber { get; set; }
    public int PickupStopOrder { get; set; }
    public int DropStopOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public class AssignDeliveryToTripRequest
{
    public Guid DeliveryId { get; set; }
    public int PickupStopOrder { get; set; }
    public int DropStopOrder { get; set; }
}

// Fleet Vehicle DTOs
public class CreateFleetVehicleRequest
{
    public string VehicleNumber { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? Color { get; set; }
    public int MaxWeightKg { get; set; }
    public decimal MaxVolumeCubicFt { get; set; }
    public string? InsuranceNumber { get; set; }
    public DateTime? InsuranceExpiryDate { get; set; }
    public string? PermitNumber { get; set; }
    public DateTime? PermitExpiryDate { get; set; }
}

public class FleetVehicleDto
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? Color { get; set; }
    public int MaxWeightKg { get; set; }
    public decimal MaxVolumeCubicFt { get; set; }
    public string? InsuranceNumber { get; set; }
    public DateTime? InsuranceExpiryDate { get; set; }
    public bool IsInsuranceValid { get; set; }
    public string? PermitNumber { get; set; }
    public DateTime? PermitExpiryDate { get; set; }
    public bool IsPermitValid { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Location Tracking DTOs
public class UpdateLocationRequest
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Speed { get; set; }
    public decimal? Heading { get; set; }
    public decimal? Accuracy { get; set; }
    public string? Source { get; set; }
    public Guid? CurrentDeliveryId { get; set; }
}

public class DPLocationDto
{
    public Guid DPId { get; set; }
    public string? DPName { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Speed { get; set; }
    public decimal? Heading { get; set; }
    public Guid? CurrentDeliveryId { get; set; }
    public DateTime RecordedAt { get; set; }
}

// Route Optimization DTOs
public class OptimizeRouteRequest
{
    public decimal StartLat { get; set; }
    public decimal StartLng { get; set; }
    public List<Guid> DeliveryIds { get; set; } = new();
}

public class OptimizedRouteDto
{
    public Guid RequestId { get; set; }
    public List<Guid> OptimizedDeliveryOrder { get; set; } = new();
    public decimal TotalDistanceKm { get; set; }
    public int TotalDurationMinutes { get; set; }
    public List<OptimizedStopDto> Stops { get; set; } = new();
}

public class OptimizedStopDto
{
    public int Order { get; set; }
    public Guid DeliveryId { get; set; }
    public string? TrackingNumber { get; set; }
    public string StopType { get; set; } = string.Empty; // PICKUP, DROP
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public decimal DistanceFromPrevious { get; set; }
    public int DurationFromPrevious { get; set; }
}

// Statistics
public class PoolRouteStatsDto
{
    public int TotalRoutes { get; set; }
    public int ActiveRoutes { get; set; }
    public int TotalTripsToday { get; set; }
    public int CompletedTripsToday { get; set; }
    public int TotalDeliveriesOnRoutes { get; set; }
    public decimal TotalRevenueToday { get; set; }
    public Dictionary<string, int> TripsByStatus { get; set; } = new();
}
