using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class PlatformFeeConfigConfiguration : IEntityTypeConfiguration<PlatformFeeConfig>
{
    public void Configure(EntityTypeBuilder<PlatformFeeConfig> builder)
    {
        builder.ToTable("PlatformFeeConfigs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FeeType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FeeCalculationType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.FeeValue)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.ApplicableRoles)
            .HasMaxLength(500);

        builder.Property(x => x.Conditions)
            .HasMaxLength(1000);

        builder.HasIndex(x => new { x.EffectiveFrom, x.EffectiveTo });
    }
}
