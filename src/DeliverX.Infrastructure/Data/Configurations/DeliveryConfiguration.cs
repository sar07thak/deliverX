using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("Deliveries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequesterType)
            .HasMaxLength(20)
            .IsRequired();

        // Pickup
        builder.Property(x => x.PickupLat).HasPrecision(10, 8).IsRequired();
        builder.Property(x => x.PickupLng).HasPrecision(11, 8).IsRequired();
        builder.Property(x => x.PickupAddress).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PickupContactName).HasMaxLength(255);
        builder.Property(x => x.PickupContactPhone).HasMaxLength(15);
        builder.Property(x => x.PickupInstructions).HasMaxLength(500);

        // Drop
        builder.Property(x => x.DropLat).HasPrecision(10, 8).IsRequired();
        builder.Property(x => x.DropLng).HasPrecision(11, 8).IsRequired();
        builder.Property(x => x.DropAddress).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DropContactName).HasMaxLength(255);
        builder.Property(x => x.DropContactPhone).HasMaxLength(15);
        builder.Property(x => x.DropInstructions).HasMaxLength(500);

        // Package
        builder.Property(x => x.WeightKg).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.PackageType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PackageDimensions).HasMaxLength(500);
        builder.Property(x => x.PackageValue).HasPrecision(10, 2);
        builder.Property(x => x.PackageDescription).HasMaxLength(500);

        // Scheduling
        builder.Property(x => x.Priority).HasMaxLength(20).HasDefaultValue("ASAP");
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("CREATED");

        // Pricing
        builder.Property(x => x.EstimatedPrice).HasPrecision(10, 2);
        builder.Property(x => x.FinalPrice).HasPrecision(10, 2);

        // Metadata
        builder.Property(x => x.DistanceKm).HasPrecision(10, 2);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);

        // Indexes
        builder.HasIndex(x => x.RequesterId);
        builder.HasIndex(x => x.AssignedDPId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.Priority, x.Status });

        // Relationships
        builder.HasOne(x => x.Requester)
            .WithMany()
            .HasForeignKey(x => x.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedDP)
            .WithMany()
            .HasForeignKey(x => x.AssignedDPId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
