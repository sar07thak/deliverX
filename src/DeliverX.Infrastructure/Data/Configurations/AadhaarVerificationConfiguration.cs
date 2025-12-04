using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class AadhaarVerificationConfiguration : IEntityTypeConfiguration<AadhaarVerification>
{
    public void Configure(EntityTypeBuilder<AadhaarVerification> builder)
    {
        builder.ToTable("AadhaarVerifications");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AadhaarHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(a => a.AadhaarReferenceId)
            .HasMaxLength(255);

        builder.Property(a => a.NameAsPerAadhaar)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.Gender)
            .HasMaxLength(10);

        builder.Property(a => a.VerificationMethod)
            .HasMaxLength(50);

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("datetime('now')");

        // Unique constraints
        builder.HasIndex(a => a.UserId)
            .IsUnique();

        builder.HasIndex(a => a.AadhaarHash)
            .IsUnique();

        // Relationships
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
