using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class BusinessConsumerProfileConfiguration : IEntityTypeConfiguration<BusinessConsumerProfile>
{
    public void Configure(EntityTypeBuilder<BusinessConsumerProfile> builder)
    {
        builder.ToTable("BusinessConsumerProfiles");

        builder.HasKey(bc => bc.Id);

        builder.Property(bc => bc.BusinessName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(bc => bc.ContactPersonName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(bc => bc.GSTIN)
            .HasMaxLength(15);

        builder.Property(bc => bc.PAN)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(bc => bc.BusinessCategory)
            .HasMaxLength(100);

        // New fields
        builder.Property(bc => bc.BusinessConstitution)
            .HasMaxLength(50);

        builder.Property(bc => bc.GSTRegistrationType)
            .HasMaxLength(20);

        builder.Property(bc => bc.IsActive)
            .HasDefaultValue(false);

        builder.Property(bc => bc.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(bc => bc.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Unique constraints
        builder.HasIndex(bc => bc.UserId)
            .IsUnique();

        // Relationships
        builder.HasOne(bc => bc.User)
            .WithMany()
            .HasForeignKey(bc => bc.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(bc => bc.SubscriptionPlan)
            .WithMany()
            .HasForeignKey(bc => bc.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

/// <summary>
/// Configuration for BCPickupLocation entity
/// </summary>
public class BCPickupLocationConfiguration : IEntityTypeConfiguration<BCPickupLocation>
{
    public void Configure(EntityTypeBuilder<BCPickupLocation> builder)
    {
        builder.ToTable("BCPickupLocations");

        builder.HasKey(pl => pl.Id);

        builder.Property(pl => pl.LocationName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pl => pl.AddressLine1)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(pl => pl.AddressLine2)
            .HasMaxLength(255);

        builder.Property(pl => pl.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pl => pl.State)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pl => pl.Pincode)
            .IsRequired()
            .HasMaxLength(6);

        builder.Property(pl => pl.Latitude)
            .HasPrecision(10, 7);

        builder.Property(pl => pl.Longitude)
            .HasPrecision(10, 7);

        builder.Property(pl => pl.ContactName)
            .HasMaxLength(100);

        builder.Property(pl => pl.ContactPhone)
            .HasMaxLength(15);

        builder.Property(pl => pl.IsDefault)
            .HasDefaultValue(false);

        builder.Property(pl => pl.IsActive)
            .HasDefaultValue(true);

        builder.Property(pl => pl.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(pl => pl.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Index for fast lookup
        builder.HasIndex(pl => pl.BusinessConsumerProfileId);
        builder.HasIndex(pl => pl.Pincode);

        // Relationships
        builder.HasOne(pl => pl.BusinessConsumerProfile)
            .WithMany()
            .HasForeignKey(pl => pl.BusinessConsumerProfileId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
