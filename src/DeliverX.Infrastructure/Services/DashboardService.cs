using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliverX.Application.DTOs.Dashboard;
using DeliverX.Application.Services;
using DeliverX.Domain.Entities;
using DeliverX.Infrastructure.Data;

namespace DeliverX.Infrastructure.Services;

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

        // Get all DPs managed by this DPCM
        var managedDPs = await _context.DeliveryPartnerProfiles
            .Include(dp => dp.User)
            .Where(dp => dp.DPCMId == dpcmId)
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

        // Get DPCM earnings
        var commissionConfig = await _context.DPCMCommissionConfigs
            .Where(c => c.DPCMId == dpcmId && (c.EffectiveTo == null || c.EffectiveTo > DateTime.UtcNow))
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
        var query = _context.DeliveryPartnerProfiles
            .Include(dp => dp.User)
            .Where(dp => dp.DPCMId == dpcmId);

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
        var dp = await _context.DeliveryPartnerProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == dpId && dp.DPCMId == dpcmId, ct);

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
        // Get all DPs managed by this DPCM
        var dpIds = await _context.DeliveryPartnerProfiles
            .Where(dp => dp.DPCMId == dpcmId)
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
}
