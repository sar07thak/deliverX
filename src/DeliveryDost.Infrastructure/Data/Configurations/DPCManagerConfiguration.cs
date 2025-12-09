using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

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

        builder.Property(dpcm => dpcm.MinCommissionAmount)
            .HasPrecision(10, 2);

        // Security Deposit fields
        builder.Property(dpcm => dpcm.SecurityDeposit)
            .HasPrecision(10, 2)
            .HasDefaultValue(0);

        builder.Property(dpcm => dpcm.SecurityDepositStatus)
            .HasMaxLength(20);

        builder.Property(dpcm => dpcm.SecurityDepositTransactionRef)
            .HasMaxLength(100);

        // Agreement Document fields
        builder.Property(dpcm => dpcm.AgreementDocumentUrl)
            .HasMaxLength(500);

        builder.Property(dpcm => dpcm.AgreementVersion)
            .HasMaxLength(20);

        builder.Property(dpcm => dpcm.IsActive)
            .HasDefaultValue(false);

        builder.Property(dpcm => dpcm.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(dpcm => dpcm.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Unique constraints
        builder.HasIndex(dpcm => dpcm.UserId)
            .IsUnique();

        // Relationships
        builder.HasOne(dpcm => dpcm.User)
            .WithMany()
            .HasForeignKey(dpcm => dpcm.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(dpcm => dpcm.PincodeMappings)
            .WithOne(pm => pm.DPCM)
            .HasForeignKey(pm => pm.DPCMId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

/// <summary>
/// Pincode-DPCM Mapping configuration
/// One Pincode = One DPCM (unique constraint)
/// </summary>
public class PincodeDPCMMappingConfiguration : IEntityTypeConfiguration<PincodeDPCMMapping>
{
    public void Configure(EntityTypeBuilder<PincodeDPCMMapping> builder)
    {
        builder.ToTable("PincodeDPCMMappings");

        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.Pincode)
            .IsRequired()
            .HasMaxLength(6);

        builder.Property(pm => pm.StateName)
            .HasMaxLength(100);

        builder.Property(pm => pm.DistrictName)
            .HasMaxLength(100);

        builder.Property(pm => pm.IsActive)
            .HasDefaultValue(true);

        builder.Property(pm => pm.AssignedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(pm => pm.DeactivationReason)
            .HasMaxLength(500);

        // Unique constraint: One pincode = One active DPCM
        // Using filtered index for active mappings only
        builder.HasIndex(pm => new { pm.Pincode, pm.IsActive })
            .HasFilter("[IsActive] = 1")
            .IsUnique();

        // Index for DPCM lookup
        builder.HasIndex(pm => pm.DPCMId);

        // Relationships
        builder.HasOne(pm => pm.DPCM)
            .WithMany(dpcm => dpcm.PincodeMappings)
            .HasForeignKey(pm => pm.DPCMId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(pm => pm.AssignedByUser)
            .WithMany()
            .HasForeignKey(pm => pm.AssignedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
