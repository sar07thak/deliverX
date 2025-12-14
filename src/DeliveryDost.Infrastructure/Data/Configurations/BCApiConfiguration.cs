using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class BCApiCredentialConfiguration : IEntityTypeConfiguration<BCApiCredential>
{
    public void Configure(EntityTypeBuilder<BCApiCredential> builder)
    {
        builder.ToTable("BCApiCredentials");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApiKeyId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ApiKeyHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Environment)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.LastUsedIp)
            .HasMaxLength(50);

        builder.Property(x => x.AllowedIps)
            .HasMaxLength(1000);

        builder.Property(x => x.Scopes)
            .HasMaxLength(500);

        builder.HasIndex(x => x.ApiKeyId).IsUnique();
        builder.HasIndex(x => x.BusinessConsumerId);
        builder.HasIndex(x => new { x.BusinessConsumerId, x.Environment });

        builder.HasOne(x => x.BusinessConsumer)
            .WithMany()
            .HasForeignKey(x => x.BusinessConsumerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class BCOAuthTokenConfiguration : IEntityTypeConfiguration<BCOAuthToken>
{
    public void Configure(EntityTypeBuilder<BCOAuthToken> builder)
    {
        builder.ToTable("BCOAuthTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccessTokenHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.RefreshTokenHash)
            .HasMaxLength(256);

        builder.Property(x => x.Scopes)
            .HasMaxLength(500);

        builder.Property(x => x.IssuedToIp)
            .HasMaxLength(50);

        builder.Property(x => x.RevokedReason)
            .HasMaxLength(500);

        builder.HasIndex(x => x.ApiCredentialId);
        builder.HasIndex(x => x.ExpiresAt);

        builder.HasOne(x => x.ApiCredential)
            .WithMany()
            .HasForeignKey(x => x.ApiCredentialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApiKeyUsageLogConfiguration : IEntityTypeConfiguration<ApiKeyUsageLog>
{
    public void Configure(EntityTypeBuilder<ApiKeyUsageLog> builder)
    {
        builder.ToTable("ApiKeyUsageLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Endpoint)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.HttpMethod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.RequestIp)
            .HasMaxLength(50);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(500);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.ApiCredentialId);
        builder.HasIndex(x => x.RequestedAt);

        builder.HasOne(x => x.ApiCredential)
            .WithMany(c => c.UsageLogs)
            .HasForeignKey(x => x.ApiCredentialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BCWebhookConfiguration : IEntityTypeConfiguration<BCWebhook>
{
    public void Configure(EntityTypeBuilder<BCWebhook> builder)
    {
        builder.ToTable("BCWebhooks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WebhookUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Events)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Secret)
            .HasMaxLength(256);

        builder.Property(x => x.LastErrorMessage)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.BusinessConsumerId);
        builder.HasIndex(x => x.IsActive);

        builder.HasOne(x => x.BusinessConsumer)
            .WithMany()
            .HasForeignKey(x => x.BusinessConsumerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class WebhookDeliveryLogConfiguration : IEntityTypeConfiguration<WebhookDeliveryLog>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLog> builder)
    {
        builder.ToTable("WebhookDeliveryLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Payload)
            .IsRequired();

        builder.Property(x => x.ResponseBody)
            .HasMaxLength(4000);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.WebhookId);
        builder.HasIndex(x => x.AttemptedAt);

        builder.HasOne(x => x.Webhook)
            .WithMany(w => w.DeliveryLogs)
            .HasForeignKey(x => x.WebhookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApiRateLimitEntryConfiguration : IEntityTypeConfiguration<ApiRateLimitEntry>
{
    public void Configure(EntityTypeBuilder<ApiRateLimitEntry> builder)
    {
        builder.ToTable("ApiRateLimitEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WindowKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.ApiCredentialId, x.WindowKey }).IsUnique();
        builder.HasIndex(x => x.ExpiresAt);
    }
}
