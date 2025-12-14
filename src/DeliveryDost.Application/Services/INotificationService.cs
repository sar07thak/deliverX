using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs;
using DeliveryDost.Application.Common;

namespace DeliveryDost.Application.Services;

public interface INotificationService
{
    // ============================================
    // News/Announcements
    // ============================================
    Task<Result<NewsDto>> CreateNewsAsync(CreateNewsRequest request, Guid adminId);
    Task<Result<NewsDto>> UpdateNewsAsync(Guid newsId, UpdateNewsRequest request);
    Task<Result<bool>> DeleteNewsAsync(Guid newsId);
    Task<Result<bool>> PublishNewsAsync(Guid newsId);
    Task<Result<bool>> UnpublishNewsAsync(Guid newsId);
    Task<Result<NewsDto>> GetNewsAsync(Guid newsId, Guid? userId = null);
    Task<Result<List<NewsListDto>>> GetNewsListAsync(string? targetAudience = null, string? category = null, bool? isPublished = null, int page = 1, int pageSize = 20);
    Task<Result<List<NewsListDto>>> GetNewsForUserAsync(Guid userId, string userRole, int page = 1, int pageSize = 20);
    Task<Result<bool>> MarkNewsAsReadAsync(Guid newsId, Guid userId);

    // ============================================
    // User Notifications
    // ============================================
    Task<Result<NotificationDto>> SendNotificationAsync(SendNotificationRequest request);
    Task<Result<int>> SendBulkNotificationAsync(SendBulkNotificationRequest request);
    Task<Result<NotificationDto>> SendTemplatedNotificationAsync(SendTemplatedNotificationRequest request);
    Task<Result<NotificationListResponse>> GetUserNotificationsAsync(Guid userId, bool? isRead = null, int page = 1, int pageSize = 20);
    Task<Result<bool>> MarkNotificationAsReadAsync(Guid notificationId, Guid userId);
    Task<Result<int>> MarkAllNotificationsAsReadAsync(Guid userId);
    Task<Result<bool>> DeleteNotificationAsync(Guid notificationId, Guid userId);
    Task<Result<int>> GetUnreadCountAsync(Guid userId);

    // ============================================
    // Notification Preferences
    // ============================================
    Task<Result<NotificationPreferenceDto>> GetPreferencesAsync(Guid userId);
    Task<Result<NotificationPreferenceDto>> UpdatePreferencesAsync(Guid userId, UpdatePreferencesRequest request);

    // ============================================
    // Push Device Registration
    // ============================================
    Task<Result<DeviceRegistrationDto>> RegisterDeviceAsync(Guid userId, RegisterDeviceRequest request);
    Task<Result<bool>> DeactivateDeviceAsync(Guid userId, string deviceToken);
    Task<Result<List<DeviceRegistrationDto>>> GetUserDevicesAsync(Guid userId);

    // ============================================
    // Notification Templates
    // ============================================
    Task<Result<TemplateDto>> CreateTemplateAsync(CreateTemplateRequest request);
    Task<Result<TemplateDto>> UpdateTemplateAsync(Guid templateId, UpdateTemplateRequest request);
    Task<Result<bool>> DeleteTemplateAsync(Guid templateId);
    Task<Result<TemplateDto>> GetTemplateAsync(Guid templateId);
    Task<Result<List<TemplateDto>>> GetTemplatesAsync(string? channel = null);

    // ============================================
    // Notification Campaigns
    // ============================================
    Task<Result<CampaignDto>> CreateCampaignAsync(CreateCampaignRequest request, Guid adminId);
    Task<Result<CampaignDto>> UpdateCampaignAsync(Guid campaignId, UpdateCampaignRequest request);
    Task<Result<bool>> DeleteCampaignAsync(Guid campaignId);
    Task<Result<bool>> ScheduleCampaignAsync(Guid campaignId, DateTime scheduledAt);
    Task<Result<bool>> LaunchCampaignAsync(Guid campaignId);
    Task<Result<bool>> CancelCampaignAsync(Guid campaignId);
    Task<Result<CampaignDto>> GetCampaignAsync(Guid campaignId);
    Task<Result<List<CampaignListDto>>> GetCampaignsAsync(string? status = null, int page = 1, int pageSize = 20);

    // ============================================
    // Statistics & Analytics
    // ============================================
    Task<Result<NotificationStatsDto>> GetNotificationStatsAsync(DateTime? fromDate = null, DateTime? toDate = null);

    // ============================================
    // System Notifications (internal use)
    // ============================================
    Task SendDeliveryStatusNotificationAsync(Guid userId, Guid deliveryId, string status, string deliveryDetails);
    Task SendPaymentNotificationAsync(Guid userId, decimal amount, string paymentType, Guid? referenceId = null);
    Task SendComplaintUpdateNotificationAsync(Guid userId, Guid complaintId, string status, string message);
    Task SendRatingReminderNotificationAsync(Guid userId, Guid deliveryId);
}
