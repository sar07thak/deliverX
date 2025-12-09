using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class PANVerificationConfiguration : IEntityTypeConfiguration<PANVerification>
{
    public void Configure(EntityTypeBuilder<PANVerification> builder)
    {
        builder.ToTable("PANVerifications");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PAN)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(p => p.NameAsPerPAN)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.PANStatus)
            .HasMaxLength(20);

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(p => p.PAN);
        builder.HasIndex(p => p.UserId);

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
