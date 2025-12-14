using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.DTOs.Dashboard;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken ct = default)
    {
        var platformStats = await GetPlatformStatsAsync(ct);
        var revenueStats = await GetRevenueStatsAsync(ct);

        // Get daily metrics for last 30 days
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).Date;
        var deliveries = await _context.Deliveries
            .Where(d => d.CreatedAt >= thirtyDaysAgo)
            .GroupBy(d => d.CreatedAt.Date)
            .Select(g => new DailyMetricDto
            {
                Date = g.Key,
                Deliveries = g.Count(),
                Revenue = g.Sum(d => d.FinalPrice ?? 0)
            })
            .OrderBy(m => m.Date)
            .ToListAsync(ct);

        // Get new users per day
        var newUsers = await _context.Users
            .Where(u => u.CreatedAt >= thirtyDaysAgo)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        foreach (var metric in deliveries)
        {
            metric.NewUsers = newUsers.FirstOrDefault(u => u.Date == metric.Date)?.Count ?? 0;
        }

        // Get top DPs by delivery count
        var dpDeliveryCounts = await _context.Deliveries
            .Where(d => d.AssignedDPId.HasValue && d.Status == "DELIVERED")
            .GroupBy(d => d.AssignedDPId!.Value)
            .Select(g => new { DPId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(ct);

        var dpIds = dpDeliveryCounts.Select(x => x.DPId).ToList();
        var dpProfiles = await _context.DeliveryPartnerProfiles
            .Include(dp => dp.User)
            .Where(dp => dpIds.Contains(dp.UserId))
            .ToListAsync(ct);

        var topDPs = dpDeliveryCounts.Select(dc =>
        {
            var profile = dpProfiles.FirstOrDefault(p => p.UserId == dc.DPId);
            return new TopPerformerDto
            {
                UserId = dc.DPId,
                Name = profile?.FullName ?? "Unknown",
                TotalDeliveries = dc.Count,
                Rating = 0, // Would come from BehaviorIndex
                TotalEarnings = 0
            };
        }).ToList();

        // Generate alerts
        var alerts = new List<AlertDto>();

        if (platformStats.OpenComplaints > 10)
        {
            alerts.Add(new AlertDto
            {
                Type = "WARNING",
                Category = "COMPLAINT",
                Message = $"{platformStats.OpenComplaints} open complaints need attention",
                Count = platformStats.OpenComplaints,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (platformStats.PendingKYC > 20)
        {
            alerts.Add(new AlertDto
            {
                Type = "INFO",
                Category = "KYC",
                Message = $"{platformStats.PendingKYC} KYC requests pending review",
                Count = platformStats.PendingKYC,
                CreatedAt = DateTime.UtcNow
            });
        }

        return new AdminDashboardDto
        {
            PlatformStats = platformStats,
            RevenueStats = revenueStats,
            DailyMetrics = deliveries,
            TopDPs = topDPs,
            Alerts = alerts
        };
    }

    public async Task<PlatformStatsDto> GetPlatformStatsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var totalUsers = await _context.Users.CountAsync(ct);
        var activeUsers = await _context.Users.CountAsync(u => u.IsActive, ct);

        var dpCount = await _context.Users.CountAsync(u => u.Role == "DP", ct);
        var activeDPs = await _context.DPAvailabilities.CountAsync(a => a.Status == "AVAILABLE", ct);

        var dpcmCount = await _context.Users.CountAsync(u => u.Role == "DPCM", ct);
        var bcCount = await _context.Users.CountAsync(u => u.Role == "BC", ct);
        var ecCount = await _context.Users.CountAsync(u => u.Role == "EC", ct);

        var totalDeliveries = await _context.Deliveries.CountAsync(ct);
        var deliveriesToday = await _context.Deliveries.CountAsync(d => d.CreatedAt >= today, ct);
        var deliveriesThisWeek = await _context.Deliveries.CountAsync(d => d.CreatedAt >= weekStart, ct);
        var deliveriesThisMonth = await _context.Deliveries.CountAsync(d => d.CreatedAt >= monthStart, ct);

        var pendingKYC = await _context.KYCRequests.CountAsync(k => k.Status == "PENDING" || k.Status == "IN_PROGRESS", ct);
        var openComplaints = await _context.Complaints.CountAsync(c => c.Status == "OPEN" || c.Status == "IN_PROGRESS", ct);

        var avgRating = await _context.Ratings.AverageAsync(r => (decimal?)r.Score, ct) ?? 0;

        return new PlatformStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalDPs = dpCount,
            ActiveDPs = activeDPs,
            TotalDPCMs = dpcmCount,
            TotalBCs = bcCount,
            TotalECs = ecCount,
            TotalDeliveries = totalDeliveries,
            DeliveriesToday = deliveriesToday,
            DeliveriesThisWeek = deliveriesThisWeek,
            DeliveriesThisMonth = deliveriesThisMonth,
            PendingKYC = pendingKYC,
            OpenComplaints = openComplaints,
            AvgRating = avgRating
        };
    }

    public async Task<RevenueStatsDto> GetRevenueStatsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var totalRevenue = await _context.Payments
            .Where(p => p.Status == "COMPLETED")
            .SumAsync(p => p.Amount, ct);

        var revenueToday = await _context.Payments
            .Where(p => p.Status == "COMPLETED" && p.CreatedAt >= today)
            .SumAsync(p => p.Amount, ct);

        var revenueThisWeek = await _context.Payments
            .Where(p => p.Status == "COMPLETED" && p.CreatedAt >= weekStart)
            .SumAsync(p => p.Amount, ct);

        var revenueThisMonth = await _context.Payments
            .Where(p => p.Status == "COMPLETED" && p.CreatedAt >= monthStart)
            .SumAsync(p => p.Amount, ct);

        var totalCommissions = await _context.CommissionRecords
            .SumAsync(c => c.PlatformFee + c.DPCMCommission, ct);

        var totalPlatformFees = await _context.CommissionRecords
            .SumAsync(c => c.PlatformFee, ct);

        var totalSettlements = await _context.Settlements
            .Where(s => s.Status == "COMPLETED")
            .SumAsync(s => s.NetAmount, ct);

        var pendingSettlements = await _context.Settlements
            .Where(s => s.Status == "PENDING")
            .SumAsync(s => s.NetAmount, ct);

        return new RevenueStatsDto
        {
            TotalRevenue = totalRevenue,
            RevenueToday = revenueToday,
            RevenueThisWeek = revenueThisWeek,
            RevenueThisMonth = revenueThisMonth,
            TotalCommissions = totalCommissions,
            TotalPlatformFees = totalPlatformFees,
            TotalSettlements = totalSettlements,
            PendingSettlements = pendingSettlements
        };
    }

    public async Task<DPCMDashboardDto> GetDPCMDashboardAsync(Guid dpcmId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        // First, get the DPCManager record for this user
        var dpcmManager = await _context.DPCManagers
            .FirstOrDefaultAsync(m => m.UserId == dpcmId, ct);

        var dpcmManagerId = dpcmManager?.Id ?? Guid.Empty;

        // Get all DPs managed by this DPCM (using DPCManager.Id, not User.Id)
        var managedDPs = await _context.DeliveryPartnerProfiles
            .Include(dp => dp.User)
            .Where(dp => dp.DPCMId == dpcmManagerId)
            .ToListAsync(ct);

        var dpIds = managedDPs.Select(dp => dp.UserId).ToList();

        var activeDPs = await _context.DPAvailabilities
            .Where(a => dpIds.Contains(a.DPId) && a.Status == "AVAILABLE")
            .CountAsync(ct);

        var totalDeliveries = await _context.Deliveries
            .Where(d => d.AssignedDPId.HasValue && dpIds.Contains(d.AssignedDPId.Value))
            .CountAsync(ct);

        var deliveriesToday = await _context.Deliveries
            .Where(d => d.AssignedDPId.HasValue && dpIds.Contains(d.AssignedDPId.Value) && d.CreatedAt >= today)
            .CountAsync(ct);

        var openComplaints = await _context.Complaints
            .Where(c => c.AgainstId.HasValue && dpIds.Contains(c.AgainstId.Value) &&
                       (c.Status == "OPEN" || c.Status == "IN_PROGRESS"))
            .CountAsync(ct);

        // Get behavior indices for average rating
        var behaviorIndices = await _context.BehaviorIndexes
            .Where(b => dpIds.Contains(b.UserId))
            .ToListAsync(ct);
        var avgRating = behaviorIndices.Count > 0 ? behaviorIndices.Average(b => b.AverageRating) : 0;

        var stats = new DPCMStatsDto
        {
            TotalManagedDPs = managedDPs.Count,
            ActiveDPs = activeDPs,
            InactiveDPs = managedDPs.Count - activeDPs,
            PendingOnboarding = managedDPs.Count(dp => !dp.IsActive),
            TotalDeliveries = totalDeliveries,
            DeliveriesToday = deliveriesToday,
            OpenComplaints = openComplaints,
            AvgDPRating = avgRating
        };

        // Get availability status for each DP
        var availabilities = await _context.DPAvailabilities
            .Where(a => dpIds.Contains(a.DPId))
            .ToDictionaryAsync(a => a.DPId, ct);

        var behaviorDict = behaviorIndices.ToDictionary(b => b.UserId);

        var dpSummaries = managedDPs.Select(dp => new DPSummaryDto
        {
            DPId = dp.UserId,
            Name = dp.FullName,
            Phone = dp.User?.Phone ?? "",
            Status = dp.IsActive ? "ACTIVE" : "INACTIVE",
            TotalDeliveries = 0, // Would need to count from deliveries
            Rating = behaviorDict.ContainsKey(dp.UserId) ? behaviorDict[dp.UserId].AverageRating : 0,
            BehaviorIndex = behaviorDict.ContainsKey(dp.UserId) ? behaviorDict[dp.UserId].BehaviorScore : 0,
            IsOnline = availabilities.ContainsKey(dp.UserId) && availabilities[dp.UserId].Status == "AVAILABLE",
            LastActive = availabilities.ContainsKey(dp.UserId) ? availabilities[dp.UserId].UpdatedAt : null
        }).ToList();

        // Get pending actions
        var pendingActions = new List<PendingActionDto>();

        var pendingComplaints = await _context.Complaints
            .Where(c => c.AgainstId.HasValue && dpIds.Contains(c.AgainstId.Value) && c.Status == "OPEN")
            .CountAsync(ct);

        if (pendingComplaints > 0)
        {
            pendingActions.Add(new PendingActionDto
            {
                ActionType = "REVIEW_COMPLAINT",
                Description = $"{pendingComplaints} complaints pending review",
                Priority = pendingComplaints > 5 ? "HIGH" : "NORMAL",
                DueDate = DateTime.UtcNow.AddDays(1)
            });
        }

        // Get DPCM earnings (DPCMCommissionConfigs uses DPCManager.Id)
        var commissionConfig = await _context.DPCMCommissionConfigs
            .Where(c => c.DPCMId == dpcmManagerId && (c.EffectiveTo == null || c.EffectiveTo > DateTime.UtcNow))
            .FirstOrDefaultAsync(ct);

        var totalEarnings = await _context.CommissionRecords
            .Where(c => c.DPCMId == dpcmId)
            .SumAsync(c => c.DPCMCommission, ct);

        var monthStart = new DateTime(today.Year, today.Month, 1);
        var earningsThisMonth = await _context.CommissionRecords
            .Where(c => c.DPCMId == dpcmId && c.CreatedAt >= monthStart)
            .SumAsync(c => c.DPCMCommission, ct);

        var pendingSettlement = await _context.Settlements
            .Where(s => s.BeneficiaryId == dpcmId && s.Status == "PENDING")
            .SumAsync(s => s.NetAmount, ct);

        var earnings = new EarningsStatsDto
        {
            TotalEarnings = totalEarnings,
            EarningsThisMonth = earningsThisMonth,
            PendingSettlement = pendingSettlement,
            CommissionRate = commissionConfig?.CommissionValue ?? 0
        };

        return new DPCMDashboardDto
        {
            Stats = stats,
            ManagedDPs = dpSummaries,
            PendingActions = pendingActions,
            Earnings = earnings
        };
    }

    public async Task<UserListResponse> GetUsersAsync(UserListRequest request, CancellationToken ct = default)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(request.Role))
        {
            query = query.Where(u => u.Role == request.Role);
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            var isActive = request.Status.ToUpper() == "ACTIVE";
            query = query.Where(u => u.IsActive == isActive);
        }

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(u =>
                (u.Phone != null && u.Phone.Contains(search)) ||
                (u.Email != null && u.Email.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(ct);

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "createdat" => request.SortDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            "role" => request.SortDesc ? query.OrderByDescending(u => u.Role) : query.OrderBy(u => u.Role),
            _ => query.OrderByDescending(u => u.CreatedAt)
        };

        var users = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                Name = u.Phone ?? u.Email ?? "Unknown",
                Phone = u.Phone ?? "",
                Email = u.Email,
                Role = u.Role,
                Status = u.IsActive ? "ACTIVE" : "INACTIVE",
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(ct);

        return new UserListResponse
        {
            Items = users,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }

    public async Task<UserListItemDto?> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null) return null;

        return new UserListItemDto
        {
            Id = user.Id,
            Name = user.Phone ?? user.Email ?? "Unknown",
            Phone = user.Phone ?? "",
            Email = user.Email,
            Role = user.Role,
            Status = user.IsActive ? "ACTIVE" : "INACTIVE",
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    public async Task<bool> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequest request, Guid adminId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null) return false;

        var oldStatus = user.IsActive ? "ACTIVE" : "INACTIVE";
        user.IsActive = request.Status.ToUpper() == "ACTIVE";
        user.UpdatedAt = DateTime.UtcNow;

        // Log the action
        var auditLog = new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = adminId,
            Action = "UPDATE_USER_STATUS",
            EntityType = "USER",
            EntityId = userId.ToString(),
            OldValue = oldStatus,
            NewValue = request.Status,
            CreatedAt = DateTime.UtcNow
        };
        _context.AdminAuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<KYCListResponse> GetKYCRequestsAsync(KYCListRequest request, CancellationToken ct = default)
    {
        var query = _context.KYCRequests
            .Include(k => k.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(k => k.Status == request.Status);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(k => k.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(k => new KYCListItemDto
            {
                Id = k.Id,
                UserId = k.UserId,
                UserName = k.User != null ? (k.User.Phone ?? k.User.Email ?? "Unknown") : "Unknown",
                Phone = k.User != null ? (k.User.Phone ?? "") : "",
                DocumentType = k.VerificationType,
                Status = k.Status,
                SubmittedAt = k.CreatedAt
            })
            .ToListAsync(ct);

        return new KYCListResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<bool> ApproveKYCAsync(Guid kycId, ApproveKYCRequest request, Guid adminId, CancellationToken ct = default)
    {
        var kyc = await _context.KYCRequests
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == kycId, ct);

        if (kyc == null) return false;

        kyc.Status = "VERIFIED";
        kyc.VerifiedBy = adminId;
        kyc.CompletedAt = DateTime.UtcNow;
        kyc.UpdatedAt = DateTime.UtcNow;

        if (kyc.User != null)
        {
            kyc.User.IsActive = true;
            kyc.User.UpdatedAt = DateTime.UtcNow;
        }

        var auditLog = new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = adminId,
            Action = "APPROVE_KYC",
            EntityType = "KYC",
            EntityId = kycId.ToString(),
            OldValue = "PENDING",
            NewValue = "VERIFIED",
            CreatedAt = DateTime.UtcNow
        };
        _context.AdminAuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RejectKYCAsync(Guid kycId, RejectKYCRequest request, Guid adminId, CancellationToken ct = default)
    {
        var kyc = await _context.KYCRequests
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == kycId, ct);

        if (kyc == null) return false;

        kyc.Status = "REJECTED";
        kyc.RejectionReason = request.Reason;
        kyc.VerifiedBy = adminId;
        kyc.CompletedAt = DateTime.UtcNow;
        kyc.UpdatedAt = DateTime.UtcNow;

        var auditLog = new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = adminId,
            Action = "REJECT_KYC",
            EntityType = "KYC",
            EntityId = kycId.ToString(),
            OldValue = "PENDING",
            NewValue = $"REJECTED: {request.Reason}",
            CreatedAt = DateTime.UtcNow
        };
        _context.AdminAuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<AuditLogResponse> GetAuditLogsAsync(AuditLogRequest request, CancellationToken ct = default)
    {
        var query = _context.AdminAuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrEmpty(request.Action))
        {
            query = query.Where(a => a.Action == request.Action);
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= request.EndDate.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogItemDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User != null ? (a.User.Phone ?? a.User.Email ?? "Unknown") : null,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(ct);

        return new AuditLogResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<SystemConfigDto> GetSystemConfigAsync(CancellationToken ct = default)
    {
        var configs = await _context.SystemConfigs
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Key)
            .Select(c => new ConfigItemDto
            {
                Key = c.Key,
                Value = c.Value,
                Category = c.Category,
                Description = c.Description,
                DataType = c.DataType,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(ct);

        return new SystemConfigDto { Items = configs };
    }

    public async Task<bool> UpdateSystemConfigAsync(UpdateConfigRequest request, Guid adminId, CancellationToken ct = default)
    {
        var config = await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.Key == request.Key, ct);

        if (config == null) return false;

        var oldValue = config.Value;
        config.Value = request.Value;
        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = adminId;

        var auditLog = new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = adminId,
            Action = "UPDATE_CONFIG",
            EntityType = "CONFIG",
            EntityId = request.Key,
            OldValue = oldValue,
            NewValue = request.Value,
            CreatedAt = DateTime.UtcNow
        };
        _context.AdminAuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ReportResponse> GenerateReportAsync(ReportRequest request, CancellationToken ct = default)
    {
        var response = new ReportResponse
        {
            ReportType = request.ReportType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            GeneratedAt = DateTime.UtcNow.ToString("o")
        };

        switch (request.ReportType.ToUpper())
        {
            case "DELIVERIES":
                response.Summary = new
                {
                    TotalDeliveries = await _context.Deliveries
                        .Where(d => d.CreatedAt >= request.StartDate && d.CreatedAt <= request.EndDate)
                        .CountAsync(ct),
                    CompletedDeliveries = await _context.Deliveries
                        .Where(d => d.CreatedAt >= request.StartDate && d.CreatedAt <= request.EndDate && d.Status == "DELIVERED")
                        .CountAsync(ct),
                    CancelledDeliveries = await _context.Deliveries
                        .Where(d => d.CreatedAt >= request.StartDate && d.CreatedAt <= request.EndDate && d.Status == "CANCELLED")
                        .CountAsync(ct)
                };

                response.Data = await _context.Deliveries
                    .Where(d => d.CreatedAt >= request.StartDate && d.CreatedAt <= request.EndDate)
                    .GroupBy(d => d.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count(), Revenue = g.Sum(d => d.FinalPrice ?? 0) } as object)
                    .ToListAsync(ct);
                break;

            case "REVENUE":
                var totalPayments = await _context.Payments
                    .Where(p => p.CreatedAt >= request.StartDate && p.CreatedAt <= request.EndDate && p.Status == "COMPLETED")
                    .SumAsync(p => p.Amount, ct);

                response.Summary = new
                {
                    TotalRevenue = totalPayments,
                    TotalCommissions = await _context.CommissionRecords
                        .Where(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate)
                        .SumAsync(c => c.PlatformFee, ct)
                };

                response.Data = await _context.Payments
                    .Where(p => p.CreatedAt >= request.StartDate && p.CreatedAt <= request.EndDate && p.Status == "COMPLETED")
                    .GroupBy(p => p.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Amount = g.Sum(p => p.Amount) } as object)
                    .ToListAsync(ct);
                break;

            case "USERS":
                response.Summary = new
                {
                    NewUsers = await _context.Users
                        .Where(u => u.CreatedAt >= request.StartDate && u.CreatedAt <= request.EndDate)
                        .CountAsync(ct),
                    ByRole = await _context.Users
                        .Where(u => u.CreatedAt >= request.StartDate && u.CreatedAt <= request.EndDate)
                        .GroupBy(u => u.Role)
                        .Select(g => new { Role = g.Key, Count = g.Count() })
                        .ToListAsync(ct)
                };

                response.Data = await _context.Users
                    .Where(u => u.CreatedAt >= request.StartDate && u.CreatedAt <= request.EndDate)
                    .GroupBy(u => u.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() } as object)
                    .ToListAsync(ct);
                break;

            case "COMPLAINTS":
                response.Summary = new
                {
                    TotalComplaints = await _context.Complaints
                        .Where(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate)
                        .CountAsync(ct),
                    ResolvedComplaints = await _context.Complaints
                        .Where(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate && c.Status == "RESOLVED")
                        .CountAsync(ct),
                    AvgResolutionTime = "N/A"
                };

                response.Data = await _context.Complaints
                    .Where(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate)
                    .GroupBy(c => c.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() } as object)
                    .ToListAsync(ct);
                break;
        }

        return response;
    }

    // DPCM Partner Management
    public async Task<DPCMPartnersResponse> GetDPCMPartnersAsync(Guid dpcmId, DPCMPartnersRequest request, CancellationToken ct = default)
    {
        // First, get the DPCManager record for this user
        var dpcmManager = await _context.DPCManagers
            .FirstOrDefaultAsync(m => m.UserId == dpcmId, ct);

        var dpcmManagerId = dpcmManager?.Id ?? Guid.Empty;

        var query = _context.DeliveryPartnerProfiles
            .Include(dp => dp.User)
            .Where(dp => dp.DPCMId == dpcmManagerId);

        // Apply status filter
        if (!string.IsNullOrEmpty(request.Status) && request.Status != "all")
        {
            query = request.Status switch
            {
                "active" => query.Where(dp => dp.IsActive),
                "inactive" => query.Where(dp => !dp.IsActive),
                "pending-kyc" => query.Where(dp => !dp.IsActive), // Pending KYC = not activated yet
                _ => query
            };
        }

        var totalCount = await query.CountAsync(ct);

        var dpProfiles = await query
            .OrderByDescending(dp => dp.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dpIds = dpProfiles.Select(dp => dp.UserId).ToList();

        // Get delivery counts
        var deliveryCounts = await _context.Deliveries
            .Where(d => d.AssignedDPId.HasValue && dpIds.Contains(d.AssignedDPId.Value) && d.Status == "DELIVERED")
            .GroupBy(d => d.AssignedDPId!.Value)
            .Select(g => new { DPId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DPId, x => x.Count, ct);

        // Get earnings (using wallet balance as earnings indicator)
        var earnings = await _context.Wallets
            .Where(w => dpIds.Contains(w.UserId))
            .ToDictionaryAsync(w => w.UserId, w => w.Balance, ct);

        // Get availability status
        var availabilities = await _context.DPAvailabilities
            .Where(a => dpIds.Contains(a.DPId))
            .ToDictionaryAsync(a => a.DPId, ct);

        // Get ratings
        var ratings = await _context.BehaviorIndexes
            .Where(b => dpIds.Contains(b.UserId))
            .ToDictionaryAsync(b => b.UserId, b => b.AverageRating, ct);

        // Get KYC status
        var kycStatuses = await _context.KYCRequests
            .Where(k => dpIds.Contains(k.UserId))
            .GroupBy(k => k.UserId)
            .Select(g => new { UserId = g.Key, Status = g.OrderByDescending(k => k.CreatedAt).First().Status })
            .ToDictionaryAsync(x => x.UserId, x => x.Status, ct);

        var items = dpProfiles.Select(dp => new DPCMPartnerDto
        {
            Id = dp.UserId,
            Name = dp.FullName,
            Phone = dp.User?.Phone ?? "",
            Status = dp.IsActive ? "ACTIVE" : "INACTIVE",
            KYCStatus = kycStatuses.ContainsKey(dp.UserId) ? kycStatuses[dp.UserId] : "NOT_SUBMITTED",
            TotalDeliveries = deliveryCounts.ContainsKey(dp.UserId) ? deliveryCounts[dp.UserId] : 0,
            Rating = ratings.ContainsKey(dp.UserId) ? ratings[dp.UserId] : 0,
            Earnings = earnings.ContainsKey(dp.UserId) ? earnings[dp.UserId] : 0,
            IsOnline = availabilities.ContainsKey(dp.UserId) && availabilities[dp.UserId].Status == "AVAILABLE",
            CreatedAt = dp.CreatedAt,
            LastActive = availabilities.ContainsKey(dp.UserId) ? availabilities[dp.UserId].UpdatedAt : null
        }).ToList();

        return new DPCMPartnersResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<bool> UpdateDPStatusByDPCMAsync(Guid dpcmId, Guid dpId, bool isActive, CancellationToken ct = default)
    {
        // First, get the DPCManager record for this user
        var dpcmManager = await _context.DPCManagers
            .FirstOrDefaultAsync(m => m.UserId == dpcmId, ct);

        var dpcmManagerId = dpcmManager?.Id ?? Guid.Empty;

        var dp = await _context.DeliveryPartnerProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == dpId && dp.DPCMId == dpcmManagerId, ct);

        if (dp == null) return false;

        dp.IsActive = isActive;
        dp.UpdatedAt = DateTime.UtcNow;
        if (isActive && !dp.ActivatedAt.HasValue)
        {
            dp.ActivatedAt = DateTime.UtcNow;
        }

        // Also update the user status
        var user = await _context.Users.FindAsync(new object[] { dpId }, ct);
        if (user != null)
        {
            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<DPCMDeliveriesResponse> GetDPCMDeliveriesAsync(Guid dpcmId, DPCMDeliveriesRequest request, CancellationToken ct = default)
    {
        // First, get the DPCManager record for this user
        var dpcmManager = await _context.DPCManagers
            .FirstOrDefaultAsync(m => m.UserId == dpcmId, ct);

        var dpcmManagerId = dpcmManager?.Id ?? Guid.Empty;

        // Get all DPs managed by this DPCM
        var dpIds = await _context.DeliveryPartnerProfiles
            .Where(dp => dp.DPCMId == dpcmManagerId)
            .Select(dp => dp.UserId)
            .ToListAsync(ct);

        var query = _context.Deliveries
            .Where(d => d.AssignedDPId.HasValue && dpIds.Contains(d.AssignedDPId.Value));

        // Apply status filter
        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(d => d.Status == request.Status);
        }

        // Apply DP filter
        if (request.DPId.HasValue)
        {
            query = query.Where(d => d.AssignedDPId == request.DPId);
        }

        var totalCount = await query.CountAsync(ct);

        var deliveries = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        // Get DP names
        var deliveryDpIds = deliveries.Where(d => d.AssignedDPId.HasValue).Select(d => d.AssignedDPId!.Value).Distinct().ToList();
        var dpNames = await _context.DeliveryPartnerProfiles
            .Where(dp => deliveryDpIds.Contains(dp.UserId))
            .ToDictionaryAsync(dp => dp.UserId, dp => dp.FullName, ct);

        // Get commission amounts
        var deliveryIds = deliveries.Select(d => d.Id).ToList();
        var commissions = await _context.CommissionRecords
            .Where(c => deliveryIds.Contains(c.DeliveryId))
            .ToDictionaryAsync(c => c.DeliveryId, c => c.DPCMCommission, ct);

        var items = deliveries.Select(d => new DPCMDeliveryDto
        {
            Id = d.Id,
            TrackingId = "DLV-" + d.Id.ToString()[..8].ToUpper(),
            DPName = d.AssignedDPId.HasValue && dpNames.ContainsKey(d.AssignedDPId.Value)
                ? dpNames[d.AssignedDPId.Value]
                : "Unassigned",
            DPId = d.AssignedDPId,
            Status = d.Status,
            PickupAddress = d.PickupAddress ?? "",
            DropAddress = d.DropAddress ?? "",
            Amount = d.FinalPrice ?? d.EstimatedPrice ?? 0,
            Commission = commissions.ContainsKey(d.Id) ? commissions[d.Id] : 0,
            CreatedAt = d.CreatedAt,
            DeliveredAt = d.Status == "DELIVERED" ? d.UpdatedAt : null
        }).ToList();

        return new DPCMDeliveriesResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<DPCMCommissionConfigDto> GetDPCMCommissionConfigAsync(Guid dpcmId, CancellationToken ct = default)
    {
        var config = await _context.DPCMCommissionConfigs
            .Where(c => c.DPCMId == dpcmId && (c.EffectiveTo == null || c.EffectiveTo > DateTime.UtcNow))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (config == null)
        {
            return new DPCMCommissionConfigDto
            {
                CommissionType = "PERCENTAGE",
                CommissionValue = 10,
                MinCommission = 5,
                MaxCommission = 100,
                EffectiveFrom = DateTime.UtcNow
            };
        }

        return new DPCMCommissionConfigDto
        {
            CommissionType = config.CommissionType,
            CommissionValue = config.CommissionValue,
            MinCommission = config.MinCommissionAmount,
            MaxCommission = config.MaxCommissionAmount ?? 1000,
            EffectiveFrom = config.EffectiveFrom
        };
    }

    public async Task<bool> UpdateDPCMCommissionConfigAsync(Guid dpcmId, UpdateCommissionConfigRequest request, CancellationToken ct = default)
    {
        // Close existing config
        var existingConfig = await _context.DPCMCommissionConfigs
            .Where(c => c.DPCMId == dpcmId && c.EffectiveTo == null)
            .FirstOrDefaultAsync(ct);

        if (existingConfig != null)
        {
            existingConfig.EffectiveTo = DateTime.UtcNow;
        }

        // Create new config
        var newConfig = new DPCMCommissionConfig
        {
            Id = Guid.NewGuid(),
            DPCMId = dpcmId,
            CommissionType = request.CommissionType,
            CommissionValue = request.CommissionValue,
            MinCommissionAmount = request.MinCommission,
            MaxCommissionAmount = request.MaxCommission,
            EffectiveFrom = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.DPCMCommissionConfigs.Add(newConfig);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<DPCMSettlementsResponse> GetDPCMSettlementsAsync(Guid dpcmId, DPCMSettlementsRequest request, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        // Get wallet for available balance
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == dpcmId, ct);

        // Get bank account (masked) - check if verified via VerifiedAt
        var bankVerification = await _context.BankVerifications
            .Where(b => b.UserId == dpcmId && b.VerifiedAt != null)
            .FirstOrDefaultAsync(ct);

        // Bank account number is encrypted, so just show placeholder if verified
        var maskedAccount = bankVerification != null ? "XXXX XXXX ****" : null;

        // Calculate total settled this month
        var settledThisMonth = await _context.Settlements
            .Where(s => s.BeneficiaryId == dpcmId && s.Status == "COMPLETED" && s.ProcessedAt >= monthStart)
            .SumAsync(s => s.NetAmount, ct);

        var summary = new DPCMSettlementSummaryDto
        {
            AvailableBalance = wallet?.Balance ?? 0,
            TotalSettledThisMonth = settledThisMonth,
            BankAccount = maskedAccount
        };

        // Get settlement history
        var query = _context.Settlements.Where(s => s.BeneficiaryId == dpcmId);

        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(s => s.Status == request.Status);
        }

        var totalCount = await query.CountAsync(ct);

        var settlements = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new DPCMSettlementDto
            {
                Id = s.Id,
                Amount = s.NetAmount,
                Status = s.Status,
                ReferenceId = s.SettlementNumber,
                CreatedAt = s.CreatedAt,
                CompletedAt = s.ProcessedAt
            })
            .ToListAsync(ct);

        return new DPCMSettlementsResponse
        {
            Summary = summary,
            Items = settlements,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<bool> RequestDPCMSettlementAsync(Guid dpcmId, decimal amount, CancellationToken ct = default)
    {
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == dpcmId, ct);
        if (wallet == null || wallet.Balance < amount || amount < 100)
        {
            return false;
        }

        // Create settlement request
        var settlement = new Settlement
        {
            Id = Guid.NewGuid(),
            SettlementNumber = $"STL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            BeneficiaryId = dpcmId,
            BeneficiaryType = "DPCM",
            GrossAmount = amount,
            TdsAmount = 0,
            NetAmount = amount,
            Status = "PENDING",
            SettlementDate = DateTime.UtcNow.Date,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Deduct from balance
        wallet.Balance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        _context.Settlements.Add(settlement);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    // ============================================
    // Drill-Down Analytics Methods
    // ============================================

    public async Task<GeographicAnalyticsDto> GetGeographicAnalyticsAsync(AnalyticsDrillDownRequest request, CancellationToken ct = default)
    {
        var result = new GeographicAnalyticsDto();

        // Get deliveries within date range
        var deliveries = await _context.Deliveries
            .Where(d => d.CreatedAt >= request.StartDate && d.CreatedAt <= request.EndDate)
            .ToListAsync(ct);

        // Group by service area
        var serviceAreas = await _context.ServiceAreas.ToListAsync(ct);
        var dpAvailabilities = await _context.DPAvailabilities
            .Where(a => a.Status == "AVAILABLE")
            .ToListAsync(ct);

        result.ByServiceArea = serviceAreas.Select(sa =>
        {
            // Estimate deliveries in area based on pickup location proximity
            var areaDeliveries = deliveries.Where(d =>
                Math.Abs((double)(d.PickupLat - sa.CenterLat)) < 0.1 &&
                Math.Abs((double)(d.PickupLng - sa.CenterLng)) < 0.1).ToList();
            // Count DPs whose last location is near the service area center
            var activeDPs = dpAvailabilities.Count(a =>
                a.LastLocationLat.HasValue && a.LastLocationLng.HasValue &&
                Math.Abs((double)(a.LastLocationLat.Value - sa.CenterLat)) < (double)sa.RadiusKm / 111 &&
                Math.Abs((double)(a.LastLocationLng.Value - sa.CenterLng)) < (double)sa.RadiusKm / 111);

            return new ServiceAreaAnalyticsDto
            {
                ServiceAreaId = sa.Id,
                ServiceAreaName = sa.AreaName ?? $"Area-{sa.Id.ToString()[..8]}",
                City = "",
                TotalDeliveries = areaDeliveries.Count,
                Revenue = areaDeliveries.Sum(d => d.FinalPrice ?? 0),
                ActiveDPs = activeDPs,
                DemandLevel = areaDeliveries.Count > 100 ? 5 : areaDeliveries.Count > 50 ? 4 : areaDeliveries.Count > 20 ? 3 : areaDeliveries.Count > 5 ? 2 : 1,
                SupplyLevel = activeDPs > 20 ? 5 : activeDPs > 10 ? 4 : activeDPs > 5 ? 3 : activeDPs > 2 ? 2 : 1,
                AvgDeliveryTime = 0,
                AvgWaitTime = 0
            };
        }).ToList();

        // Identify hotspots (top pickup areas)
        var hotspots = deliveries
            .GroupBy(d => new { Lat = Math.Round((double)d.PickupLat, 2), Lng = Math.Round((double)d.PickupLng, 2) })
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new HotspotDto
            {
                AreaName = $"Lat: {g.Key.Lat}, Lng: {g.Key.Lng}",
                Latitude = (decimal)g.Key.Lat,
                Longitude = (decimal)g.Key.Lng,
                DeliveryCount = g.Count(),
                DemandIntensity = g.Count() / Math.Max(1, (decimal)(request.EndDate - request.StartDate).TotalHours)
            })
            .ToList();

        result.Hotspots = hotspots;

        // Identify coverage gaps (areas with high demand but low supply)
        result.CoverageGaps = result.ByServiceArea
            .Where(sa => sa.DemandLevel > sa.SupplyLevel + 1)
            .Select(sa => new CoverageGapDto
            {
                AreaName = sa.ServiceAreaName,
                District = sa.City,
                RequestCount = sa.TotalDeliveries,
                AvailableDPs = sa.ActiveDPs,
                SuggestedAction = $"Recruit {sa.DemandLevel - sa.SupplyLevel} more DPs"
            })
            .ToList();

        return result;
    }

    public async Task<DeliveryPerformanceAnalyticsDto> GetDeliveryPerformanceAnalyticsAsync(AnalyticsDrillDownRequest request, CancellationToken ct = default)
    {
        var deliveries = await _context.Deliveries
            .Where(d => d.CreatedAt >= request.StartDate && d.CreatedAt <= request.EndDate)
            .ToListAsync(ct);

        var result = new DeliveryPerformanceAnalyticsDto();

        // Summary
        var completed = deliveries.Count(d => d.Status == "DELIVERED");
        var cancelled = deliveries.Count(d => d.Status == "CANCELLED");
        var inProgress = deliveries.Count(d => d.Status == "IN_TRANSIT" || d.Status == "PICKED_UP");

        var deliveriesWithDistance = deliveries.Where(d => d.DistanceKm.HasValue).ToList();
        result.Summary = new PerformanceSummaryDto
        {
            TotalDeliveries = deliveries.Count,
            Completed = completed,
            Cancelled = cancelled,
            InProgress = inProgress,
            CompletionRate = deliveries.Count > 0 ? (decimal)completed / deliveries.Count * 100 : 0,
            CancellationRate = deliveries.Count > 0 ? (decimal)cancelled / deliveries.Count * 100 : 0,
            OnTimeRate = 85, // Placeholder - would calculate based on actual SLA
            AvgDistanceKm = deliveriesWithDistance.Count > 0 ? deliveriesWithDistance.Average(d => d.DistanceKm!.Value) : 0,
            AvgOrderValue = deliveries.Count > 0 ? deliveries.Average(d => d.FinalPrice ?? d.EstimatedPrice ?? 0) : 0
        };

        // By time slot
        result.ByTimeSlot = new List<TimeSlotPerformanceDto>
        {
            new() { TimeSlot = "00:00-06:00", TotalDeliveries = deliveries.Count(d => d.CreatedAt.Hour >= 0 && d.CreatedAt.Hour < 6) },
            new() { TimeSlot = "06:00-12:00", TotalDeliveries = deliveries.Count(d => d.CreatedAt.Hour >= 6 && d.CreatedAt.Hour < 12) },
            new() { TimeSlot = "12:00-18:00", TotalDeliveries = deliveries.Count(d => d.CreatedAt.Hour >= 12 && d.CreatedAt.Hour < 18) },
            new() { TimeSlot = "18:00-24:00", TotalDeliveries = deliveries.Count(d => d.CreatedAt.Hour >= 18 && d.CreatedAt.Hour < 24) }
        };

        // By distance range
        result.ByDistanceRange = new List<DistanceRangePerformanceDto>
        {
            new() { DistanceRange = "0-2km", TotalDeliveries = deliveries.Count(d => d.DistanceKm <= 2), AvgPrice = deliveries.Where(d => d.DistanceKm <= 2).Select(d => d.FinalPrice ?? d.EstimatedPrice ?? 0).DefaultIfEmpty(0).Average() },
            new() { DistanceRange = "2-5km", TotalDeliveries = deliveries.Count(d => d.DistanceKm > 2 && d.DistanceKm <= 5) },
            new() { DistanceRange = "5-10km", TotalDeliveries = deliveries.Count(d => d.DistanceKm > 5 && d.DistanceKm <= 10) },
            new() { DistanceRange = "10-15km", TotalDeliveries = deliveries.Count(d => d.DistanceKm > 10 && d.DistanceKm <= 15) },
            new() { DistanceRange = ">15km", TotalDeliveries = deliveries.Count(d => d.DistanceKm > 15) }
        };

        // By day of week
        result.ByDayOfWeek = Enumerable.Range(0, 7)
            .Select(day => new DayOfWeekPerformanceDto
            {
                DayOfWeek = ((DayOfWeek)day).ToString(),
                DayIndex = day,
                TotalDeliveries = deliveries.Count(d => (int)d.CreatedAt.DayOfWeek == day),
                Revenue = deliveries.Where(d => (int)d.CreatedAt.DayOfWeek == day).Sum(d => d.FinalPrice ?? 0)
            })
            .ToList();

        // Funnel
        var ordersReceived = deliveries.Count;
        var dpMatched = deliveries.Count(d => d.AssignedDPId.HasValue);
        var pickedUp = deliveries.Count(d => d.Status == "PICKED_UP" || d.Status == "IN_TRANSIT" || d.Status == "DELIVERED");
        var delivered = completed;

        result.Funnel = new DeliveryFunnelDto
        {
            OrdersReceived = ordersReceived,
            DPMatched = dpMatched,
            PickedUp = pickedUp,
            InTransit = inProgress,
            Delivered = delivered,
            Cancelled = cancelled,
            MatchRate = ordersReceived > 0 ? (decimal)dpMatched / ordersReceived * 100 : 0,
            PickupRate = dpMatched > 0 ? (decimal)pickedUp / dpMatched * 100 : 0,
            DeliveryRate = pickedUp > 0 ? (decimal)delivered / pickedUp * 100 : 0
        };

        return result;
    }

    public async Task<FinancialAnalyticsDto> GetFinancialAnalyticsAsync(AnalyticsDrillDownRequest request, CancellationToken ct = default)
    {
        var result = new FinancialAnalyticsDto();

        var payments = await _context.Payments
            .Where(p => p.CreatedAt >= request.StartDate && p.CreatedAt <= request.EndDate && p.Status == "COMPLETED")
            .ToListAsync(ct);

        var commissions = await _context.CommissionRecords
            .Where(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate)
            .ToListAsync(ct);

        var settlements = await _context.Settlements
            .Where(s => s.CreatedAt >= request.StartDate && s.CreatedAt <= request.EndDate)
            .ToListAsync(ct);

        // Calculate previous period for comparison
        var periodLength = request.EndDate - request.StartDate;
        var previousStart = request.StartDate - periodLength;
        var previousPayments = await _context.Payments
            .Where(p => p.CreatedAt >= previousStart && p.CreatedAt < request.StartDate && p.Status == "COMPLETED")
            .SumAsync(p => p.Amount, ct);

        var grossRevenue = payments.Sum(p => p.Amount);
        var platformFees = commissions.Sum(c => c.PlatformFee);
        var dpcmCommissions = commissions.Sum(c => c.DPCMCommission);
        var dpPayouts = settlements.Where(s => s.BeneficiaryType == "DP").Sum(s => s.NetAmount);

        result.Summary = new FinancialSummaryDto
        {
            GrossRevenue = grossRevenue,
            NetRevenue = platformFees,
            PlatformFees = platformFees,
            DPCMCommissions = dpcmCommissions,
            DPPayouts = dpPayouts,
            RefundsIssued = payments.Where(p => p.PaymentType == "REFUND").Sum(p => p.Amount),
            GrowthVsPreviousPeriod = previousPayments > 0 ? (grossRevenue - previousPayments) / previousPayments * 100 : 0,
            AvgOrderValue = payments.Count > 0 ? grossRevenue / payments.Count : 0
        };

        // Revenue trend
        result.RevenueTrend = payments
            .GroupBy(p => p.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new RevenueTrendDto
            {
                Date = g.Key,
                Revenue = g.Sum(p => p.Amount),
                Orders = g.Count()
            })
            .ToList();

        // Revenue by channel (using payment method as proxy)
        result.ByChannel = payments
            .GroupBy(p => p.PaymentMethod ?? "UNKNOWN")
            .Select(g => new RevenueByChannelDto
            {
                Channel = g.Key,
                Revenue = g.Sum(p => p.Amount),
                Orders = g.Count(),
                Percentage = grossRevenue > 0 ? g.Sum(p => p.Amount) / grossRevenue * 100 : 0
            })
            .ToList();

        // Payment analytics
        result.PaymentAnalytics = new PaymentAnalyticsDto
        {
            TotalCollected = grossRevenue,
            CashCollected = payments.Where(p => p.PaymentMethod == "CASH").Sum(p => p.Amount),
            OnlineCollected = payments.Where(p => p.PaymentMethod != "CASH").Sum(p => p.Amount),
            CollectionRate = 95, // Placeholder
            ByPaymentMethod = payments.GroupBy(p => p.PaymentMethod ?? "OTHER").ToDictionary(g => g.Key, g => g.Sum(p => p.Amount))
        };

        // Settlement analytics
        result.SettlementAnalytics = new SettlementAnalyticsDto
        {
            TotalSettled = settlements.Where(s => s.Status == "COMPLETED").Sum(s => s.NetAmount),
            PendingSettlement = settlements.Where(s => s.Status == "PENDING").Sum(s => s.NetAmount),
            SettlementCount = settlements.Count(s => s.Status == "COMPLETED"),
            PendingCount = settlements.Count(s => s.Status == "PENDING"),
            OnTimeSettlementRate = 90 // Placeholder
        };

        return result;
    }

    public async Task<UserAnalyticsDto> GetUserAnalyticsAsync(AnalyticsDrillDownRequest request, CancellationToken ct = default)
    {
        var result = new UserAnalyticsDto();

        var users = await _context.Users
            .Where(u => u.CreatedAt >= request.StartDate && u.CreatedAt <= request.EndDate)
            .ToListAsync(ct);

        var allUsers = await _context.Users.ToListAsync(ct);

        // Acquisition
        result.Acquisition = new UserAcquisitionDto
        {
            NewUsersTotal = users.Count,
            NewDPs = users.Count(u => u.Role == "DP"),
            NewDPCMs = users.Count(u => u.Role == "DPCM"),
            NewBCs = users.Count(u => u.Role == "BC"),
            NewECs = users.Count(u => u.Role == "EC"),
            Trend = users
                .GroupBy(u => u.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new AcquisitionTrendDto
                {
                    Date = g.Key,
                    Total = g.Count(),
                    DPs = g.Count(u => u.Role == "DP"),
                    DPCMs = g.Count(u => u.Role == "DPCM"),
                    BCs = g.Count(u => u.Role == "BC"),
                    ECs = g.Count(u => u.Role == "EC")
                })
                .ToList()
        };

        // Retention (simplified)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var activeInLast30Days = allUsers.Count(u => u.LastLoginAt >= thirtyDaysAgo);
        var activeInLast7Days = allUsers.Count(u => u.LastLoginAt >= sevenDaysAgo);

        result.Retention = new UserRetentionDto
        {
            ActiveUsers = activeInLast30Days,
            InactiveUsers = allUsers.Count - activeInLast30Days,
            MonthlyActiveUserRate = allUsers.Count > 0 ? (decimal)activeInLast30Days / allUsers.Count * 100 : 0,
            WeeklyActiveUserRate = allUsers.Count > 0 ? (decimal)activeInLast7Days / allUsers.Count * 100 : 0
        };

        // Engagement
        var dpDeliveryCounts = await _context.Deliveries
            .Where(d => d.AssignedDPId.HasValue && d.Status == "DELIVERED")
            .GroupBy(d => d.AssignedDPId!.Value)
            .Select(g => new { DPId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        result.Engagement = new UserEngagementDto
        {
            AvgDeliveriesPerDP = dpDeliveryCounts.Count > 0 ? (decimal)dpDeliveryCounts.Average(d => d.Count) : 0,
            PowerUsers = dpDeliveryCounts.Count(d => d.Count > 50),
            CasualUsers = dpDeliveryCounts.Count(d => d.Count >= 5 && d.Count <= 50),
            DormantUsers = allUsers.Count(u => u.Role == "DP") - dpDeliveryCounts.Count
        };

        // Churn
        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var churned = allUsers.Count(u => u.LastLoginAt < lastMonth && u.CreatedAt < lastMonth);

        result.Churn = new UserChurnDto
        {
            ChurnedThisMonth = churned,
            ChurnRate = allUsers.Count > 0 ? (decimal)churned / allUsers.Count * 100 : 0,
            AtRiskUsers = allUsers.Count(u => u.LastLoginAt >= lastMonth && u.LastLoginAt < sevenDaysAgo),
            ChurnByRole = allUsers.Where(u => u.LastLoginAt < lastMonth)
                .GroupBy(u => u.Role)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return result;
    }

    public async Task<DPCMPerformanceAnalyticsDto> GetDPCMPerformanceAnalyticsAsync(AnalyticsDrillDownRequest request, CancellationToken ct = default)
    {
        var result = new DPCMPerformanceAnalyticsDto();

        var dpcms = await _context.DPCManagers
            .Include(d => d.User)
            .ToListAsync(ct);

        var dpProfiles = await _context.DeliveryPartnerProfiles.ToListAsync(ct);
        var behaviorIndexes = await _context.BehaviorIndexes.ToListAsync(ct);
        var commissions = await _context.CommissionRecords
            .Where(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate)
            .ToListAsync(ct);

        var deliveries = await _context.Deliveries
            .Where(d => d.CreatedAt >= request.StartDate && d.CreatedAt <= request.EndDate && d.Status == "DELIVERED")
            .ToListAsync(ct);

        var complaints = await _context.Complaints
            .Where(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate)
            .ToListAsync(ct);

        var rankings = new List<DPCMRankingDto>();
        var rank = 1;

        foreach (var dpcm in dpcms)
        {
            var managedDPs = dpProfiles.Where(dp => dp.DPCMId == dpcm.Id).ToList();
            var dpIds = managedDPs.Select(dp => dp.UserId).ToList();

            var dpcmDeliveries = deliveries.Where(d => d.AssignedDPId.HasValue && dpIds.Contains(d.AssignedDPId.Value)).ToList();
            var dpcmCommissions = commissions.Where(c => c.DPCMId == dpcm.UserId).Sum(c => c.DPCMCommission);
            var dpcmComplaints = complaints.Count(c => c.AgainstId.HasValue && dpIds.Contains(c.AgainstId.Value));
            var avgRating = behaviorIndexes.Where(b => dpIds.Contains(b.UserId)).Select(b => b.AverageRating).DefaultIfEmpty(0).Average();

            rankings.Add(new DPCMRankingDto
            {
                DPCMId = dpcm.UserId,
                DPCMName = dpcm.User?.Phone ?? "Unknown",
                ManagedDPs = managedDPs.Count,
                ActiveDPs = managedDPs.Count(dp => dp.IsActive),
                TotalDeliveries = dpcmDeliveries.Count,
                Revenue = dpcmDeliveries.Sum(d => d.FinalPrice ?? 0),
                Earnings = dpcmCommissions,
                AvgDPRating = avgRating,
                Complaints = dpcmComplaints,
                Score = (dpcmDeliveries.Count * 0.4m) + (avgRating * 20) - (dpcmComplaints * 5)
            });
        }

        // Sort by score and assign ranks
        rankings = rankings.OrderByDescending(r => r.Score).ToList();
        foreach (var r in rankings)
        {
            r.Rank = rank++;
        }

        result.Rankings = rankings;

        // Benchmark
        result.Benchmark = new DPCMBenchmarkDto
        {
            AvgDPsPerDPCM = dpcms.Count > 0 ? (decimal)dpProfiles.Count / dpcms.Count : 0,
            AvgDeliveriesPerDPCM = dpcms.Count > 0 ? (decimal)deliveries.Count / dpcms.Count : 0,
            AvgRevenuePerDPCM = dpcms.Count > 0 ? deliveries.Sum(d => d.FinalPrice ?? 0) / dpcms.Count : 0,
            AvgDPRating = behaviorIndexes.Count > 0 ? behaviorIndexes.Average(b => b.AverageRating) : 0,
            TopPerformerThreshold = rankings.Count > 0 ? rankings.Take(Math.Max(1, rankings.Count / 4)).Last().Score : 0
        };

        return result;
    }

    public async Task<ComplaintAnalyticsDto> GetComplaintAnalyticsAsync(AnalyticsDrillDownRequest request, CancellationToken ct = default)
    {
        var result = new ComplaintAnalyticsDto();

        var complaints = await _context.Complaints
            .Where(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate)
            .ToListAsync(ct);

        // Summary
        result.Summary = new ComplaintSummaryDto
        {
            TotalComplaints = complaints.Count,
            Open = complaints.Count(c => c.Status == "OPEN"),
            InProgress = complaints.Count(c => c.Status == "IN_PROGRESS"),
            Resolved = complaints.Count(c => c.Status == "RESOLVED"),
            Escalated = complaints.Count(c => c.Status == "ESCALATED"),
            ResolutionRate = complaints.Count > 0 ? (decimal)complaints.Count(c => c.Status == "RESOLVED") / complaints.Count * 100 : 0,
            CustomerSatisfactionRate = 85 // Placeholder
        };

        // Trend
        result.Trend = complaints
            .GroupBy(c => c.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ComplaintTrendDto
            {
                Date = g.Key,
                NewComplaints = g.Count(),
                Resolved = g.Count(c => c.Status == "RESOLVED"),
                Escalated = g.Count(c => c.Status == "ESCALATED"),
                ResolutionRate = g.Count() > 0 ? (decimal)g.Count(c => c.Status == "RESOLVED") / g.Count() * 100 : 0
            })
            .ToList();

        // By category
        result.ByCategory = complaints
            .GroupBy(c => c.Category ?? "OTHER")
            .Select(g => new ComplaintByCategoryDto
            {
                Category = g.Key,
                Count = g.Count(),
                Percentage = complaints.Count > 0 ? (decimal)g.Count() / complaints.Count * 100 : 0,
                EscalationRate = g.Count() > 0 ? (decimal)g.Count(c => c.Status == "ESCALATED") / g.Count() * 100 : 0
            })
            .ToList();

        // By source (complainant role)
        var complainerIds = complaints.Select(c => c.RaisedById).Distinct().ToList();
        var complainerRoles = await _context.Users
            .Where(u => complainerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Role, ct);

        result.BySource = complaints
            .GroupBy(c => complainerRoles.ContainsKey(c.RaisedById) ? complainerRoles[c.RaisedById] : "UNKNOWN")
            .Select(g => new ComplaintBySourceDto
            {
                Source = g.Key,
                Count = g.Count(),
                Percentage = complaints.Count > 0 ? (decimal)g.Count() / complaints.Count * 100 : 0
            })
            .ToList();

        // Resolution analytics
        result.Resolution = new ResolutionAnalyticsDto
        {
            AvgFirstResponseTime = 2, // Placeholder - hours
            AvgResolutionTime = 24, // Placeholder - hours
            ResolvedWithin24Hours = complaints.Count(c => c.Status == "RESOLVED") / 2,
            ResolvedWithin48Hours = complaints.Count(c => c.Status == "RESOLVED") / 3,
            ResolvedBeyond48Hours = complaints.Count(c => c.Status == "RESOLVED") / 6
        };

        // SLA analytics
        var slaBreaches = await _context.SLABreaches
            .Where(s => s.BreachedAt >= request.StartDate && s.BreachedAt <= request.EndDate)
            .ToListAsync(ct);

        result.SLA = new SLAAnalyticsDto
        {
            SLACompliance = complaints.Count > 0 ? (decimal)(complaints.Count - slaBreaches.Count) / complaints.Count * 100 : 100,
            SLABreaches = slaBreaches.Count,
            NearSLABreaches = complaints.Count(c => c.Status == "IN_PROGRESS") / 4, // Estimate
            BreachesByCategory = slaBreaches
                .GroupBy(s => s.BreachType ?? "OTHER")
                .Select(g => new SLABreachByCategoryDto
                {
                    Category = g.Key,
                    Breaches = g.Count()
                })
                .ToList()
        };

        return result;
    }

    public async Task<RealTimeMetricsDto> GetRealTimeMetricsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var oneHourAgo = now.AddHours(-1);

        var result = new RealTimeMetricsDto
        {
            Timestamp = now,
            ActiveDeliveries = await _context.Deliveries.CountAsync(d => d.Status == "IN_TRANSIT" || d.Status == "PICKED_UP", ct),
            OnlineDPs = await _context.DPAvailabilities.CountAsync(a => a.Status == "AVAILABLE", ct),
            WaitingOrders = await _context.Deliveries.CountAsync(d => d.Status == "PENDING" || d.Status == "CONFIRMED", ct),
            DeliveriesLastHour = await _context.Deliveries.CountAsync(d => d.CreatedAt >= oneHourAgo, ct),
            RevenueLastHour = await _context.Payments
                .Where(p => p.CreatedAt >= oneHourAgo && p.Status == "COMPLETED")
                .SumAsync(p => p.Amount, ct)
        };

        // Recent orders
        result.RecentOrders = await _context.Deliveries
            .OrderByDescending(d => d.CreatedAt)
            .Take(10)
            .Select(d => new LiveOrderDto
            {
                DeliveryId = d.Id,
                Status = d.Status,
                PickupCity = d.PickupAddress ?? "",
                DropCity = d.DropAddress ?? "",
                MinutesSinceCreated = (int)(now - d.CreatedAt).TotalMinutes
            })
            .ToListAsync(ct);

        // Active alerts
        var alerts = new List<ActiveAlertDto>();

        var openComplaints = await _context.Complaints.CountAsync(c => c.Status == "OPEN", ct);
        if (openComplaints > 10)
        {
            alerts.Add(new ActiveAlertDto
            {
                Severity = "WARNING",
                Type = "COMPLAINTS",
                Message = $"{openComplaints} open complaints need attention",
                Count = openComplaints,
                ActionUrl = "/admin/complaints"
            });
        }

        var pendingKYC = await _context.KYCRequests.CountAsync(k => k.Status == "PENDING", ct);
        if (pendingKYC > 20)
        {
            alerts.Add(new ActiveAlertDto
            {
                Severity = "INFO",
                Type = "KYC",
                Message = $"{pendingKYC} KYC requests pending review",
                Count = pendingKYC,
                ActionUrl = "/admin/kyc"
            });
        }

        var waitingOrders = result.WaitingOrders;
        if (waitingOrders > 50)
        {
            alerts.Add(new ActiveAlertDto
            {
                Severity = "CRITICAL",
                Type = "ORDERS",
                Message = $"{waitingOrders} orders waiting for assignment",
                Count = waitingOrders,
                ActionUrl = "/admin/deliveries"
            });
        }

        result.ActiveAlerts = alerts;

        return result;
    }

    public async Task<ComparisonAnalyticsDto> GetComparisonAnalyticsAsync(DateTime currentStart, DateTime currentEnd, DateTime previousStart, DateTime previousEnd, CancellationToken ct = default)
    {
        var result = new ComparisonAnalyticsDto();

        // Current period
        var currentDeliveries = await _context.Deliveries
            .Where(d => d.CreatedAt >= currentStart && d.CreatedAt <= currentEnd)
            .ToListAsync(ct);
        var currentPayments = await _context.Payments
            .Where(p => p.CreatedAt >= currentStart && p.CreatedAt <= currentEnd && p.Status == "COMPLETED")
            .ToListAsync(ct);
        var currentUsers = await _context.Users
            .Where(u => u.CreatedAt >= currentStart && u.CreatedAt <= currentEnd)
            .CountAsync(ct);
        var currentComplaints = await _context.Complaints
            .Where(c => c.CreatedAt >= currentStart && c.CreatedAt <= currentEnd)
            .CountAsync(ct);

        result.CurrentPeriod = new ComparisonPeriodDto
        {
            StartDate = currentStart,
            EndDate = currentEnd,
            Deliveries = currentDeliveries.Count,
            Revenue = currentPayments.Sum(p => p.Amount),
            NewUsers = currentUsers,
            AvgOrderValue = currentPayments.Count > 0 ? currentPayments.Sum(p => p.Amount) / currentPayments.Count : 0,
            OnTimeRate = 85, // Placeholder
            Complaints = currentComplaints
        };

        // Previous period
        var previousDeliveries = await _context.Deliveries
            .Where(d => d.CreatedAt >= previousStart && d.CreatedAt <= previousEnd)
            .ToListAsync(ct);
        var previousPayments = await _context.Payments
            .Where(p => p.CreatedAt >= previousStart && p.CreatedAt <= previousEnd && p.Status == "COMPLETED")
            .ToListAsync(ct);
        var previousUsers = await _context.Users
            .Where(u => u.CreatedAt >= previousStart && u.CreatedAt <= previousEnd)
            .CountAsync(ct);
        var previousComplaints = await _context.Complaints
            .Where(c => c.CreatedAt >= previousStart && c.CreatedAt <= previousEnd)
            .CountAsync(ct);

        result.PreviousPeriod = new ComparisonPeriodDto
        {
            StartDate = previousStart,
            EndDate = previousEnd,
            Deliveries = previousDeliveries.Count,
            Revenue = previousPayments.Sum(p => p.Amount),
            NewUsers = previousUsers,
            AvgOrderValue = previousPayments.Count > 0 ? previousPayments.Sum(p => p.Amount) / previousPayments.Count : 0,
            OnTimeRate = 85,
            Complaints = previousComplaints
        };

        // Calculate changes
        result.KeyMetricChanges = new List<MetricChangeDto>
        {
            CreateMetricChange("Deliveries", result.CurrentPeriod.Deliveries, result.PreviousPeriod.Deliveries, true),
            CreateMetricChange("Revenue", result.CurrentPeriod.Revenue, result.PreviousPeriod.Revenue, true),
            CreateMetricChange("New Users", result.CurrentPeriod.NewUsers, result.PreviousPeriod.NewUsers, true),
            CreateMetricChange("Avg Order Value", result.CurrentPeriod.AvgOrderValue, result.PreviousPeriod.AvgOrderValue, true),
            CreateMetricChange("Complaints", result.CurrentPeriod.Complaints, result.PreviousPeriod.Complaints, false)
        };

        return result;
    }

    private static MetricChangeDto CreateMetricChange(string name, decimal current, decimal previous, bool higherIsBetter)
    {
        var change = previous > 0 ? (current - previous) / previous * 100 : (current > 0 ? 100 : 0);
        var trend = change > 5 ? "UP" : change < -5 ? "DOWN" : "STABLE";

        return new MetricChangeDto
        {
            MetricName = name,
            CurrentValue = current,
            PreviousValue = previous,
            ChangePercentage = change,
            Trend = trend,
            IsPositive = higherIsBetter ? change >= 0 : change <= 0
        };
    }

    // ===================================================
    // STAKEHOLDER ONBOARDING IMPLEMENTATION
    // ===================================================

    public async Task<RegisterStakeholderResponse> RegisterStakeholderAsync(
        RegisterStakeholderRequest request,
        Guid adminId,
        CancellationToken ct = default)
    {
        try
        {
            // Validate phone uniqueness
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == request.Phone, ct);

            if (existingUser != null)
            {
                return new RegisterStakeholderResponse
                {
                    Success = false,
                    Message = "A user with this phone number already exists",
                    Errors = new List<string> { "Phone number already registered" }
                };
            }

            // Create User
            var user = new User
            {
                Id = Guid.NewGuid(),
                Phone = request.Phone,
                FullName = request.FullName,
                Email = request.Email,
                Role = request.Role,
                IsActive = true,
                IsPhoneVerified = request.SkipKYC,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            // Create role-specific profile
            switch (request.Role.ToUpper())
            {
                case "DPCM":
                    CreateDPCMProfile(user.Id, request);
                    break;
                case "DP":
                    CreateDPProfile(user.Id, request);
                    break;
                case "BC":
                case "DBC":
                    CreateBCProfile(user.Id, request);
                    break;
            }

            // Create wallet if requested
            if (request.AutoCreateWallet)
            {
                var wallet = new Wallet
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Balance = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Wallets.Add(wallet);
            }

            // Log audit
            _context.AdminAuditLogs.Add(new AdminAuditLog
            {
                Id = Guid.NewGuid(),
                UserId = adminId,
                Action = "STAKEHOLDER_REGISTERED",
                EntityType = "User",
                EntityId = user.Id.ToString(),
                NewValue = $"Role: {request.Role}, Phone: {request.Phone}, Name: {request.FullName}",
                IpAddress = "system",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Admin {AdminId} registered new stakeholder {UserId} with role {Role}",
                adminId, user.Id, request.Role);

            return new RegisterStakeholderResponse
            {
                Success = true,
                UserId = user.Id,
                Phone = user.Phone,
                Role = user.Role,
                Message = $"{request.Role} registered successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering stakeholder");
            return new RegisterStakeholderResponse
            {
                Success = false,
                Message = "Failed to register stakeholder",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private void CreateDPCMProfile(Guid userId, RegisterStakeholderRequest request)
    {
        var profile = new DPCManager
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationName = request.BusinessName ?? request.FullName,
            ContactPersonName = request.FullName,
            PAN = request.BusinessPAN ?? "",
            CommissionType = request.CommissionType ?? "PERCENTAGE",
            CommissionValue = request.CommissionValue ?? 5m,
            SecurityDeposit = request.SecurityDeposit ?? 0,
            ServiceRegions = request.ServiceRegions != null ? System.Text.Json.JsonSerializer.Serialize(request.ServiceRegions) : null,
            IsActive = request.SkipKYC,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DPCManagers.Add(profile);
    }

    private void CreateDPProfile(Guid userId, RegisterStakeholderRequest request)
    {
        var profile = new DeliveryPartnerProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = request.FullName,
            VehicleType = request.VehicleType ?? "BIKE",
            DPCMId = request.DPCMId,
            IsActive = request.SkipKYC,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DeliveryPartnerProfiles.Add(profile);

        // Create service area if pincodes provided
        if (request.ServicePincodes?.Any() == true)
        {
            var serviceArea = new ServiceArea
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserRole = "DP",
                AreaName = "Service Area",
                AreaType = "CIRCLE",
                CenterLat = 0,
                CenterLng = 0,
                RadiusKm = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ServiceAreas.Add(serviceArea);
        }
    }

    private void CreateBCProfile(Guid userId, RegisterStakeholderRequest request)
    {
        var profile = new BusinessConsumerProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BusinessName = request.BusinessName ?? request.FullName,
            ContactPersonName = request.FullName,
            BusinessCategory = request.BusinessType ?? "RETAIL",
            GSTIN = request.GSTIN,
            PAN = request.BusinessPAN ?? "",
            IsActive = request.SkipKYC,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.BusinessConsumerProfiles.Add(profile);
    }

    public async Task<StakeholderListResponse> GetStakeholdersAsync(
        StakeholderListRequest request,
        CancellationToken ct = default)
    {
        var query = _context.Users.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.Role))
        {
            query = query.Where(u => u.Role == request.Role);
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            var isActive = request.Status.ToUpper() == "ACTIVE";
            query = query.Where(u => u.IsActive == isActive);
        }

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(u =>
                (u.Phone != null && u.Phone.Contains(term)) ||
                (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)));
        }

        if (request.RegisteredFrom.HasValue)
        {
            query = query.Where(u => u.CreatedAt >= request.RegisteredFrom.Value);
        }

        if (request.RegisteredTo.HasValue)
        {
            query = query.Where(u => u.CreatedAt <= request.RegisteredTo.Value);
        }

        // Exclude Admin users from stakeholder list
        query = query.Where(u => u.Role != "Admin");

        var totalCount = await query.CountAsync(ct);

        // Apply sorting
        query = request.SortDesc
            ? query.OrderByDescending(u => u.CreatedAt)
            : query.OrderBy(u => u.CreatedAt);

        // Apply pagination
        var users = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        // Get related data
        var userIds = users.Select(u => u.Id).ToList();
        var dpProfiles = await _context.DeliveryPartnerProfiles
            .Where(dp => userIds.Contains(dp.UserId))
            .ToListAsync(ct);
        var bcProfiles = await _context.BusinessConsumerProfiles
            .Where(bc => userIds.Contains(bc.UserId))
            .ToListAsync(ct);
        var dpcmProfiles = await _context.DPCManagers
            .Where(dpcm => userIds.Contains(dpcm.UserId))
            .ToListAsync(ct);
        var wallets = await _context.Wallets
            .Where(w => userIds.Contains(w.UserId))
            .ToListAsync(ct);
        var dpAvailabilities = await _context.DPAvailabilities
            .Where(a => userIds.Contains(a.DPId))
            .ToListAsync(ct);

        // Get DPCM names for DPs
        var dpcmIds = dpProfiles.Where(dp => dp.DPCMId.HasValue).Select(dp => dp.DPCMId!.Value).Distinct().ToList();
        var dpcmNames = await _context.Users
            .Where(u => dpcmIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.Phone ?? "", ct);

        // Map to DTOs
        var items = users.Select(u =>
        {
            var dpProfile = dpProfiles.FirstOrDefault(dp => dp.UserId == u.Id);
            var bcProfile = bcProfiles.FirstOrDefault(bc => bc.UserId == u.Id);
            var dpcmProfile = dpcmProfiles.FirstOrDefault(dpcm => dpcm.UserId == u.Id);
            var wallet = wallets.FirstOrDefault(w => w.UserId == u.Id);
            var availability = dpAvailabilities.FirstOrDefault(a => a.DPId == u.Id);

            return new StakeholderListItemDto
            {
                Id = u.Id,
                Phone = u.Phone ?? "",
                PhoneMasked = MaskPhone(u.Phone ?? ""),
                FullName = u.FullName ?? "N/A",
                Email = u.Email,
                EmailMasked = MaskEmail(u.Email),
                Role = u.Role,
                Status = u.IsActive ? "Active" : "Inactive",
                KYCStatus = u.IsPhoneVerified ? "VERIFIED" : "PENDING",
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                BusinessName = bcProfile?.BusinessName ?? dpcmProfile?.OrganizationName,
                VehicleType = dpProfile?.VehicleType,
                DPCMName = dpProfile?.DPCMId.HasValue == true && dpcmNames.ContainsKey(dpProfile.DPCMId.Value)
                    ? dpcmNames[dpProfile.DPCMId.Value]
                    : null,
                TotalDeliveries = 0,
                Rating = null,
                WalletBalance = wallet?.Balance,
                IsOnline = availability?.Status == "ONLINE"
            };
        }).ToList();

        // Get summary counts
        var allUsersQuery = _context.Users.Where(u => u.Role != "Admin");
        var totalDPCMs = await allUsersQuery.CountAsync(u => u.Role == "DPCM", ct);
        var totalDPs = await allUsersQuery.CountAsync(u => u.Role == "DP", ct);
        var totalBCs = await allUsersQuery.CountAsync(u => u.Role == "BC" || u.Role == "DBC", ct);
        var totalECs = await allUsersQuery.CountAsync(u => u.Role == "EC", ct);
        var pendingKYC = await allUsersQuery.CountAsync(u => !u.IsPhoneVerified, ct);

        return new StakeholderListResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalDPCMs = totalDPCMs,
            TotalDPs = totalDPs,
            TotalBCs = totalBCs,
            TotalECs = totalECs,
            PendingKYC = pendingKYC
        };
    }

    public async Task<StakeholderDetailDto?> GetStakeholderDetailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return null;

        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
        var deliveries = await _context.Deliveries
            .Where(d => d.RequesterId == userId || d.AssignedDPId == userId)
            .ToListAsync(ct);

        var detail = new StakeholderDetailDto
        {
            Id = user.Id,
            Phone = user.Phone ?? "",
            FullName = user.FullName ?? "N/A",
            Email = user.Email,
            Role = user.Role,
            Status = user.IsActive ? "Active" : "Inactive",
            CreatedAt = user.CreatedAt,
            KYCStatus = user.IsPhoneVerified ? "VERIFIED" : "PENDING",
            WalletBalance = wallet?.Balance ?? 0,
            TotalDeliveries = deliveries.Count,
            CompletedDeliveries = deliveries.Count(d => d.Status == "DELIVERED"),
            CancelledDeliveries = deliveries.Count(d => d.Status == "CANCELLED")
        };

        // Load role-specific details
        switch (user.Role.ToUpper())
        {
            case "DPCM":
                var dpcmProfile = await _context.DPCManagers
                    .FirstOrDefaultAsync(p => p.UserId == userId, ct);
                if (dpcmProfile != null)
                {
                    var managedDPs = await _context.DeliveryPartnerProfiles
                        .Where(dp => dp.DPCMId == userId)
                        .ToListAsync(ct);

                    detail.DPCMDetails = new DPCMDetailDto
                    {
                        BusinessName = dpcmProfile.OrganizationName,
                        CommissionType = dpcmProfile.CommissionType ?? "PERCENTAGE",
                        CommissionValue = dpcmProfile.CommissionValue ?? 0,
                        SecurityDeposit = dpcmProfile.SecurityDeposit,
                        ManagedDPsCount = managedDPs.Count,
                        ActiveDPsCount = managedDPs.Count(dp => dp.IsActive),
                        ServiceRegions = dpcmProfile.ServiceRegions != null
                            ? TryParseJsonArray(dpcmProfile.ServiceRegions)
                            : new List<string>()
                    };
                }
                break;

            case "DP":
                var dpProfile = await _context.DeliveryPartnerProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId, ct);
                if (dpProfile != null)
                {
                    var serviceAreas = await _context.ServiceAreas
                        .Where(sa => sa.UserId == userId && sa.IsActive)
                        .ToListAsync(ct);
                    var availability = await _context.DPAvailabilities
                        .FirstOrDefaultAsync(a => a.DPId == userId, ct);

                    string? dpcmName = null;
                    if (dpProfile.DPCMId.HasValue)
                    {
                        var dpcm = await _context.Users.FindAsync(dpProfile.DPCMId.Value);
                        dpcmName = dpcm?.FullName ?? dpcm?.Phone;
                    }

                    detail.DPDetails = new DPDetailDto
                    {
                        DPCMId = dpProfile.DPCMId,
                        DPCMName = dpcmName,
                        VehicleType = dpProfile.VehicleType,
                        VehicleNumber = null,
                        ServicePincodes = new List<string>(),
                        IsOnline = availability?.Status == "ONLINE",
                        LastActiveAt = availability?.UpdatedAt,
                        BehaviorIndex = 100
                    };
                    detail.Rating = 5;
                }
                break;

            case "BC":
            case "DBC":
                var bcProfile = await _context.BusinessConsumerProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId, ct);
                if (bcProfile != null)
                {
                    var apiKeys = await _context.BCApiCredentials
                        .Where(c => c.BusinessConsumerId == userId)
                        .CountAsync(ct);

                    detail.BCDetails = new BCDetailDto
                    {
                        BusinessName = bcProfile.BusinessName,
                        BusinessType = bcProfile.BusinessCategory,
                        GSTIN = bcProfile.GSTIN,
                        BusinessPAN = bcProfile.PAN,
                        HasAPIAccess = apiKeys > 0,
                        APIKeyCount = apiKeys
                    };
                }
                break;
        }

        return detail;
    }

    private static List<string> TryParseJsonArray(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return json.Split(',').Select(s => s.Trim()).ToList();
        }
    }

    public async Task<OnboardingStatsDto> GetOnboardingStatsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var users = await _context.Users.Where(u => u.Role != "Admin").ToListAsync(ct);

        return new OnboardingStatsDto
        {
            TotalStakeholders = users.Count,
            RegisteredToday = users.Count(u => u.CreatedAt.Date == today),
            RegisteredThisWeek = users.Count(u => u.CreatedAt >= weekStart),
            RegisteredThisMonth = users.Count(u => u.CreatedAt >= monthStart),
            PendingKYC = users.Count(u => !u.IsPhoneVerified),
            PendingApproval = users.Count(u => !u.IsActive),
            DPCMStats = CreateRoleStats(users, "DPCM", monthStart),
            DPStats = CreateRoleStats(users, "DP", monthStart),
            BCStats = CreateRoleStats(users.Where(u => u.Role == "BC" || u.Role == "DBC").ToList(), null, monthStart),
            ECStats = CreateRoleStats(users, "EC", monthStart)
        };
    }

    private static RoleStatsDto CreateRoleStats(List<User> allUsers, string? role, DateTime monthStart)
    {
        var users = role != null ? allUsers.Where(u => u.Role == role).ToList() : allUsers;
        return new RoleStatsDto
        {
            Total = users.Count,
            Active = users.Count(u => u.IsActive),
            Inactive = users.Count(u => !u.IsActive),
            PendingKYC = users.Count(u => !u.IsPhoneVerified),
            NewThisMonth = users.Count(u => u.CreatedAt >= monthStart)
        };
    }

    public async Task<List<AvailableDPCMDto>> GetAvailableDPCMsAsync(CancellationToken ct = default)
    {
        var dpcmProfiles = await _context.DPCManagers
            .Include(p => p.User)
            .Where(p => p.IsActive && p.User.IsActive)
            .ToListAsync(ct);

        var dpcmIds = dpcmProfiles.Select(p => p.UserId).ToList();
        var dpCounts = await _context.DeliveryPartnerProfiles
            .Where(dp => dp.DPCMId.HasValue && dpcmIds.Contains(dp.DPCMId.Value))
            .GroupBy(dp => dp.DPCMId!.Value)
            .Select(g => new { DPCMId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DPCMId, x => x.Count, ct);

        return dpcmProfiles.Select(p => new AvailableDPCMDto
        {
            Id = p.UserId,
            Name = p.User.FullName ?? p.User.Phone ?? "",
            Phone = p.User.Phone ?? "",
            BusinessName = p.OrganizationName,
            ManagedDPsCount = dpCounts.ContainsKey(p.UserId) ? dpCounts[p.UserId] : 0,
            ServiceRegions = p.ServiceRegions != null ? TryParseJsonArray(p.ServiceRegions) : new List<string>()
        }).ToList();
    }

    private static string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 4)
            return "****";
        return "******" + phone.Substring(phone.Length - 4);
    }

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return null;
        var parts = email.Split('@');
        var name = parts[0];
        var maskedName = name.Length > 2 ? name[0] + "***" + name[^1] : "***";
        return maskedName + "@" + parts[1];
    }
}
