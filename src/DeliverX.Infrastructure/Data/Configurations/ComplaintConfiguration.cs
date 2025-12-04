using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("Complaints");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ComplaintNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.RaisedByType)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.AgainstType)
            .HasMaxLength(10);

        builder.Property(c => c.Category)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Severity)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.Subject)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Resolution)
            .HasMaxLength(50);

        builder.Property(c => c.ResolutionNotes)
            .HasMaxLength(1000);

        builder.HasIndex(c => c.ComplaintNumber).IsUnique();
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.Category);
        builder.HasIndex(c => c.CreatedAt);

        builder.HasOne(c => c.Delivery)
            .WithMany()
            .HasForeignKey(c => c.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.RaisedBy)
            .WithMany()
            .HasForeignKey(c => c.RaisedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.Against)
            .WithMany()
            .HasForeignKey(c => c.AgainstId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.AssignedTo)
            .WithMany()
            .HasForeignKey(c => c.AssignedToId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class ComplaintEvidenceConfiguration : IEntityTypeConfiguration<ComplaintEvidence>
{
    public void Configure(EntityTypeBuilder<ComplaintEvidence> builder)
    {
        builder.ToTable("ComplaintEvidences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.FileUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.HasOne(e => e.Complaint)
            .WithMany(c => c.Evidences)
            .HasForeignKey(e => e.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.UploadedBy)
            .WithMany()
            .HasForeignKey(e => e.UploadedById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class ComplaintCommentConfiguration : IEntityTypeConfiguration<ComplaintComment>
{
    public void Configure(EntityTypeBuilder<ComplaintComment> builder)
    {
        builder.ToTable("ComplaintComments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasOne(c => c.Complaint)
            .WithMany(c => c.Comments)
            .HasForeignKey(c => c.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class InspectorConfiguration : IEntityTypeConfiguration<Inspector>
{
    public void Configure(EntityTypeBuilder<Inspector> builder)
    {
        builder.ToTable("Inspectors");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InspectorCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.Email)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.Phone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Zone)
            .HasMaxLength(50);

        builder.Property(i => i.ResolutionRate)
            .HasPrecision(5, 2);

        builder.Property(i => i.AverageResolutionTimeHours)
            .HasPrecision(10, 2);

        builder.HasIndex(i => i.InspectorCode).IsUnique();
        builder.HasIndex(i => i.UserId).IsUnique();

        builder.HasOne(i => i.User)
            .WithOne()
            .HasForeignKey<Inspector>(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ComplaintSLAConfigConfiguration : IEntityTypeConfiguration<ComplaintSLAConfig>
{
    public void Configure(EntityTypeBuilder<ComplaintSLAConfig> builder)
    {
        builder.ToTable("ComplaintSLAConfigs");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Category)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.Severity)
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(s => new { s.Category, s.Severity }).IsUnique();
    }
}
