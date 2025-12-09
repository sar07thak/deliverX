using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class DPCMCommissionConfigConfiguration : IEntityTypeConfiguration<DPCMCommissionConfig>
{
    public void Configure(EntityTypeBuilder<DPCMCommissionConfig> builder)
    {
        builder.ToTable("DPCMCommissionConfigs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CommissionType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CommissionValue)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.MinCommissionAmount)
            .HasPrecision(10, 2)
            .HasDefaultValue(0m);

        builder.Property(x => x.MaxCommissionAmount)
            .HasPrecision(10, 2);

        builder.HasIndex(x => x.DPCMId);

        builder.HasOne(x => x.DPCM)
            .WithMany()
            .HasForeignKey(x => x.DPCMId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
