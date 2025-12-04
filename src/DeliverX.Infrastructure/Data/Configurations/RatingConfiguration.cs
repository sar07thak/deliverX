using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> builder)
    {
        builder.ToTable("Ratings");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RaterType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.TargetType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.Score)
            .IsRequired();

        builder.Property(r => r.Tags)
            .HasMaxLength(1000);

        builder.Property(r => r.Comment)
            .HasMaxLength(500);

        // Unique constraint: one rating per rater per delivery per target
        builder.HasIndex(r => new { r.DeliveryId, r.RaterId, r.TargetId })
            .IsUnique();

        builder.HasIndex(r => r.TargetId);
        builder.HasIndex(r => r.CreatedAt);
    }
}

public class BehaviorIndexConfiguration : IEntityTypeConfiguration<BehaviorIndex>
{
    public void Configure(EntityTypeBuilder<BehaviorIndex> builder)
    {
        builder.ToTable("BehaviorIndexes");

        builder.HasKey(b => b.UserId);

        builder.Property(b => b.AverageRating)
            .HasPrecision(3, 2);

        builder.Property(b => b.CompletionRate)
            .HasPrecision(5, 2);

        builder.Property(b => b.PunctualityRate)
            .HasPrecision(5, 2);

        builder.Property(b => b.ComplaintFreeRate)
            .HasPrecision(5, 2);

        builder.Property(b => b.BehaviorScore)
            .HasPrecision(5, 2);

        // Configure relationship with User - ignore navigation to avoid shadow FK
        builder.HasOne(b => b.User)
            .WithOne()
            .HasForeignKey<BehaviorIndex>(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
