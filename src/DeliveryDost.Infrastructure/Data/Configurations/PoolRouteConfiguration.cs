using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class PoolRouteConfiguration : IEntityTypeConfiguration<PoolRoute>
{
    public void Configure(EntityTypeBuilder<PoolRoute> builder)
    {
        builder.ToTable("PoolRoutes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RouteCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.StartPincode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.EndPincode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.StartLat).HasPrecision(10, 7);
        builder.Property(x => x.StartLng).HasPrecision(10, 7);
        builder.Property(x => x.EndLat).HasPrecision(10, 7);
        builder.Property(x => x.EndLng).HasPrecision(10, 7);
        builder.Property(x => x.DistanceKm).HasPrecision(10, 2);
        builder.Property(x => x.BasePrice).HasPrecision(18, 2);
        builder.Property(x => x.PricePerKm).HasPrecision(18, 2);

        builder.Property(x => x.ScheduleType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.ScheduleDays)
            .HasMaxLength(50);

        builder.Property(x => x.DepartureTimes)
            .HasMaxLength(200);

        builder.HasIndex(x => x.RouteCode).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}

public class PoolRouteStopConfiguration : IEntityTypeConfiguration<PoolRouteStop>
{
    public void Configure(EntityTypeBuilder<PoolRouteStop> builder)
    {
        builder.ToTable("PoolRouteStops");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Pincode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.Latitude).HasPrecision(10, 7);
        builder.Property(x => x.Longitude).HasPrecision(10, 7);

        builder.HasIndex(x => x.RouteId);
        builder.HasIndex(x => new { x.RouteId, x.StopOrder });

        builder.HasOne(x => x.Route)
            .WithMany(r => r.Stops)
            .HasForeignKey(x => x.RouteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PoolRouteTripConfiguration : IEntityTypeConfiguration<PoolRouteTrip>
{
    public void Configure(EntityTypeBuilder<PoolRouteTrip> builder)
    {
        builder.ToTable("PoolRouteTrips");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TripNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.TotalRevenue).HasPrecision(18, 2);
        builder.Property(x => x.DPEarning).HasPrecision(18, 2);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.TripNumber).IsUnique();
        builder.HasIndex(x => x.RouteId);
        builder.HasIndex(x => x.ScheduledDeparture);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Route)
            .WithMany(r => r.Trips)
            .HasForeignKey(x => x.RouteId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.AssignedDP)
            .WithMany()
            .HasForeignKey(x => x.AssignedDPId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class PoolTripDeliveryConfiguration : IEntityTypeConfiguration<PoolTripDelivery>
{
    public void Configure(EntityTypeBuilder<PoolTripDelivery> builder)
    {
        builder.ToTable("PoolTripDeliveries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.HasIndex(x => x.TripId);
        builder.HasIndex(x => x.DeliveryId);

        builder.HasOne(x => x.Trip)
            .WithMany(t => t.Deliveries)
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Delivery)
            .WithMany()
            .HasForeignKey(x => x.DeliveryId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class FleetVehicleConfiguration : IEntityTypeConfiguration<FleetVehicle>
{
    public void Configure(EntityTypeBuilder<FleetVehicle> builder)
    {
        builder.ToTable("FleetVehicles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.VehicleNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.VehicleType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.Make)
            .HasMaxLength(50);

        builder.Property(x => x.Model)
            .HasMaxLength(50);

        builder.Property(x => x.Color)
            .HasMaxLength(30);

        builder.Property(x => x.MaxVolumeCubicFt).HasPrecision(10, 2);

        builder.Property(x => x.InsuranceNumber)
            .HasMaxLength(50);

        builder.Property(x => x.PermitNumber)
            .HasMaxLength(50);

        builder.Property(x => x.FitnessNumber)
            .HasMaxLength(50);

        builder.HasIndex(x => x.VehicleNumber).IsUnique();
        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => x.VehicleType);

        builder.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class DPLocationHistoryConfiguration : IEntityTypeConfiguration<DPLocationHistory>
{
    public void Configure(EntityTypeBuilder<DPLocationHistory> builder)
    {
        builder.ToTable("DPLocationHistories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Latitude).HasPrecision(10, 7);
        builder.Property(x => x.Longitude).HasPrecision(10, 7);
        builder.Property(x => x.Speed).HasPrecision(10, 2);
        builder.Property(x => x.Heading).HasPrecision(10, 2);
        builder.Property(x => x.Accuracy).HasPrecision(10, 2);

        builder.Property(x => x.Source)
            .HasMaxLength(20);

        builder.HasIndex(x => x.DPId);
        builder.HasIndex(x => x.RecordedAt);
        builder.HasIndex(x => new { x.DPId, x.RecordedAt });

        builder.HasOne(x => x.DP)
            .WithMany()
            .HasForeignKey(x => x.DPId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class RouteOptimizationRequestConfiguration : IEntityTypeConfiguration<RouteOptimizationRequest>
{
    public void Configure(EntityTypeBuilder<RouteOptimizationRequest> builder)
    {
        builder.ToTable("RouteOptimizationRequests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.StartLat).HasPrecision(10, 7);
        builder.Property(x => x.StartLng).HasPrecision(10, 7);
        builder.Property(x => x.TotalDistanceKm).HasPrecision(10, 2);

        builder.Property(x => x.DeliveryIds)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.OptimizedOrder)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.RequestedById);
        builder.HasIndex(x => x.RequestedAt);

        builder.HasOne(x => x.RequestedBy)
            .WithMany()
            .HasForeignKey(x => x.RequestedById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
