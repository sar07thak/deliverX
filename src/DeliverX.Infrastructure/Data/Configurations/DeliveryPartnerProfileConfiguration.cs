using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class DeliveryPartnerProfileConfiguration : IEntityTypeConfiguration<DeliveryPartnerProfile>
{
    public void Configure(EntityTypeBuilder<DeliveryPartnerProfile> builder)
    {
        builder.ToTable("DeliveryPartnerProfiles");

        builder.HasKey(dp => dp.Id);

        builder.Property(dp => dp.FullName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(dp => dp.ProfilePhotoUrl)
            .HasMaxLength(500);

        builder.Property(dp => dp.Gender)
            .HasMaxLength(10);

        builder.Property(dp => dp.VehicleType)
            .HasMaxLength(50);

        builder.Property(dp => dp.Availability)
            .HasMaxLength(50);

        builder.Property(dp => dp.ServiceAreaCenterLat)
            .HasPrecision(10, 8);

        builder.Property(dp => dp.ServiceAreaCenterLng)
            .HasPrecision(11, 8);

        builder.Property(dp => dp.ServiceAreaRadiusKm)
            .HasPrecision(5, 2);

        builder.Property(dp => dp.PerKmRate)
            .HasPrecision(10, 2);

        builder.Property(dp => dp.PerKgRate)
            .HasPrecision(10, 2);

        builder.Property(dp => dp.MinCharge)
            .HasPrecision(10, 2);

        builder.Property(dp => dp.MaxDistanceKm)
            .HasPrecision(5, 2);

        builder.Property(dp => dp.IsActive)
            .HasDefaultValue(false);

        builder.Property(dp => dp.CreatedAt)
            .HasDefaultValueSql("datetime('now')");

        builder.Property(dp => dp.UpdatedAt)
            .HasDefaultValueSql("datetime('now')");

        // Unique constraints
        builder.HasIndex(dp => dp.UserId)
            .IsUnique();

        // Indexes
        builder.HasIndex(dp => dp.DPCMId);
        builder.HasIndex(dp => dp.IsActive);

        // Relationships
        builder.HasOne(dp => dp.User)
            .WithMany()
            .HasForeignKey(dp => dp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dp => dp.DPCM)
            .WithMany(dpcm => dpcm.DeliveryPartners)
            .HasForeignKey(dp => dp.DPCMId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
