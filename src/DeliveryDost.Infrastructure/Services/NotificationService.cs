using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.Common;
using DeliveryDost.Application.DTOs;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ============================================
    // News/Announcements
    // ============================================

    public async Task<Result<NewsDto>> CreateNewsAsync(CreateNewsRequest request, Guid adminId)
    {
        try
        {
            var news = new NewsAnnouncement
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Content = request.Content,
                ImageUrl = request.ImageUrl,
                TargetAudience = request.TargetAudience,
                Category = request.Category,
                IsPinned = request.IsPinned,
                Priority = request.Priority,
                PublishAt = request.PublishImmediately ? null : request.PublishAt,
                ExpiresAt = request.ExpiresAt,
                IsPublished = request.PublishImmediately,
                CreatedById = adminId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.NewsAnnouncements.Add(news);
            await _context.SaveChangesAsync();

            var admin = await _context.Users.FindAsync(adminId);

            return Result<NewsDto>.Success(new NewsDto
            {
                Id = news.Id,
                Title = news.Title,
                Content = news.Content,
                ImageUrl = news.ImageUrl,
                TargetAudience = news.TargetAudience,
                Category = news.Category,
                IsPinned = news.IsPinned,
                Priority = news.Priority,
                PublishAt = news.PublishAt,
                ExpiresAt = news.ExpiresAt,
                IsPublished = news.IsPublished,
                ViewCount = news.ViewCount,
                CreatedByName = admin?.FullName ?? "Unknown",
                CreatedAt = news.CreatedAt,
                UpdatedAt = news.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating news");
            return Result<NewsDto>.Failure("Failed to create news");
        }
    }

    public async Task<Result<NewsDto>> UpdateNewsAsync(Guid newsId, UpdateNewsRequest request)
    {
        try
        {
            var news = await _context.NewsAnnouncements
                .Include(n => n.CreatedBy)
                .FirstOrDefaultAsync(n => n.Id == newsId);

            if (news == null)
                return Result<NewsDto>.Failure("News not found");

            if (request.Title != null) news.Title = request.Title;
            if (request.Content != null) news.Content = request.Content;
            if (request.ImageUrl != null) news.ImageUrl = request.ImageUrl;
            if (request.TargetAudience != null) news.TargetAudience = request.TargetAudience;
            if (request.Category != null) news.Category = request.Category;
            if (request.IsPinned.HasValue) news.IsPinned = request.IsPinned.Value;
            if (request.Priority.HasValue) news.Priority = request.Priority.Value;
            if (request.PublishAt.HasValue) news.PublishAt = request.PublishAt.Value;
            if (request.ExpiresAt.HasValue) news.ExpiresAt = request.ExpiresAt.Value;

            news.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result<NewsDto>.Success(new NewsDto
            {
                Id = news.Id,
                Title = news.Title,
                Content = news.Content,
                ImageUrl = news.ImageUrl,
                TargetAudience = news.TargetAudience,
                Category = news.Category,
                IsPinned = news.IsPinned,
                Priority = news.Priority,
                PublishAt = news.PublishAt,
                ExpiresAt = news.ExpiresAt,
                IsPublished = news.IsPublished,
                ViewCount = news.ViewCount,
                CreatedByName = news.CreatedBy?.FullName ?? "Unknown",
                CreatedAt = news.CreatedAt,
                UpdatedAt = news.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating news {NewsId}", newsId);
            return Result<NewsDto>.Failure("Failed to update news");
        }
    }

    public async Task<Result<bool>> DeleteNewsAsync(Guid newsId)
    {
        try
        {
            var news = await _context.NewsAnnouncements.FindAsync(newsId);
            if (news == null)
                return Result<bool>.Failure("News not found");

            _context.NewsAnnouncements.Remove(news);
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting news {NewsId}", newsId);
            return Result<bool>.Failure("Failed to delete news");
        }
    }

    public async Task<Result<bool>> PublishNewsAsync(Guid newsId)
    {
        try
        {
            var news = await _context.NewsAnnouncements.FindAsync(newsId);
            if (news == null)
                return Result<bool>.Failure("News not found");

            news.IsPublished = true;
            news.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing news {NewsId}", newsId);
            return Result<bool>.Failure("Failed to publish news");
        }
    }

    public async Task<Result<bool>> UnpublishNewsAsync(Guid newsId)
    {
        try
        {
            var news = await _context.NewsAnnouncements.FindAsync(newsId);
            if (news == null)
                return Result<bool>.Failure("News not found");

            news.IsPublished = false;
            news.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing news {NewsId}", newsId);
            return Result<bool>.Failure("Failed to unpublish news");
        }
    }

    public async Task<Result<NewsDto>> GetNewsAsync(Guid newsId, Guid? userId = null)
    {
        try
        {
            var news = await _context.NewsAnnouncements
                .Include(n => n.CreatedBy)
                .FirstOrDefaultAsync(n => n.Id == newsId);

            if (news == null)
                return Result<NewsDto>.Failure("News not found");

            // Increment view count
            news.ViewCount++;
            await _context.SaveChangesAsync();

            var isRead = false;
            if (userId.HasValue)
            {
                isRead = await _context.NewsReadStatuses
                    .AnyAsync(r => r.NewsId == newsId && r.UserId == userId.Value);
            }

            return Result<NewsDto>.Success(new NewsDto
            {
                Id = news.Id,
                Title = news.Title,
                Content = news.Content,
                ImageUrl = news.ImageUrl,
                TargetAudience = news.TargetAudience,
                Category = news.Category,
                IsPinned = news.IsPinned,
                Priority = news.Priority,
                PublishAt = news.PublishAt,
                ExpiresAt = news.ExpiresAt,
                IsPublished = news.IsPublished,
                ViewCount = news.ViewCount,
                CreatedByName = news.CreatedBy?.FullName ?? "Unknown",
                CreatedAt = news.CreatedAt,
                UpdatedAt = news.UpdatedAt,
                IsRead = isRead
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting news {NewsId}", newsId);
            return Result<NewsDto>.Failure("Failed to get news");
        }
    }

    public async Task<Result<List<NewsListDto>>> GetNewsListAsync(string? targetAudience = null, string? category = null, bool? isPublished = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.NewsAnnouncements.AsQueryable();

            if (!string.IsNullOrEmpty(targetAudience))
                query = query.Where(n => n.TargetAudience == targetAudience || n.TargetAudience == "ALL");

            if (!string.IsNullOrEmpty(category))
                query = query.Where(n => n.Category == category);

            if (isPublished.HasValue)
                query = query.Where(n => n.IsPublished == isPublished.Value);

            var news = await query
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.Priority)
                .ThenByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NewsListDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Category = n.Category,
                    TargetAudience = n.TargetAudience,
                    IsPinned = n.IsPinned,
                    Priority = n.Priority,
                    IsPublished = n.IsPublished,
                    ViewCount = n.ViewCount,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Result<List<NewsListDto>>.Success(news);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting news list");
            return Result<List<NewsListDto>>.Failure("Failed to get news list");
        }
    }

    public async Task<Result<List<NewsListDto>>> GetNewsForUserAsync(Guid userId, string userRole, int page = 1, int pageSize = 20)
    {
        try
        {
            var now = DateTime.UtcNow;

            var readNewsIds = await _context.NewsReadStatuses
                .Where(r => r.UserId == userId)
                .Select(r => r.NewsId)
                .ToListAsync();

            var news = await _context.NewsAnnouncements
                .Where(n => n.IsPublished)
                .Where(n => n.TargetAudience == "ALL" || n.TargetAudience == userRole)
                .Where(n => !n.PublishAt.HasValue || n.PublishAt <= now)
                .Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt > now)
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.Priority)
                .ThenByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NewsListDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Category = n.Category,
                    TargetAudience = n.TargetAudience,
                    IsPinned = n.IsPinned,
                    Priority = n.Priority,
                    IsPublished = n.IsPublished,
                    ViewCount = n.ViewCount,
                    CreatedAt = n.CreatedAt,
                    IsRead = readNewsIds.Contains(n.Id)
                })
                .ToListAsync();

            return Result<List<NewsListDto>>.Success(news);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting news for user {UserId}", userId);
            return Result<List<NewsListDto>>.Failure("Failed to get news for user");
        }
    }

    public async Task<Result<bool>> MarkNewsAsReadAsync(Guid newsId, Guid userId)
    {
        try
        {
            var exists = await _context.NewsReadStatuses
                .AnyAsync(r => r.NewsId == newsId && r.UserId == userId);

            if (!exists)
            {
                _context.NewsReadStatuses.Add(new NewsReadStatus
                {
                    Id = Guid.NewGuid(),
                    NewsId = newsId,
                    UserId = userId,
                    ReadAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking news as read {NewsId} for user {UserId}", newsId, userId);
            return Result<bool>.Failure("Failed to mark news as read");
        }
    }

    // ============================================
    // User Notifications
    // ============================================

    public async Task<Result<NotificationDto>> SendNotificationAsync(SendNotificationRequest request)
    {
        try
        {
            var notification = new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
                ImageUrl = request.ImageUrl,
                ActionUrl = request.ActionUrl,
                ActionType = request.ActionType,
                ReferenceId = request.ReferenceId,
                ReferenceType = request.ReferenceType,
                Channel = request.Channel,
                CreatedAt = DateTime.UtcNow
            };

            // For IN_APP, mark as sent immediately
            if (request.Channel == "IN_APP")
            {
                notification.IsSent = true;
                notification.SentAt = DateTime.UtcNow;
            }
            else
            {
                // For PUSH/SMS/EMAIL, would integrate with external providers
                // For now, mark as sent
                notification.IsSent = true;
                notification.SentAt = DateTime.UtcNow;
            }

            _context.UserNotifications.Add(notification);
            await _context.SaveChangesAsync();

            return Result<NotificationDto>.Success(MapToDto(notification));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", request.UserId);
            return Result<NotificationDto>.Failure("Failed to send notification");
        }
    }

    public async Task<Result<int>> SendBulkNotificationAsync(SendBulkNotificationRequest request)
    {
        try
        {
            var notifications = request.UserIds.Select(userId => new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
                ImageUrl = request.ImageUrl,
                ActionUrl = request.ActionUrl,
                ActionType = request.ActionType,
                Channel = request.Channel,
                IsSent = true,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.UserNotifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return Result<int>.Success(notifications.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk notification");
            return Result<int>.Failure("Failed to send bulk notification");
        }
    }

    public async Task<Result<NotificationDto>> SendTemplatedNotificationAsync(SendTemplatedNotificationRequest request)
    {
        try
        {
            var template = await _context.NotificationTemplates
                .FirstOrDefaultAsync(t => t.TemplateCode == request.TemplateCode && t.Channel == request.Channel && t.IsActive);

            if (template == null)
                return Result<NotificationDto>.Failure("Template not found");

            // Replace placeholders
            var title = ReplacePlaceholders(template.TitleTemplate, request.Placeholders);
            var body = ReplacePlaceholders(template.BodyTemplate, request.Placeholders);
            var actionUrl = template.ActionUrlTemplate != null
                ? ReplacePlaceholders(template.ActionUrlTemplate, request.Placeholders)
                : null;

            var notification = new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Type = request.TemplateCode,
                Title = title,
                Message = body,
                ImageUrl = template.DefaultImageUrl,
                ActionUrl = actionUrl,
                ActionType = template.ActionType,
                ReferenceId = request.ReferenceId,
                ReferenceType = request.ReferenceType,
                Channel = request.Channel,
                IsSent = true,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserNotifications.Add(notification);
            await _context.SaveChangesAsync();

            return Result<NotificationDto>.Success(MapToDto(notification));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending templated notification");
            return Result<NotificationDto>.Failure("Failed to send templated notification");
        }
    }

    public async Task<Result<NotificationListResponse>> GetUserNotificationsAsync(Guid userId, bool? isRead = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.UserNotifications
                .Where(n => n.UserId == userId);

            if (isRead.HasValue)
                query = query.Where(n => n.IsRead == isRead.Value);

            var totalCount = await query.CountAsync();
            var unreadCount = await _context.UserNotifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Result<NotificationListResponse>.Success(new NotificationListResponse
            {
                Notifications = notifications.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            return Result<NotificationListResponse>.Failure("Failed to get notifications");
        }
    }

    public async Task<Result<bool>> MarkNotificationAsReadAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _context.UserNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
                return Result<bool>.Failure("Notification not found");

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read {NotificationId}", notificationId);
            return Result<bool>.Failure("Failed to mark notification as read");
        }
    }

    public async Task<Result<int>> MarkAllNotificationsAsReadAsync(Guid userId)
    {
        try
        {
            var notifications = await _context.UserNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Result<int>.Success(notifications.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return Result<int>.Failure("Failed to mark notifications as read");
        }
    }

    public async Task<Result<bool>> DeleteNotificationAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _context.UserNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
                return Result<bool>.Failure("Notification not found");

            _context.UserNotifications.Remove(notification);
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
            return Result<bool>.Failure("Failed to delete notification");
        }
    }

    public async Task<Result<int>> GetUnreadCountAsync(Guid userId)
    {
        try
        {
            var count = await _context.UserNotifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            return Result<int>.Failure("Failed to get unread count");
        }
    }

    // ============================================
    // Notification Preferences
    // ============================================

    public async Task<Result<NotificationPreferenceDto>> GetPreferencesAsync(Guid userId)
    {
        try
        {
            var pref = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (pref == null)
            {
                // Create default preferences
                pref = new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.NotificationPreferences.Add(pref);
                await _context.SaveChangesAsync();
            }

            return Result<NotificationPreferenceDto>.Success(new NotificationPreferenceDto
            {
                EnablePushNotifications = pref.EnablePushNotifications,
                EnableSmsNotifications = pref.EnableSmsNotifications,
                EnableEmailNotifications = pref.EnableEmailNotifications,
                DeliveryStatusUpdates = pref.DeliveryStatusUpdates,
                PaymentNotifications = pref.PaymentNotifications,
                PromotionalMessages = pref.PromotionalMessages,
                NewsAndAnnouncements = pref.NewsAndAnnouncements,
                RatingReminders = pref.RatingReminders,
                ComplaintUpdates = pref.ComplaintUpdates,
                QuietHoursStart = pref.QuietHoursStart,
                QuietHoursEnd = pref.QuietHoursEnd
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preferences for user {UserId}", userId);
            return Result<NotificationPreferenceDto>.Failure("Failed to get preferences");
        }
    }

    public async Task<Result<NotificationPreferenceDto>> UpdatePreferencesAsync(Guid userId, UpdatePreferencesRequest request)
    {
        try
        {
            var pref = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (pref == null)
            {
                pref = new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.NotificationPreferences.Add(pref);
            }

            if (request.EnablePushNotifications.HasValue) pref.EnablePushNotifications = request.EnablePushNotifications.Value;
            if (request.EnableSmsNotifications.HasValue) pref.EnableSmsNotifications = request.EnableSmsNotifications.Value;
            if (request.EnableEmailNotifications.HasValue) pref.EnableEmailNotifications = request.EnableEmailNotifications.Value;
            if (request.DeliveryStatusUpdates.HasValue) pref.DeliveryStatusUpdates = request.DeliveryStatusUpdates.Value;
            if (request.PaymentNotifications.HasValue) pref.PaymentNotifications = request.PaymentNotifications.Value;
            if (request.PromotionalMessages.HasValue) pref.PromotionalMessages = request.PromotionalMessages.Value;
            if (request.NewsAndAnnouncements.HasValue) pref.NewsAndAnnouncements = request.NewsAndAnnouncements.Value;
            if (request.RatingReminders.HasValue) pref.RatingReminders = request.RatingReminders.Value;
            if (request.ComplaintUpdates.HasValue) pref.ComplaintUpdates = request.ComplaintUpdates.Value;
            if (request.QuietHoursStart != null) pref.QuietHoursStart = request.QuietHoursStart;
            if (request.QuietHoursEnd != null) pref.QuietHoursEnd = request.QuietHoursEnd;

            pref.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result<NotificationPreferenceDto>.Success(new NotificationPreferenceDto
            {
                EnablePushNotifications = pref.EnablePushNotifications,
                EnableSmsNotifications = pref.EnableSmsNotifications,
                EnableEmailNotifications = pref.EnableEmailNotifications,
                DeliveryStatusUpdates = pref.DeliveryStatusUpdates,
                PaymentNotifications = pref.PaymentNotifications,
                PromotionalMessages = pref.PromotionalMessages,
                NewsAndAnnouncements = pref.NewsAndAnnouncements,
                RatingReminders = pref.RatingReminders,
                ComplaintUpdates = pref.ComplaintUpdates,
                QuietHoursStart = pref.QuietHoursStart,
                QuietHoursEnd = pref.QuietHoursEnd
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences for user {UserId}", userId);
            return Result<NotificationPreferenceDto>.Failure("Failed to update preferences");
        }
    }

    // ============================================
    // Push Device Registration
    // ============================================

    public async Task<Result<DeviceRegistrationDto>> RegisterDeviceAsync(Guid userId, RegisterDeviceRequest request)
    {
        try
        {
            // Check if device token already exists
            var existing = await _context.PushDeviceRegistrations
                .FirstOrDefaultAsync(d => d.DeviceToken == request.DeviceToken);

            if (existing != null)
            {
                // Update existing registration
                existing.UserId = userId;
                existing.Platform = request.Platform;
                existing.DeviceModel = request.DeviceModel;
                existing.AppVersion = request.AppVersion;
                existing.IsActive = true;
                existing.LastUsedAt = DateTime.UtcNow;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                existing = new PushDeviceRegistration
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DeviceToken = request.DeviceToken,
                    Platform = request.Platform,
                    DeviceModel = request.DeviceModel,
                    AppVersion = request.AppVersion,
                    IsActive = true,
                    LastUsedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.PushDeviceRegistrations.Add(existing);
            }

            await _context.SaveChangesAsync();

            return Result<DeviceRegistrationDto>.Success(new DeviceRegistrationDto
            {
                Id = existing.Id,
                DeviceToken = existing.DeviceToken,
                Platform = existing.Platform,
                DeviceModel = existing.DeviceModel,
                AppVersion = existing.AppVersion,
                IsActive = existing.IsActive,
                LastUsedAt = existing.LastUsedAt,
                CreatedAt = existing.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device for user {UserId}", userId);
            return Result<DeviceRegistrationDto>.Failure("Failed to register device");
        }
    }

    public async Task<Result<bool>> DeactivateDeviceAsync(Guid userId, string deviceToken)
    {
        try
        {
            var device = await _context.PushDeviceRegistrations
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == deviceToken);

            if (device == null)
                return Result<bool>.Failure("Device not found");

            device.IsActive = false;
            device.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating device for user {UserId}", userId);
            return Result<bool>.Failure("Failed to deactivate device");
        }
    }

    public async Task<Result<List<DeviceRegistrationDto>>> GetUserDevicesAsync(Guid userId)
    {
        try
        {
            var devices = await _context.PushDeviceRegistrations
                .Where(d => d.UserId == userId && d.IsActive)
                .Select(d => new DeviceRegistrationDto
                {
                    Id = d.Id,
                    DeviceToken = d.DeviceToken,
                    Platform = d.Platform,
                    DeviceModel = d.DeviceModel,
                    AppVersion = d.AppVersion,
                    IsActive = d.IsActive,
                    LastUsedAt = d.LastUsedAt,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            return Result<List<DeviceRegistrationDto>>.Success(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices for user {UserId}", userId);
            return Result<List<DeviceRegistrationDto>>.Failure("Failed to get devices");
        }
    }

    // ============================================
    // Notification Templates
    // ============================================

    public async Task<Result<TemplateDto>> CreateTemplateAsync(CreateTemplateRequest request)
    {
        try
        {
            // Check for duplicate
            var exists = await _context.NotificationTemplates
                .AnyAsync(t => t.TemplateCode == request.TemplateCode && t.Channel == request.Channel);

            if (exists)
                return Result<TemplateDto>.Failure("Template with same code and channel already exists");

            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                TemplateCode = request.TemplateCode,
                Name = request.Name,
                Channel = request.Channel,
                Subject = request.Subject,
                TitleTemplate = request.TitleTemplate,
                BodyTemplate = request.BodyTemplate,
                DefaultImageUrl = request.DefaultImageUrl,
                ActionType = request.ActionType,
                ActionUrlTemplate = request.ActionUrlTemplate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.NotificationTemplates.Add(template);
            await _context.SaveChangesAsync();

            return Result<TemplateDto>.Success(MapTemplateToDto(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return Result<TemplateDto>.Failure("Failed to create template");
        }
    }

    public async Task<Result<TemplateDto>> UpdateTemplateAsync(Guid templateId, UpdateTemplateRequest request)
    {
        try
        {
            var template = await _context.NotificationTemplates.FindAsync(templateId);
            if (template == null)
                return Result<TemplateDto>.Failure("Template not found");

            if (request.Name != null) template.Name = request.Name;
            if (request.Subject != null) template.Subject = request.Subject;
            if (request.TitleTemplate != null) template.TitleTemplate = request.TitleTemplate;
            if (request.BodyTemplate != null) template.BodyTemplate = request.BodyTemplate;
            if (request.DefaultImageUrl != null) template.DefaultImageUrl = request.DefaultImageUrl;
            if (request.ActionType != null) template.ActionType = request.ActionType;
            if (request.ActionUrlTemplate != null) template.ActionUrlTemplate = request.ActionUrlTemplate;
            if (request.IsActive.HasValue) template.IsActive = request.IsActive.Value;

            template.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result<TemplateDto>.Success(MapTemplateToDto(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", templateId);
            return Result<TemplateDto>.Failure("Failed to update template");
        }
    }

    public async Task<Result<bool>> DeleteTemplateAsync(Guid templateId)
    {
        try
        {
            var template = await _context.NotificationTemplates.FindAsync(templateId);
            if (template == null)
                return Result<bool>.Failure("Template not found");

            _context.NotificationTemplates.Remove(template);
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", templateId);
            return Result<bool>.Failure("Failed to delete template");
        }
    }

    public async Task<Result<TemplateDto>> GetTemplateAsync(Guid templateId)
    {
        try
        {
            var template = await _context.NotificationTemplates.FindAsync(templateId);
            if (template == null)
                return Result<TemplateDto>.Failure("Template not found");

            return Result<TemplateDto>.Success(MapTemplateToDto(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template {TemplateId}", templateId);
            return Result<TemplateDto>.Failure("Failed to get template");
        }
    }

    public async Task<Result<List<TemplateDto>>> GetTemplatesAsync(string? channel = null)
    {
        try
        {
            var query = _context.NotificationTemplates.AsQueryable();

            if (!string.IsNullOrEmpty(channel))
                query = query.Where(t => t.Channel == channel);

            var templates = await query
                .OrderBy(t => t.TemplateCode)
                .Select(t => MapTemplateToDto(t))
                .ToListAsync();

            return Result<List<TemplateDto>>.Success(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates");
            return Result<List<TemplateDto>>.Failure("Failed to get templates");
        }
    }

    // ============================================
    // Notification Campaigns
    // ============================================

    public async Task<Result<CampaignDto>> CreateCampaignAsync(CreateCampaignRequest request, Guid adminId)
    {
        try
        {
            var campaign = new NotificationCampaign
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Type = request.Type,
                TargetAudience = request.TargetAudience,
                TargetCriteria = request.TargetCriteria,
                Channel = request.Channel,
                Title = request.Title,
                Message = request.Message,
                ImageUrl = request.ImageUrl,
                ActionUrl = request.ActionUrl,
                ScheduledAt = request.ScheduledAt,
                Status = request.ScheduledAt.HasValue ? "SCHEDULED" : "DRAFT",
                CreatedById = adminId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Calculate target recipients
            campaign.TotalRecipients = await CalculateCampaignRecipientsAsync(request.TargetAudience, request.TargetCriteria);

            _context.NotificationCampaigns.Add(campaign);
            await _context.SaveChangesAsync();

            var admin = await _context.Users.FindAsync(adminId);

            return Result<CampaignDto>.Success(new CampaignDto
            {
                Id = campaign.Id,
                Name = campaign.Name,
                Type = campaign.Type,
                TargetAudience = campaign.TargetAudience,
                TargetCriteria = campaign.TargetCriteria,
                Channel = campaign.Channel,
                Title = campaign.Title,
                Message = campaign.Message,
                ImageUrl = campaign.ImageUrl,
                ActionUrl = campaign.ActionUrl,
                ScheduledAt = campaign.ScheduledAt,
                Status = campaign.Status,
                TotalRecipients = campaign.TotalRecipients,
                CreatedByName = admin?.FullName ?? "Unknown",
                CreatedAt = campaign.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating campaign");
            return Result<CampaignDto>.Failure("Failed to create campaign");
        }
    }

    public async Task<Result<CampaignDto>> UpdateCampaignAsync(Guid campaignId, UpdateCampaignRequest request)
    {
        try
        {
            var campaign = await _context.NotificationCampaigns
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == campaignId);

            if (campaign == null)
                return Result<CampaignDto>.Failure("Campaign not found");

            if (campaign.Status != "DRAFT" && campaign.Status != "SCHEDULED")
                return Result<CampaignDto>.Failure("Cannot update campaign that has already started");

            if (request.Name != null) campaign.Name = request.Name;
            if (request.Type != null) campaign.Type = request.Type;
            if (request.TargetAudience != null) campaign.TargetAudience = request.TargetAudience;
            if (request.TargetCriteria != null) campaign.TargetCriteria = request.TargetCriteria;
            if (request.Channel != null) campaign.Channel = request.Channel;
            if (request.Title != null) campaign.Title = request.Title;
            if (request.Message != null) campaign.Message = request.Message;
            if (request.ImageUrl != null) campaign.ImageUrl = request.ImageUrl;
            if (request.ActionUrl != null) campaign.ActionUrl = request.ActionUrl;
            if (request.ScheduledAt.HasValue) campaign.ScheduledAt = request.ScheduledAt.Value;

            // Recalculate recipients if targeting changed
            if (request.TargetAudience != null || request.TargetCriteria != null)
            {
                campaign.TotalRecipients = await CalculateCampaignRecipientsAsync(campaign.TargetAudience, campaign.TargetCriteria);
            }

            campaign.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result<CampaignDto>.Success(MapCampaignToDto(campaign));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating campaign {CampaignId}", campaignId);
            return Result<CampaignDto>.Failure("Failed to update campaign");
        }
    }

    public async Task<Result<bool>> DeleteCampaignAsync(Guid campaignId)
    {
        try
        {
            var campaign = await _context.NotificationCampaigns.FindAsync(campaignId);
            if (campaign == null)
                return Result<bool>.Failure("Campaign not found");

            if (campaign.Status == "SENDING")
                return Result<bool>.Failure("Cannot delete campaign that is currently sending");

            _context.NotificationCampaigns.Remove(campaign);
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting campaign {CampaignId}", campaignId);
            return Result<bool>.Failure("Failed to delete campaign");
        }
    }

    public async Task<Result<bool>> ScheduleCampaignAsync(Guid campaignId, DateTime scheduledAt)
    {
        try
        {
            var campaign = await _context.NotificationCampaigns.FindAsync(campaignId);
            if (campaign == null)
                return Result<bool>.Failure("Campaign not found");

            if (campaign.Status != "DRAFT")
                return Result<bool>.Failure("Only draft campaigns can be scheduled");

            campaign.ScheduledAt = scheduledAt;
            campaign.Status = "SCHEDULED";
            campaign.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling campaign {CampaignId}", campaignId);
            return Result<bool>.Failure("Failed to schedule campaign");
        }
    }

    public async Task<Result<bool>> LaunchCampaignAsync(Guid campaignId)
    {
        try
        {
            var campaign = await _context.NotificationCampaigns.FindAsync(campaignId);
            if (campaign == null)
                return Result<bool>.Failure("Campaign not found");

            if (campaign.Status != "DRAFT" && campaign.Status != "SCHEDULED")
                return Result<bool>.Failure("Campaign cannot be launched");

            campaign.Status = "SENDING";
            campaign.StartedAt = DateTime.UtcNow;
            campaign.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Get target users and send notifications
            var targetUsers = await GetCampaignTargetUsersAsync(campaign.TargetAudience, campaign.TargetCriteria);

            foreach (var userId in targetUsers)
            {
                try
                {
                    var notification = new UserNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Type = campaign.Type,
                        Title = campaign.Title,
                        Message = campaign.Message,
                        ImageUrl = campaign.ImageUrl,
                        ActionUrl = campaign.ActionUrl,
                        Channel = campaign.Channel,
                        IsSent = true,
                        SentAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserNotifications.Add(notification);
                    campaign.SentCount++;
                    campaign.DeliveredCount++;
                }
                catch
                {
                    campaign.FailedCount++;
                }
            }

            campaign.Status = "COMPLETED";
            campaign.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error launching campaign {CampaignId}", campaignId);
            return Result<bool>.Failure("Failed to launch campaign");
        }
    }

    public async Task<Result<bool>> CancelCampaignAsync(Guid campaignId)
    {
        try
        {
            var campaign = await _context.NotificationCampaigns.FindAsync(campaignId);
            if (campaign == null)
                return Result<bool>.Failure("Campaign not found");

            if (campaign.Status == "COMPLETED")
                return Result<bool>.Failure("Cannot cancel completed campaign");

            campaign.Status = "CANCELLED";
            campaign.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling campaign {CampaignId}", campaignId);
            return Result<bool>.Failure("Failed to cancel campaign");
        }
    }

    public async Task<Result<CampaignDto>> GetCampaignAsync(Guid campaignId)
    {
        try
        {
            var campaign = await _context.NotificationCampaigns
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == campaignId);

            if (campaign == null)
                return Result<CampaignDto>.Failure("Campaign not found");

            return Result<CampaignDto>.Success(MapCampaignToDto(campaign));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting campaign {CampaignId}", campaignId);
            return Result<CampaignDto>.Failure("Failed to get campaign");
        }
    }

    public async Task<Result<List<CampaignListDto>>> GetCampaignsAsync(string? status = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.NotificationCampaigns.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(c => c.Status == status);

            var campaigns = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CampaignListDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Type = c.Type,
                    TargetAudience = c.TargetAudience,
                    Channel = c.Channel,
                    Status = c.Status,
                    TotalRecipients = c.TotalRecipients,
                    SentCount = c.SentCount,
                    ScheduledAt = c.ScheduledAt,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Result<List<CampaignListDto>>.Success(campaigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting campaigns");
            return Result<List<CampaignListDto>>.Failure("Failed to get campaigns");
        }
    }

    // ============================================
    // Statistics & Analytics
    // ============================================

    public async Task<Result<NotificationStatsDto>> GetNotificationStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var notifications = await _context.UserNotifications
                .Where(n => n.CreatedAt >= from && n.CreatedAt <= to)
                .ToListAsync();

            var totalSent = notifications.Count(n => n.IsSent);
            var totalFailed = notifications.Count(n => !n.IsSent && !string.IsNullOrEmpty(n.SendError));
            var totalRead = notifications.Count(n => n.IsRead);

            var stats = new NotificationStatsDto
            {
                TotalNotificationsSent = totalSent,
                TotalDelivered = totalSent - totalFailed,
                TotalFailed = totalFailed,
                TotalRead = totalRead,
                DeliveryRate = totalSent > 0 ? (decimal)(totalSent - totalFailed) / totalSent * 100 : 0,
                ReadRate = totalSent > 0 ? (decimal)totalRead / totalSent * 100 : 0,
                ActiveDevices = await _context.PushDeviceRegistrations.CountAsync(d => d.IsActive),
                PublishedNews = await _context.NewsAnnouncements.CountAsync(n => n.IsPublished),
                ActiveCampaigns = await _context.NotificationCampaigns.CountAsync(c => c.Status == "SENDING" || c.Status == "SCHEDULED"),
                ByChannel = notifications.GroupBy(n => n.Channel).ToDictionary(g => g.Key, g => g.Count()),
                ByType = notifications.GroupBy(n => n.Type).ToDictionary(g => g.Key, g => g.Count())
            };

            return Result<NotificationStatsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification stats");
            return Result<NotificationStatsDto>.Failure("Failed to get notification stats");
        }
    }

    // ============================================
    // System Notifications (internal use)
    // ============================================

    public async Task SendDeliveryStatusNotificationAsync(Guid userId, Guid deliveryId, string status, string deliveryDetails)
    {
        try
        {
            var pref = await _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
            if (pref != null && !pref.DeliveryStatusUpdates) return;

            await SendNotificationAsync(new SendNotificationRequest
            {
                UserId = userId,
                Type = "DELIVERY_STATUS",
                Title = $"Delivery {status}",
                Message = deliveryDetails,
                ActionType = "OPEN_DELIVERY",
                ReferenceId = deliveryId,
                ReferenceType = "DELIVERY",
                Channel = "IN_APP"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending delivery status notification");
        }
    }

    public async Task SendPaymentNotificationAsync(Guid userId, decimal amount, string paymentType, Guid? referenceId = null)
    {
        try
        {
            var pref = await _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
            if (pref != null && !pref.PaymentNotifications) return;

            await SendNotificationAsync(new SendNotificationRequest
            {
                UserId = userId,
                Type = "PAYMENT",
                Title = $"Payment {paymentType}",
                Message = $"Rs. {amount:N2} has been {paymentType.ToLower()}",
                ActionType = "OPEN_WALLET",
                ReferenceId = referenceId,
                ReferenceType = "PAYMENT",
                Channel = "IN_APP"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment notification");
        }
    }

    public async Task SendComplaintUpdateNotificationAsync(Guid userId, Guid complaintId, string status, string message)
    {
        try
        {
            var pref = await _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
            if (pref != null && !pref.ComplaintUpdates) return;

            await SendNotificationAsync(new SendNotificationRequest
            {
                UserId = userId,
                Type = "COMPLAINT",
                Title = $"Complaint {status}",
                Message = message,
                ActionType = "OPEN_COMPLAINT",
                ReferenceId = complaintId,
                ReferenceType = "COMPLAINT",
                Channel = "IN_APP"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending complaint update notification");
        }
    }

    public async Task SendRatingReminderNotificationAsync(Guid userId, Guid deliveryId)
    {
        try
        {
            var pref = await _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
            if (pref != null && !pref.RatingReminders) return;

            await SendNotificationAsync(new SendNotificationRequest
            {
                UserId = userId,
                Type = "RATING",
                Title = "Rate Your Delivery",
                Message = "How was your delivery experience? Tap to rate.",
                ActionType = "OPEN_RATING",
                ReferenceId = deliveryId,
                ReferenceType = "DELIVERY",
                Channel = "IN_APP"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending rating reminder notification");
        }
    }

    // ============================================
    // Helper Methods
    // ============================================

    private static NotificationDto MapToDto(UserNotification n)
    {
        return new NotificationDto
        {
            Id = n.Id,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            ImageUrl = n.ImageUrl,
            ActionUrl = n.ActionUrl,
            ActionType = n.ActionType,
            ReferenceId = n.ReferenceId,
            ReferenceType = n.ReferenceType,
            Channel = n.Channel,
            IsRead = n.IsRead,
            ReadAt = n.ReadAt,
            CreatedAt = n.CreatedAt
        };
    }

    private static TemplateDto MapTemplateToDto(NotificationTemplate t)
    {
        return new TemplateDto
        {
            Id = t.Id,
            TemplateCode = t.TemplateCode,
            Name = t.Name,
            Channel = t.Channel,
            Subject = t.Subject,
            TitleTemplate = t.TitleTemplate,
            BodyTemplate = t.BodyTemplate,
            DefaultImageUrl = t.DefaultImageUrl,
            ActionType = t.ActionType,
            ActionUrlTemplate = t.ActionUrlTemplate,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }

    private static CampaignDto MapCampaignToDto(NotificationCampaign c)
    {
        return new CampaignDto
        {
            Id = c.Id,
            Name = c.Name,
            Type = c.Type,
            TargetAudience = c.TargetAudience,
            TargetCriteria = c.TargetCriteria,
            Channel = c.Channel,
            Title = c.Title,
            Message = c.Message,
            ImageUrl = c.ImageUrl,
            ActionUrl = c.ActionUrl,
            ScheduledAt = c.ScheduledAt,
            Status = c.Status,
            TotalRecipients = c.TotalRecipients,
            SentCount = c.SentCount,
            DeliveredCount = c.DeliveredCount,
            FailedCount = c.FailedCount,
            OpenedCount = c.OpenedCount,
            ClickedCount = c.ClickedCount,
            CreatedByName = c.CreatedBy?.FullName ?? "Unknown",
            StartedAt = c.StartedAt,
            CompletedAt = c.CompletedAt,
            CreatedAt = c.CreatedAt
        };
    }

    private static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        var result = template;
        foreach (var kvp in placeholders)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value);
        }
        return result;
    }

    private async Task<int> CalculateCampaignRecipientsAsync(string targetAudience, string? targetCriteria)
    {
        var query = _context.Users.Where(u => u.IsActive);

        if (targetAudience != "ALL")
        {
            query = query.Where(u => u.Role == targetAudience);
        }

        // TODO: Parse and apply targetCriteria JSON filters

        return await query.CountAsync();
    }

    private async Task<List<Guid>> GetCampaignTargetUsersAsync(string targetAudience, string? targetCriteria)
    {
        var query = _context.Users.Where(u => u.IsActive);

        if (targetAudience != "ALL")
        {
            query = query.Where(u => u.Role == targetAudience);
        }

        // TODO: Parse and apply targetCriteria JSON filters

        return await query.Select(u => u.Id).ToListAsync();
    }
}
