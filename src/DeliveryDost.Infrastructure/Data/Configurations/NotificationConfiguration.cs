using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class NewsAnnouncementConfiguration : IEntityTypeConfiguration<NewsAnnouncement>
{
    public void Configure(EntityTypeBuilder<NewsAnnouncement> builder)
    {
        builder.ToTable("NewsAnnouncements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.TargetAudience)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.IsPublished);
        builder.HasIndex(x => x.TargetAudience);
        builder.HasIndex(x => x.PublishAt);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class NewsReadStatusConfiguration : IEntityTypeConfiguration<NewsReadStatus>
{
    public void Configure(EntityTypeBuilder<NewsReadStatus> builder)
    {
        builder.ToTable("NewsReadStatuses");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.NewsId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.News)
            .WithMany(n => n.ReadStatuses)
            .HasForeignKey(x => x.NewsId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("UserNotifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ActionUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ActionType)
            .HasMaxLength(30);

        builder.Property(x => x.ReferenceType)
            .HasMaxLength(30);

        builder.Property(x => x.Channel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.SendError)
            .HasMaxLength(500);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.IsRead);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.UserId, x.IsRead });

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuietHoursStart)
            .HasMaxLength(10);

        builder.Property(x => x.QuietHoursEnd)
            .HasMaxLength(10);

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<NotificationPreference>(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class PushDeviceRegistrationConfiguration : IEntityTypeConfiguration<PushDeviceRegistration>
{
    public void Configure(EntityTypeBuilder<PushDeviceRegistration> builder)
    {
        builder.ToTable("PushDeviceRegistrations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DeviceToken)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Platform)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.DeviceModel)
            .HasMaxLength(100);

        builder.Property(x => x.AppVersion)
            .HasMaxLength(20);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.DeviceToken);
        builder.HasIndex(x => new { x.UserId, x.IsActive });

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TemplateCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Channel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Subject)
            .HasMaxLength(200);

        builder.Property(x => x.TitleTemplate)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.BodyTemplate)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.DefaultImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ActionType)
            .HasMaxLength(30);

        builder.Property(x => x.ActionUrlTemplate)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.TemplateCode, x.Channel }).IsUnique();
    }
}

public class NotificationCampaignConfiguration : IEntityTypeConfiguration<NotificationCampaign>
{
    public void Configure(EntityTypeBuilder<NotificationCampaign> builder)
    {
        builder.ToTable("NotificationCampaigns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.TargetAudience)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.TargetCriteria)
            .HasMaxLength(2000);

        builder.Property(x => x.Channel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ActionUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ScheduledAt);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
