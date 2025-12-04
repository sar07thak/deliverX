using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class VehicleLicenseVerificationConfiguration : IEntityTypeConfiguration<VehicleLicenseVerification>
{
    public void Configure(EntityTypeBuilder<VehicleLicenseVerification> builder)
    {
        builder.ToTable("VehicleLicenseVerifications");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.LicenseNumber)
            .HasMaxLength(20);

        builder.Property(v => v.LicenseDocumentUrl)
            .HasMaxLength(500);

        builder.Property(v => v.VehicleNumber)
            .HasMaxLength(20);

        builder.Property(v => v.VehicleRCDocumentUrl)
            .HasMaxLength(500);

        builder.Property(v => v.VehicleType)
            .HasMaxLength(50);

        builder.Property(v => v.VehicleOwnerName)
            .HasMaxLength(255);

        builder.Property(v => v.CreatedAt)
            .HasDefaultValueSql("datetime('now')");

        // Indexes
        builder.HasIndex(v => v.UserId);

        // Relationships
        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
