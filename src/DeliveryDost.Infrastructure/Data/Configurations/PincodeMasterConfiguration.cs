using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class PincodeMasterConfiguration : IEntityTypeConfiguration<PincodeMaster>
{
    public void Configure(EntityTypeBuilder<PincodeMaster> builder)
    {
        builder.ToTable("PincodeMasters");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Pincode)
            .HasMaxLength(6)
            .IsRequired();

        builder.Property(p => p.StateName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.StateCode)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(p => p.DistrictName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.TalukName)
            .HasMaxLength(100);

        builder.Property(p => p.AreaName)
            .HasMaxLength(200);

        builder.Property(p => p.OfficeName)
            .HasMaxLength(200);

        builder.Property(p => p.OfficeType)
            .HasMaxLength(10);

        builder.Property(p => p.Delivery)
            .HasMaxLength(20);

        builder.Property(p => p.Latitude)
            .HasPrecision(10, 7);

        builder.Property(p => p.Longitude)
            .HasPrecision(10, 7);

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes for fast lookups
        builder.HasIndex(p => p.Pincode);
        builder.HasIndex(p => p.StateCode);
        builder.HasIndex(p => p.DistrictName);
        builder.HasIndex(p => new { p.Pincode, p.IsActive });
    }
}

public class StateMasterConfiguration : IEntityTypeConfiguration<StateMaster>
{
    public void Configure(EntityTypeBuilder<StateMaster> builder)
    {
        builder.ToTable("StateMasters");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StateCode)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(s => s.StateName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(s => s.StateCode).IsUnique();
        builder.HasIndex(s => s.StateName);
    }
}

public class DistrictMasterConfiguration : IEntityTypeConfiguration<DistrictMaster>
{
    public void Configure(EntityTypeBuilder<DistrictMaster> builder)
    {
        builder.ToTable("DistrictMasters");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.StateCode)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(d => d.DistrictName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(d => d.StateCode);
        builder.HasIndex(d => new { d.StateCode, d.DistrictName }).IsUnique();
    }
}
