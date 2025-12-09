using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class DeliveryPricingConfiguration : IEntityTypeConfiguration<DeliveryPricing>
{
    public void Configure(EntityTypeBuilder<DeliveryPricing> builder)
    {
        builder.ToTable("DeliveryPricings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DistanceKm)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.WeightKg)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.PerKmRate)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.PerKgRate)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.MinCharge)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.Surcharges)
            .HasMaxLength(2000);

        builder.Property(x => x.Subtotal)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.GSTPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(x => x.GSTAmount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.DPEarning)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.DPCMCommission)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.PlatformFee)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("INR");

        builder.HasIndex(x => x.DeliveryId);
        builder.HasIndex(x => x.DPId);

        // Link to User instead of DeliveryPartnerProfile
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.DPId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
