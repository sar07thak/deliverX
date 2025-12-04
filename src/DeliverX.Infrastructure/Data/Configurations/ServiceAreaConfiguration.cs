using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class ServiceAreaConfiguration : IEntityTypeConfiguration<ServiceArea>
{
    public void Configure(EntityTypeBuilder<ServiceArea> builder)
    {
        builder.ToTable("ServiceAreas");

        builder.HasKey(sa => sa.Id);

        builder.Property(sa => sa.UserId)
            .IsRequired();

        builder.Property(sa => sa.UserRole)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("DP");

        builder.Property(sa => sa.AreaType)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("CIRCLE");

        builder.Property(sa => sa.CenterLat)
            .IsRequired()
            .HasPrecision(10, 8);

        builder.Property(sa => sa.CenterLng)
            .IsRequired()
            .HasPrecision(11, 8);

        builder.Property(sa => sa.RadiusKm)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(sa => sa.AreaName)
            .HasMaxLength(100);

        builder.Property(sa => sa.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(sa => sa.AllowDropOutsideArea)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(sa => sa.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("datetime('now')");

        builder.Property(sa => sa.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("datetime('now')");

        // Foreign key relationship
        builder.HasOne(sa => sa.User)
            .WithMany()
            .HasForeignKey(sa => sa.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(sa => sa.UserId);
        builder.HasIndex(sa => sa.IsActive);
        builder.HasIndex(sa => new { sa.UserId, sa.IsActive });
        builder.HasIndex(sa => new { sa.CenterLat, sa.CenterLng });
    }
}
