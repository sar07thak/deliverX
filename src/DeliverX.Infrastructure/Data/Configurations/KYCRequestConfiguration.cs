using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class KYCRequestConfiguration : IEntityTypeConfiguration<KYCRequest>
{
    public void Configure(EntityTypeBuilder<KYCRequest> builder)
    {
        builder.ToTable("KYCRequests");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.VerificationType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(k => k.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("PENDING");

        builder.Property(k => k.Method)
            .HasMaxLength(50);

        builder.Property(k => k.RejectionReason)
            .HasMaxLength(500);

        builder.Property(k => k.InitiatedAt)
            .HasDefaultValueSql("datetime('now')");

        builder.Property(k => k.CreatedAt)
            .HasDefaultValueSql("datetime('now')");

        builder.Property(k => k.UpdatedAt)
            .HasDefaultValueSql("datetime('now')");

        // Indexes
        builder.HasIndex(k => k.UserId);
        builder.HasIndex(k => k.Status);
        builder.HasIndex(k => new { k.VerificationType, k.Status });

        // Relationships
        builder.HasOne(k => k.User)
            .WithMany()
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(k => k.Verifier)
            .WithMany()
            .HasForeignKey(k => k.VerifiedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
