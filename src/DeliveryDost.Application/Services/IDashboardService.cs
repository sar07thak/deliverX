using System;
using System.Threading;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs.Dashboard;

namespace DeliveryDost.Application.Services;

public interface IDashboardService
{
    // Admin Dashboard
    Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken ct = default);
    Task<PlatformStatsDto> GetPlatformStatsAsync(CancellationToken ct = default);
    Task<RevenueStatsDto> GetRevenueStatsAsync(CancellationToken ct = default);
    Task<ReportResponse> GenerateReportAsync(ReportRequest request, CancellationToken ct = default);

    // Drill-Down Analytics
    Task<GeographicAnalyticsDto> GetGeographicAnalyticsAsync(AnalyticsDrillDownRequest request, CancellationToken ct = default);
    Task<DeliveryPerformanceAnalyticsDto> GetDeliveryPerformanceAnalyticsAsync(AnalyticsDrillDownRequest request, CancellationToken ct = default);
    Task<FinancialAnalyticsDto> GetFinancialAnalyticsAsync(AnalyticsDrillDownRequest request, CancellationToken ct = default);
    Task<UserAnalyticsDto> GetUserAnalyticsAsync(AnalyticsDrillDownRequest request, CancellationToken ct = default);
    Task<DPCMPerformanceAnalyticsDto> GetDPCMPerformanceAnalyticsAsync(AnalyticsDrillDownRequest request, CancellationToken ct = default);
    Task<ComplaintAnalyticsDto> GetComplaintAnalyticsAsync(AnalyticsDrillDownRequest request, CancellationToken ct = default);
    Task<RealTimeMetricsDto> GetRealTimeMetricsAsync(CancellationToken ct = default);
    Task<ComparisonAnalyticsDto> GetComparisonAnalyticsAsync(DateTime currentStart, DateTime currentEnd, DateTime previousStart, DateTime previousEnd, CancellationToken ct = default);

    // DPCM Dashboard
    Task<DPCMDashboardDto> GetDPCMDashboardAsync(Guid dpcmId, CancellationToken ct = default);
    Task<DPCMPartnersResponse> GetDPCMPartnersAsync(Guid dpcmId, DPCMPartnersRequest request, CancellationToken ct = default);
    Task<bool> UpdateDPStatusByDPCMAsync(Guid dpcmId, Guid dpId, bool isActive, CancellationToken ct = default);
    Task<DPCMDeliveriesResponse> GetDPCMDeliveriesAsync(Guid dpcmId, DPCMDeliveriesRequest request, CancellationToken ct = default);
    Task<DPCMCommissionConfigDto> GetDPCMCommissionConfigAsync(Guid dpcmId, CancellationToken ct = default);
    Task<bool> UpdateDPCMCommissionConfigAsync(Guid dpcmId, UpdateCommissionConfigRequest request, CancellationToken ct = default);
    Task<DPCMSettlementsResponse> GetDPCMSettlementsAsync(Guid dpcmId, DPCMSettlementsRequest request, CancellationToken ct = default);
    Task<bool> RequestDPCMSettlementAsync(Guid dpcmId, decimal amount, CancellationToken ct = default);

    // User Management
    Task<UserListResponse> GetUsersAsync(UserListRequest request, CancellationToken ct = default);
    Task<UserListItemDto?> GetUserAsync(Guid userId, CancellationToken ct = default);
    Task<bool> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequest request, Guid adminId, CancellationToken ct = default);

    // KYC Management
    Task<KYCListResponse> GetKYCRequestsAsync(KYCListRequest request, CancellationToken ct = default);
    Task<bool> ApproveKYCAsync(Guid kycId, ApproveKYCRequest request, Guid adminId, CancellationToken ct = default);
    Task<bool> RejectKYCAsync(Guid kycId, RejectKYCRequest request, Guid adminId, CancellationToken ct = default);

    // Audit Logs
    Task<AuditLogResponse> GetAuditLogsAsync(AuditLogRequest request, CancellationToken ct = default);

    // System Configuration
    Task<SystemConfigDto> GetSystemConfigAsync(CancellationToken ct = default);
    Task<bool> UpdateSystemConfigAsync(UpdateConfigRequest request, Guid adminId, CancellationToken ct = default);

    // Stakeholder Onboarding (Admin)
    Task<RegisterStakeholderResponse> RegisterStakeholderAsync(RegisterStakeholderRequest request, Guid adminId, CancellationToken ct = default);
    Task<StakeholderListResponse> GetStakeholdersAsync(StakeholderListRequest request, CancellationToken ct = default);
    Task<StakeholderDetailDto?> GetStakeholderDetailAsync(Guid userId, CancellationToken ct = default);
    Task<OnboardingStatsDto> GetOnboardingStatsAsync(CancellationToken ct = default);
    Task<List<AvailableDPCMDto>> GetAvailableDPCMsAsync(CancellationToken ct = default);
}
