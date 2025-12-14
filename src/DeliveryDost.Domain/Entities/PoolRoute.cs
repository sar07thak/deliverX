using System;
using System.Collections.Generic;

namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Pool route for batched deliveries along a common path
/// </summary>
public class PoolRoute
{
    public Guid Id { get; set; }
    public string RouteCode { get; set; } = string.Empty; // PLR-XXXX
    public string Name { get; set; } = string.Empty; // "Downtown to Airport Route"
    public string? Description { get; set; }
    public string StartPincode { get; set; } = string.Empty;
    public string EndPincode { get; set; } = string.Empty;
    public decimal StartLat { get; set; }
    public decimal StartLng { get; set; }
    public decimal EndLat { get; set; }
    public decimal EndLng { get; set; }
    public decimal DistanceKm { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public int MaxDeliveries { get; set; } = 10; // Max deliveries per trip
    public decimal BasePrice { get; set; } // Base price for this route
    public decimal PricePerKm { get; set; }
    public string ScheduleType { get; set; } = "DAILY"; // DAILY, WEEKLY, ON_DEMAND
    public string? ScheduleDays { get; set; } // "MON,TUE,WED,THU,FRI" for weekly
    public string? DepartureTimes { get; set; } // "09:00,14:00,18:00" multiple departure times
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<PoolRouteStop> Stops { get; set; } = new List<PoolRouteStop>();
    public ICollection<PoolRouteTrip> Trips { get; set; } = new List<PoolRouteTrip>();
}

/// <summary>
/// Intermediate stops along a pool route
/// </summary>
public class PoolRouteStop
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; }
    public int StopOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int EstimatedArrivalMinutes { get; set; } // Minutes from start
    public bool IsPickupPoint { get; set; } = true;
    public bool IsDropPoint { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PoolRoute? Route { get; set; }
}

/// <summary>
/// Scheduled trip instance for a pool route
/// </summary>
public class PoolRouteTrip
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; }
    public Guid? AssignedDPId { get; set; }
    public string TripNumber { get; set; } = string.Empty; // TRP-YYYYMMDD-XXXX
    public DateTime ScheduledDeparture { get; set; }
    public DateTime? ActualDeparture { get; set; }
    public DateTime? ActualArrival { get; set; }
    public string Status { get; set; } = "SCHEDULED"; // SCHEDULED, IN_PROGRESS, COMPLETED, CANCELLED
    public int TotalDeliveries { get; set; }
    public int CompletedDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal DPEarning { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PoolRoute? Route { get; set; }
    public User? AssignedDP { get; set; }
    public ICollection<PoolTripDelivery> Deliveries { get; set; } = new List<PoolTripDelivery>();
}

/// <summary>
/// Delivery assigned to a pool route trip
/// </summary>
public class PoolTripDelivery
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid DeliveryId { get; set; }
    public int PickupStopOrder { get; set; }
    public int DropStopOrder { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, PICKED_UP, DELIVERED, FAILED
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PoolRouteTrip? Trip { get; set; }
    public Delivery? Delivery { get; set; }
}

/// <summary>
/// Fleet vehicle for DPs with vehicles
/// </summary>
public class FleetVehicle
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; } // DP or DPCM
    public string VehicleNumber { get; set; } = string.Empty; // Registration number
    public string VehicleType { get; set; } = string.Empty; // TWO_WHEELER, THREE_WHEELER, FOUR_WHEELER, TEMPO
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
    public string? FitnessNumber { get; set; }
    public DateTime? FitnessExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Owner { get; set; }
}

/// <summary>
/// Real-time location tracking for DPs
/// </summary>
public class DPLocationHistory
{
    public Guid Id { get; set; }
    public Guid DPId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Speed { get; set; } // km/h
    public decimal? Heading { get; set; } // 0-360 degrees
    public decimal? Accuracy { get; set; } // meters
    public string? Source { get; set; } // GPS, NETWORK
    public Guid? CurrentDeliveryId { get; set; }
    public Guid? CurrentTripId { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? DP { get; set; }
}

/// <summary>
/// Route optimization request for multiple deliveries
/// </summary>
public class RouteOptimizationRequest
{
    public Guid Id { get; set; }
    public Guid RequestedById { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, PROCESSING, COMPLETED, FAILED
    public decimal StartLat { get; set; }
    public decimal StartLng { get; set; }
    public string DeliveryIds { get; set; } = string.Empty; // Comma-separated delivery IDs
    public string? OptimizedOrder { get; set; } // Result: ordered delivery IDs
    public decimal? TotalDistanceKm { get; set; }
    public int? TotalDurationMinutes { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public User? RequestedBy { get; set; }
}
