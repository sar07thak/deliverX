using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class DPCManagerConfiguration : IEntityTypeConfiguration<DPCManager>
{
    public void Configure(EntityTypeBuilder<DPCManager> builder)
    {
        builder.ToTable("DPCManagers");

        builder.HasKey(dpcm => dpcm.Id);

        builder.Property(dpcm => dpcm.OrganizationName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(dpcm => dpcm.ContactPersonName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(dpcm => dpcm.PAN)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(dpcm => dpcm.RegistrationCertificateUrl)
            .HasMaxLength(500);

        builder.Property(dpcm => dpcm.CommissionType)
            .HasMaxLength(20);

        builder.Property(dpcm => dpcm.CommissionValue)
            .HasPrecision(10, 2);

        builder.Property(dpcm => dpcm.IsActive)
            .HasDefaultValue(false);

        builder.Property(dpcm => dpcm.CreatedAt)
            .HasDefaultValueSql("datetime('now')");

        builder.Property(dpcm => dpcm.UpdatedAt)
            .HasDefaultValueSql("datetime('now')");

        // Unique constraints
        builder.HasIndex(dpcm => dpcm.UserId)
            .IsUnique();

        // Relationships
        builder.HasOne(dpcm => dpcm.User)
            .WithMany()
            .HasForeignKey(dpcm => dpcm.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
