using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class DeliveryBidConfiguration : IEntityTypeConfiguration<DeliveryBid>
{
    public void Configure(EntityTypeBuilder<DeliveryBid> builder)
    {
        builder.ToTable("DeliveryBids");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BidAmount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.BidNotes)
            .HasMaxLength(500);

        builder.Property(x => x.DPLatitude)
            .HasPrecision(10, 8);

        builder.Property(x => x.DPLongitude)
            .HasPrecision(11, 8);

        builder.Property(x => x.DistanceToPickupKm)
            .HasPrecision(10, 2);

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .HasDefaultValue("PENDING");

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(x => x.DeliveryId);
        builder.HasIndex(x => x.DPId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.DeliveryId, x.DPId }).IsUnique();
        builder.HasIndex(x => new { x.DeliveryId, x.Status });

        // Relationships
        builder.HasOne(x => x.Delivery)
            .WithMany()
            .HasForeignKey(x => x.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DP)
            .WithMany()
            .HasForeignKey(x => x.DPId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class BiddingConfigConfiguration : IEntityTypeConfiguration<BiddingConfig>
{
    public void Configure(EntityTypeBuilder<BiddingConfig> builder)
    {
        builder.ToTable("BiddingConfigs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MinBidPercentage)
            .HasPrecision(5, 2)
            .HasDefaultValue(0.5m);

        builder.Property(x => x.MaxBidPercentage)
            .HasPrecision(5, 2)
            .HasDefaultValue(1.5m);
    }
}
