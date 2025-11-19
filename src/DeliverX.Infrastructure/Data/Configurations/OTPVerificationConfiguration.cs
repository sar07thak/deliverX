using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class OTPVerificationConfiguration : IEntityTypeConfiguration<OTPVerification>
{
    public void Configure(EntityTypeBuilder<OTPVerification> builder)
    {
        builder.ToTable("OTPVerifications");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Phone)
            .IsRequired()
            .HasMaxLength(15)
            .IsUnicode(false);

        builder.Property(o => o.OTPHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(o => o.CreatedAt)
            .HasDefaultValueSql("datetime('now')");

        // Index for efficient lookup
        builder.HasIndex(o => new { o.Phone, o.ExpiresAt });
    }
}
