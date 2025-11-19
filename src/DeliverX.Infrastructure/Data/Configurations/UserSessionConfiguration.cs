using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.RefreshTokenHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(s => s.DeviceType)
            .HasMaxLength(50);

        builder.Property(s => s.DeviceId)
            .HasMaxLength(255);

        builder.Property(s => s.IpAddress)
            .HasMaxLength(45);

        builder.Property(s => s.UserAgent)
            .HasMaxLength(500);

        builder.Property(s => s.Location)
            .HasMaxLength(255);

        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("datetime('now')");

        builder.Property(s => s.LastActiveAt)
            .HasDefaultValueSql("datetime('now')");

        // Indexes
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.RefreshTokenHash);
        builder.HasIndex(s => s.ExpiresAt);
    }
}
