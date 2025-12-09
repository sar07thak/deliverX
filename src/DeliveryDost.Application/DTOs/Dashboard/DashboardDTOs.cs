using System;
using System.Collections.Generic;

namespace DeliveryDost.Application.DTOs.Dashboard;

// Admin Dashboard DTOs
public class AdminDashboardDto
{
    // Platform Overview
    public PlatformStatsDto PlatformStats { get; set; } = new();
    public RevenueStatsDto RevenueStats { get; set; } = new();
    public List<DailyMetricDto> DailyMetrics { get; set; } = new();
    public List<TopPerformerDto> TopDPs { get; set; } = new();
    public List<AlertDto> Alerts { get; set; } = new();
}

public class PlatformStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalDPs { get; set; }
    public int ActiveDPs { get; set; }
    public int TotalDPCMs { get; set; }
    public int TotalBCs { get; set; }
    public int TotalECs { get; set; }
    public int TotalDeliveries { get; set; }
    public int DeliveriesToday { get; set; }
    public int DeliveriesThisWeek { get; set; }
    public int DeliveriesThisMonth { get; set; }
    public int PendingKYC { get; set; }
    public int OpenComplaints { get; set; }
    public decimal AvgRating { get; set; }
}

public class RevenueStatsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueToday { get; set; }
    public decimal RevenueThisWeek { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal TotalCommissions { get; set; }
    public decimal TotalPlatformFees { get; set; }
    public decimal TotalSettlements { get; set; }
    public decimal PendingSettlements { get; set; }
}

public class DailyMetricDto
{
    public DateTime Date { get; set; }
    public int Deliveries { get; set; }
    public decimal Revenue { get; set; }
    public int NewUsers { get; set; }
}

public class TopPerformerDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalDeliveries { get; set; }
    public decimal Rating { get; set; }
    public decimal TotalEarnings { get; set; }
}

public class AlertDto
{
    public string Type { get; set; } = string.Empty; // WARNING, CRITICAL, INFO
    public string Category { get; set; } = string.Empty; // COMPLAINT, KYC, SETTLEMENT, etc.
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime CreatedAt { get; set; }
}

// DPCM Dashboard DTOs
public class DPCMDashboardDto
{
    public DPCMStatsDto Stats { get; set; } = new();
    public List<DPSummaryDto> ManagedDPs { get; set; } = new();
    public List<PendingActionDto> PendingActions { get; set; } = new();
    public EarningsStatsDto Earnings { get; set; } = new();
}

public class DPCMStatsDto
{
    public int TotalManagedDPs { get; set; }
    public int ActiveDPs { get; set; }
    public int InactiveDPs { get; set; }
    public int PendingOnboarding { get; set; }
    public int TotalDeliveries { get; set; }
    public int DeliveriesToday { get; set; }
    public int OpenComplaints { get; set; }
    public decimal AvgDPRating { get; set; }
}

public class DPSummaryDto
{
    public Guid DPId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalDeliveries { get; set; }
    public decimal Rating { get; set; }
    public decimal BehaviorIndex { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastActive { get; set; }
}

public class PendingActionDto
{
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public string Priority { get; set; } = "NORMAL"; // LOW, NORMAL, HIGH, URGENT
    public DateTime DueDate { get; set; }
}

public class EarningsStatsDto
{
    public decimal TotalEarnings { get; set; }
    public decimal EarningsThisMonth { get; set; }
    public decimal PendingSettlement { get; set; }
    public decimal CommissionRate { get; set; }
}

// User Management DTOs
public class UserListRequest
{
    public string? Role { get; set; }
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; }
}

public class UserListResponse
{
    public List<UserListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class UserListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? KYCStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UpdateUserStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

// System Configuration DTOs
public class SystemConfigDto
{
    public List<ConfigItemDto> Items { get; set; } = new();
}

public class ConfigItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = "STRING"; // STRING, NUMBER, BOOLEAN, JSON
    public DateTime UpdatedAt { get; set; }
}

public class UpdateConfigRequest
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

// Report DTOs
public class ReportRequest
{
    public string ReportType { get; set; } = string.Empty; // DELIVERIES, REVENUE, USERS, COMPLAINTS
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? GroupBy { get; set; } // DAY, WEEK, MONTH
    public string? FilterBy { get; set; }
    public string? FilterValue { get; set; }
}

public class ReportResponse
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string GeneratedAt { get; set; } = string.Empty;
    public object Summary { get; set; } = new();
    public List<object> Data { get; set; } = new();
}

// Audit Log DTOs
public class AuditLogRequest
{
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AuditLogResponse
{
    public List<AuditLogItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class AuditLogItemDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

// KYC Management DTOs
public class KYCListRequest
{
    public string? Status { get; set; }
    public string? DocumentType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class KYCListResponse
{
    public List<KYCListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class KYCListItemDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
}

public class ApproveKYCRequest
{
    public string? Notes { get; set; }
}

public class RejectKYCRequest
{
    public string Reason { get; set; } = string.Empty;
}

// DPCM Partner List DTOs
public class DPCMPartnersRequest
{
    public string? Status { get; set; } // all, active, inactive, pending-kyc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class DPCMPartnersResponse
{
    public List<DPCMPartnerDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class DPCMPartnerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string KYCStatus { get; set; } = string.Empty;
    public int TotalDeliveries { get; set; }
    public decimal Rating { get; set; }
    public decimal Earnings { get; set; }
    public bool IsOnline { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActive { get; set; }
}

// DPCM Deliveries DTOs
public class DPCMDeliveriesRequest
{
    public string? Status { get; set; }
    public Guid? DPId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class DPCMDeliveriesResponse
{
    public List<DPCMDeliveryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class DPCMDeliveryDto
{
    public Guid Id { get; set; }
    public string TrackingId { get; set; } = string.Empty;
    public string DPName { get; set; } = string.Empty;
    public Guid? DPId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public string DropAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Commission { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

// DPCM Commission DTOs
public class DPCMCommissionConfigDto
{
    public string CommissionType { get; set; } = "PERCENTAGE"; // PERCENTAGE or FIXED
    public decimal CommissionValue { get; set; }
    public decimal MinCommission { get; set; }
    public decimal MaxCommission { get; set; }
    public DateTime? EffectiveFrom { get; set; }
}

public class UpdateCommissionConfigRequest
{
    public string CommissionType { get; set; } = "PERCENTAGE";
    public decimal CommissionValue { get; set; }
    public decimal MinCommission { get; set; }
    public decimal MaxCommission { get; set; }
}

// DPCM Settlement DTOs
public class DPCMSettlementsRequest
{
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class DPCMSettlementsResponse
{
    public DPCMSettlementSummaryDto Summary { get; set; } = new();
    public List<DPCMSettlementDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class DPCMSettlementSummaryDto
{
    public decimal AvailableBalance { get; set; }
    public decimal TotalSettledThisMonth { get; set; }
    public string? BankAccount { get; set; }
}

public class DPCMSettlementDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class RequestSettlementRequest
{
    public decimal Amount { get; set; }
}

public class UpdateDPStatusRequest
{
    public bool IsActive { get; set; }
}
