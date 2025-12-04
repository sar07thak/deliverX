using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class DeliveryEventConfiguration : IEntityTypeConfiguration<DeliveryEvent>
{
    public void Configure(EntityTypeBuilder<DeliveryEvent> builder)
    {
        builder.ToTable("DeliveryEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FromStatus).HasMaxLength(50);
        builder.Property(x => x.ToStatus).HasMaxLength(50);
        builder.Property(x => x.ActorType).HasMaxLength(20);
        builder.Property(x => x.Metadata).HasMaxLength(2000);

        builder.HasIndex(x => x.DeliveryId);
        builder.HasIndex(x => x.Timestamp);

        builder.HasOne(x => x.Delivery)
            .WithMany()
            .HasForeignKey(x => x.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Actor)
            .WithMany()
            .HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
