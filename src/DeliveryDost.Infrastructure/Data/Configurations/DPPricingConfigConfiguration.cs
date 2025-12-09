using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class DPPricingConfigConfiguration : IEntityTypeConfiguration<DPPricingConfig>
{
    public void Configure(EntityTypeBuilder<DPPricingConfig> builder)
    {
        builder.ToTable("DPPricingConfigs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PerKmRate)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.PerKgRate)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.MinCharge)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.MaxDistanceKm)
            .HasPrecision(5, 2)
            .HasDefaultValue(20m);

        builder.Property(x => x.AcceptsPriorityDelivery)
            .HasDefaultValue(true);

        builder.Property(x => x.PrioritySurcharge)
            .HasPrecision(10, 2)
            .HasDefaultValue(0m);

        builder.Property(x => x.PeakHourSurcharge)
            .HasPrecision(10, 2)
            .HasDefaultValue(0m);

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("INR");

        builder.HasIndex(x => x.DPId);
        builder.HasIndex(x => new { x.EffectiveFrom, x.EffectiveTo });

        // Link to User instead of DeliveryPartnerProfile (user may not have profile yet)
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.DPId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
