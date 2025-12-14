using System;
using System.Collections.Generic;

namespace DeliveryDost.Application.DTOs;

// ============================================
// News/Announcement DTOs
// ============================================

public class CreateNewsRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string TargetAudience { get; set; } = "ALL"; // ALL, DP, DPCM, BC, EC
    public string Category { get; set; } = "GENERAL"; // GENERAL, UPDATE, PROMOTION, ALERT, POLICY
    public bool IsPinned { get; set; }
    public int Priority { get; set; }
    public DateTime? PublishAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool PublishImmediately { get; set; } = true;
}

public class UpdateNewsRequest
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public string? TargetAudience { get; set; }
    public string? Category { get; set; }
    public bool? IsPinned { get; set; }
    public int? Priority { get; set; }
    public DateTime? PublishAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class NewsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string TargetAudience { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public int Priority { get; set; }
    public DateTime? PublishAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsPublished { get; set; }
    public int ViewCount { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsRead { get; set; } // For current user
}

public class NewsListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public int Priority { get; set; }
    public bool IsPublished { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

// ============================================
// User Notification DTOs
// ============================================

public class SendNotificationRequest
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string Channel { get; set; } = "IN_APP";
}

public class SendBulkNotificationRequest
{
    public List<Guid> UserIds { get; set; } = new();
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionType { get; set; }
    public string Channel { get; set; } = "IN_APP";
}

public class SendTemplatedNotificationRequest
{
    public Guid UserId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string Channel { get; set; } = "IN_APP";
    public Dictionary<string, string> Placeholders { get; set; } = new();
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string Channel { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationListResponse
{
    public List<NotificationDto> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// ============================================
// Notification Preferences DTOs
// ============================================

public class NotificationPreferenceDto
{
    public bool EnablePushNotifications { get; set; }
    public bool EnableSmsNotifications { get; set; }
    public bool EnableEmailNotifications { get; set; }
    public bool DeliveryStatusUpdates { get; set; }
    public bool PaymentNotifications { get; set; }
    public bool PromotionalMessages { get; set; }
    public bool NewsAndAnnouncements { get; set; }
    public bool RatingReminders { get; set; }
    public bool ComplaintUpdates { get; set; }
    public string? QuietHoursStart { get; set; }
    public string? QuietHoursEnd { get; set; }
}

public class UpdatePreferencesRequest
{
    public bool? EnablePushNotifications { get; set; }
    public bool? EnableSmsNotifications { get; set; }
    public bool? EnableEmailNotifications { get; set; }
    public bool? DeliveryStatusUpdates { get; set; }
    public bool? PaymentNotifications { get; set; }
    public bool? PromotionalMessages { get; set; }
    public bool? NewsAndAnnouncements { get; set; }
    public bool? RatingReminders { get; set; }
    public bool? ComplaintUpdates { get; set; }
    public string? QuietHoursStart { get; set; }
    public string? QuietHoursEnd { get; set; }
}

// ============================================
// Push Device Registration DTOs
// ============================================

public class RegisterDeviceRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // IOS, ANDROID, WEB
    public string? DeviceModel { get; set; }
    public string? AppVersion { get; set; }
}

public class DeviceRegistrationDto
{
    public Guid Id { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string? DeviceModel { get; set; }
    public string? AppVersion { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============================================
// Notification Template DTOs
// ============================================

public class CreateTemplateRequest
{
    public string TemplateCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string TitleTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string? DefaultImageUrl { get; set; }
    public string? ActionType { get; set; }
    public string? ActionUrlTemplate { get; set; }
}

public class UpdateTemplateRequest
{
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? TitleTemplate { get; set; }
    public string? BodyTemplate { get; set; }
    public string? DefaultImageUrl { get; set; }
    public string? ActionType { get; set; }
    public string? ActionUrlTemplate { get; set; }
    public bool? IsActive { get; set; }
}

public class TemplateDto
{
    public Guid Id { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string TitleTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string? DefaultImageUrl { get; set; }
    public string? ActionType { get; set; }
    public string? ActionUrlTemplate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ============================================
// Notification Campaign DTOs
// ============================================

public class CreateCampaignRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // PROMOTIONAL, ANNOUNCEMENT, REMINDER
    public string TargetAudience { get; set; } = "ALL";
    public string? TargetCriteria { get; set; } // JSON filter
    public string Channel { get; set; } = "PUSH";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

public class UpdateCampaignRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? TargetAudience { get; set; }
    public string? TargetCriteria { get; set; }
    public string? Channel { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? ImageUrl { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

public class CampaignDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public string? TargetCriteria { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int DeliveredCount { get; set; }
    public int FailedCount { get; set; }
    public int OpenedCount { get; set; }
    public int ClickedCount { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CampaignListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============================================
// Dashboard Statistics DTOs
// ============================================

public class NotificationStatsDto
{
    public int TotalNotificationsSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalFailed { get; set; }
    public int TotalRead { get; set; }
    public decimal DeliveryRate { get; set; }
    public decimal ReadRate { get; set; }
    public int ActiveDevices { get; set; }
    public int PublishedNews { get; set; }
    public int ActiveCampaigns { get; set; }
    public Dictionary<string, int> ByChannel { get; set; } = new();
    public Dictionary<string, int> ByType { get; set; } = new();
}
