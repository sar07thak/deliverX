using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class PoliceVerificationConfiguration : IEntityTypeConfiguration<PoliceVerification>
{
    public void Configure(EntityTypeBuilder<PoliceVerification> builder)
    {
        builder.ToTable("PoliceVerifications");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.VerificationAgency)
            .HasMaxLength(255);

        builder.Property(p => p.RequestDocumentUrl)
            .HasMaxLength(500);

        builder.Property(p => p.ClearanceDocumentUrl)
            .HasMaxLength(500);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("PENDING");

        builder.Property(p => p.InitiatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.Status);

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
