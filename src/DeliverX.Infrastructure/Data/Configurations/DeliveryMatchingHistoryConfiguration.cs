using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class DeliveryMatchingHistoryConfiguration : IEntityTypeConfiguration<DeliveryMatchingHistory>
{
    public void Configure(EntityTypeBuilder<DeliveryMatchingHistory> builder)
    {
        builder.ToTable("DeliveryMatchingHistories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MatchingAttempt).HasDefaultValue(1);
        builder.Property(x => x.ResponseType).HasMaxLength(20);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);

        builder.HasIndex(x => x.DeliveryId);
        builder.HasIndex(x => x.DPId);

        builder.HasOne(x => x.Delivery)
            .WithMany()
            .HasForeignKey(x => x.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DP)
            .WithMany()
            .HasForeignKey(x => x.DPId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
