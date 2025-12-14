using System;
using System.Collections.Generic;

namespace DeliveryDost.Domain.Entities;

/// <summary>
/// News/Announcement posted by admin
/// </summary>
public class NewsAnnouncement
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string TargetAudience { get; set; } = "ALL"; // ALL, DP, DPCM, BC, EC
    public string Category { get; set; } = "GENERAL"; // GENERAL, UPDATE, PROMOTION, ALERT, POLICY
    public bool IsPinned { get; set; }
    public int Priority { get; set; } // Higher = more important
    public DateTime? PublishAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsPublished { get; set; }
    public Guid CreatedById { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? CreatedBy { get; set; }
    public ICollection<NewsReadStatus> ReadStatuses { get; set; } = new List<NewsReadStatus>();
}

/// <summary>
/// Track which users have read which news
/// </summary>
public class NewsReadStatus
{
    public Guid Id { get; set; }
    public Guid NewsId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public NewsAnnouncement? News { get; set; }
    public User? User { get; set; }
}

/// <summary>
/// User notification (push, in-app)
/// </summary>
public class UserNotification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty; // DELIVERY_STATUS, PAYMENT, PROMOTION, SYSTEM, COMPLAINT, RATING
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ActionUrl { get; set; } // Deep link or URL
    public string? ActionType { get; set; } // OPEN_DELIVERY, OPEN_WALLET, OPEN_URL, etc.
    public Guid? ReferenceId { get; set; } // Related entity ID (delivery, payment, etc.)
    public string? ReferenceType { get; set; } // DELIVERY, PAYMENT, COMPLAINT, etc.
    public string Channel { get; set; } = "IN_APP"; // IN_APP, PUSH, SMS, EMAIL
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsSent { get; set; } // For push/SMS/email
    public DateTime? SentAt { get; set; }
    public string? SendError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}

/// <summary>
/// User notification preferences
/// </summary>
public class NotificationPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool EnablePushNotifications { get; set; } = true;
    public bool EnableSmsNotifications { get; set; } = true;
    public bool EnableEmailNotifications { get; set; } = true;
    public bool DeliveryStatusUpdates { get; set; } = true;
    public bool PaymentNotifications { get; set; } = true;
    public bool PromotionalMessages { get; set; } = false;
    public bool NewsAndAnnouncements { get; set; } = true;
    public bool RatingReminders { get; set; } = true;
    public bool ComplaintUpdates { get; set; } = true;
    public string? QuietHoursStart { get; set; } // "22:00"
    public string? QuietHoursEnd { get; set; } // "07:00"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}

/// <summary>
/// Push notification device registration
/// </summary>
public class PushDeviceRegistration
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // IOS, ANDROID, WEB
    public string? DeviceModel { get; set; }
    public string? AppVersion { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}

/// <summary>
/// Notification/Email/SMS template
/// </summary>
public class NotificationTemplate
{
    public Guid Id { get; set; }
    public string TemplateCode { get; set; } = string.Empty; // DELIVERY_CREATED, PAYMENT_RECEIVED, etc.
    public string Name { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty; // IN_APP, PUSH, SMS, EMAIL
    public string? Subject { get; set; } // For email
    public string TitleTemplate { get; set; } = string.Empty; // "Your delivery {DeliveryId} has been picked up"
    public string BodyTemplate { get; set; } = string.Empty; // With placeholders {DeliveryId}, {DPName}, etc.
    public string? DefaultImageUrl { get; set; }
    public string? ActionType { get; set; }
    public string? ActionUrlTemplate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Bulk notification campaign
/// </summary>
public class NotificationCampaign
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // PROMOTIONAL, ANNOUNCEMENT, REMINDER
    public string TargetAudience { get; set; } = "ALL"; // ALL, DP, DPCM, BC, EC
    public string? TargetCriteria { get; set; } // JSON filter criteria
    public string Channel { get; set; } = "PUSH"; // PUSH, SMS, EMAIL, ALL
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string Status { get; set; } = "DRAFT"; // DRAFT, SCHEDULED, SENDING, COMPLETED, CANCELLED
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int DeliveredCount { get; set; }
    public int FailedCount { get; set; }
    public int OpenedCount { get; set; }
    public int ClickedCount { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? CreatedBy { get; set; }
}
