using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class ProofOfDeliveryConfiguration : IEntityTypeConfiguration<ProofOfDelivery>
{
    public void Configure(EntityTypeBuilder<ProofOfDelivery> builder)
    {
        builder.ToTable("ProofOfDeliveries");

        builder.HasKey(p => p.Id);

        // One-to-one with Delivery
        builder.HasIndex(p => p.DeliveryId)
            .IsUnique();

        // Recipient Information
        builder.Property(p => p.RecipientName)
            .HasMaxLength(255);

        builder.Property(p => p.RecipientRelation)
            .HasMaxLength(50);

        // OTP
        builder.Property(p => p.RecipientOTP)
            .HasMaxLength(4);

        // Photo URLs
        builder.Property(p => p.PODPhotoUrl)
            .HasMaxLength(500);

        builder.Property(p => p.PackagePhotoUrl)
            .HasMaxLength(500);

        builder.Property(p => p.SignatureUrl)
            .HasMaxLength(500);

        // GPS Coordinates
        builder.Property(p => p.DeliveredLat)
            .HasPrecision(10, 8);

        builder.Property(p => p.DeliveredLng)
            .HasPrecision(11, 8);

        builder.Property(p => p.DistanceFromDropLocation)
            .HasPrecision(10, 2);

        // Notes and Metadata
        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.Property(p => p.DeliveryCondition)
            .HasMaxLength(50);

        // Timestamps
        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Navigation
        builder.HasOne(p => p.Delivery)
            .WithOne()
            .HasForeignKey<ProofOfDelivery>(p => p.DeliveryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.Verifier)
            .WithMany()
            .HasForeignKey(p => p.VerifiedBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
