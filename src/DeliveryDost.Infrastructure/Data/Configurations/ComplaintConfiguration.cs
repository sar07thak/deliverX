using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

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
            .OnDelete(DeleteBehavior.NoAction);

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
            .OnDelete(DeleteBehavior.NoAction);

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
            .OnDelete(DeleteBehavior.NoAction);

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
            .OnDelete(DeleteBehavior.NoAction);
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

public class FieldVisitConfiguration : IEntityTypeConfiguration<FieldVisit>
{
    public void Configure(EntityTypeBuilder<FieldVisit> builder)
    {
        builder.ToTable("FieldVisits");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.Address)
            .HasMaxLength(500);

        builder.Property(f => f.Notes)
            .HasMaxLength(2000);

        builder.Property(f => f.CancellationReason)
            .HasMaxLength(500);

        builder.Property(f => f.Latitude)
            .HasPrecision(10, 7);

        builder.Property(f => f.Longitude)
            .HasPrecision(10, 7);

        builder.HasIndex(f => f.ScheduledAt);
        builder.HasIndex(f => f.Status);

        builder.HasOne(f => f.Complaint)
            .WithMany()
            .HasForeignKey(f => f.ComplaintId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(f => f.Inspector)
            .WithMany()
            .HasForeignKey(f => f.InspectorId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class FieldVisitEvidenceConfiguration : IEntityTypeConfiguration<FieldVisitEvidence>
{
    public void Configure(EntityTypeBuilder<FieldVisitEvidence> builder)
    {
        builder.ToTable("FieldVisitEvidences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.FileName)
            .HasMaxLength(255);

        builder.Property(e => e.FileUrl)
            .HasMaxLength(500);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Latitude)
            .HasPrecision(10, 7);

        builder.Property(e => e.Longitude)
            .HasPrecision(10, 7);

        builder.HasOne(e => e.FieldVisit)
            .WithMany(f => f.Evidences)
            .HasForeignKey(e => e.FieldVisitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InvestigationReportConfiguration : IEntityTypeConfiguration<InvestigationReport>
{
    public void Configure(EntityTypeBuilder<InvestigationReport> builder)
    {
        builder.ToTable("InvestigationReports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Findings)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(r => r.Verdict)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.VerdictReason)
            .HasMaxLength(2000);

        builder.Property(r => r.RecommendedAction)
            .HasMaxLength(50);

        builder.Property(r => r.CompensationAmount)
            .HasPrecision(18, 2);

        builder.Property(r => r.PenaltyType)
            .HasMaxLength(20);

        builder.Property(r => r.PenaltyAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(r => r.ComplaintId).IsUnique();
        builder.HasIndex(r => r.Verdict);

        builder.HasOne(r => r.Complaint)
            .WithMany()
            .HasForeignKey(r => r.ComplaintId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.Inspector)
            .WithMany()
            .HasForeignKey(r => r.InspectorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.ApprovedBy)
            .WithMany()
            .HasForeignKey(r => r.ApprovedById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class SLABreachConfiguration : IEntityTypeConfiguration<SLABreach>
{
    public void Configure(EntityTypeBuilder<SLABreach> builder)
    {
        builder.ToTable("SLABreaches");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.BreachType)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(s => s.ComplaintId);
        builder.HasIndex(s => s.BreachedAt);

        builder.HasOne(s => s.Complaint)
            .WithMany()
            .HasForeignKey(s => s.ComplaintId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.EscalatedTo)
            .WithMany()
            .HasForeignKey(s => s.EscalatedToId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
