using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class BusinessConsumerProfileConfiguration : IEntityTypeConfiguration<BusinessConsumerProfile>
{
    public void Configure(EntityTypeBuilder<BusinessConsumerProfile> builder)
    {
        builder.ToTable("BusinessConsumerProfiles");

        builder.HasKey(bc => bc.Id);

        builder.Property(bc => bc.BusinessName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(bc => bc.ContactPersonName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(bc => bc.GSTIN)
            .HasMaxLength(15);

        builder.Property(bc => bc.PAN)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(bc => bc.BusinessCategory)
            .HasMaxLength(100);

        builder.Property(bc => bc.IsActive)
            .HasDefaultValue(false);

        builder.Property(bc => bc.CreatedAt)
            .HasDefaultValueSql("datetime('now')");

        builder.Property(bc => bc.UpdatedAt)
            .HasDefaultValueSql("datetime('now')");

        // Unique constraints
        builder.HasIndex(bc => bc.UserId)
            .IsUnique();

        // Relationships
        builder.HasOne(bc => bc.User)
            .WithMany()
            .HasForeignKey(bc => bc.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
