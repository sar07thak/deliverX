using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(rp => rp.CreatedAt)
            .HasDefaultValueSql("datetime('now')");

        // Unique constraint on Role + PermissionId
        builder.HasIndex(rp => new { rp.Role, rp.PermissionId })
            .IsUnique();

        builder.HasIndex(rp => rp.Role);
    }
}
