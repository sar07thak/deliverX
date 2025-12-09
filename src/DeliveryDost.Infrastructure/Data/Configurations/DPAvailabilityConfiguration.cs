using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class DPAvailabilityConfiguration : IEntityTypeConfiguration<DPAvailability>
{
    public void Configure(EntityTypeBuilder<DPAvailability> builder)
    {
        builder.ToTable("DPAvailabilities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .HasDefaultValue("OFFLINE")
            .IsRequired();

        builder.Property(x => x.LastLocationLat).HasPrecision(10, 8);
        builder.Property(x => x.LastLocationLng).HasPrecision(11, 8);

        builder.HasIndex(x => x.DPId).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.DP)
            .WithMany()
            .HasForeignKey(x => x.DPId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.CurrentDelivery)
            .WithMany()
            .HasForeignKey(x => x.CurrentDeliveryId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
