using System;
using System.Collections.Generic;

namespace DeliveryDost.Application.DTOs.Dashboard;

// ============================================
// Drill-Down Analytics DTOs
// ============================================

public class AnalyticsDrillDownRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Granularity { get; set; } = "DAY"; // HOUR, DAY, WEEK, MONTH
    public string? Region { get; set; }
    public string? ServiceArea { get; set; }
    public string? DPCMId { get; set; }
    public string? DPId { get; set; }
}

// ============================================
// Geographic Analytics
// ============================================

public class GeographicAnalyticsDto
{
    public List<StateAnalyticsDto> ByState { get; set; } = new();
    public List<DistrictAnalyticsDto> ByDistrict { get; set; } = new();
    public List<ServiceAreaAnalyticsDto> ByServiceArea { get; set; } = new();
    public List<HotspotDto> Hotspots { get; set; } = new();
    public List<CoverageGapDto> CoverageGaps { get; set; } = new();
}

public class StateAnalyticsDto
{
    public string StateCode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public int TotalDeliveries { get; set; }
    public decimal Revenue { get; set; }
    public int ActiveDPs { get; set; }
    public int ActiveBCs { get; set; }
    public decimal AvgDeliveryTime { get; set; } // minutes
    public decimal SuccessRate { get; set; }
    public decimal GrowthRate { get; set; } // vs previous period
}

public class DistrictAnalyticsDto
{
    public string StateCode { get; set; } = string.Empty;
    public string DistrictName { get; set; } = string.Empty;
    public int TotalDeliveries { get; set; }
    public decimal Revenue { get; set; }
    public int ActiveDPs { get; set; }
    public decimal AvgDeliveryTime { get; set; }
    public decimal SuccessRate { get; set; }
}

public class ServiceAreaAnalyticsDto
{
    public Guid ServiceAreaId { get; set; }
    public string ServiceAreaName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int TotalDeliveries { get; set; }
    public decimal Revenue { get; set; }
    public int ActiveDPs { get; set; }
    public int DemandLevel { get; set; } // 1-5
    public int SupplyLevel { get; set; } // 1-5
    public decimal AvgWaitTime { get; set; } // minutes before DP pickup
    public decimal AvgDeliveryTime { get; set; }
}

public class HotspotDto
{
    public string AreaName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int DeliveryCount { get; set; }
    public string TimeSlot { get; set; } = string.Empty; // "Morning", "Afternoon", "Evening"
    public decimal DemandIntensity { get; set; } // deliveries per hour
}

public class CoverageGapDto
{
    public string Pincode { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public int RequestCount { get; set; } // unfulfilled requests
    public int AvailableDPs { get; set; }
    public string SuggestedAction { get; set; } = string.Empty;
}

// ============================================
// Delivery Performance Analytics
// ============================================

public class DeliveryPerformanceAnalyticsDto
{
    public PerformanceSummaryDto Summary { get; set; } = new();
    public List<TimeSlotPerformanceDto> ByTimeSlot { get; set; } = new();
    public List<VehicleTypePerformanceDto> ByVehicleType { get; set; } = new();
    public List<DistanceRangePerformanceDto> ByDistanceRange { get; set; } = new();
    public List<DayOfWeekPerformanceDto> ByDayOfWeek { get; set; } = new();
    public DeliveryFunnelDto Funnel { get; set; } = new();
}

public class PerformanceSummaryDto
{
    public int TotalDeliveries { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int InProgress { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal CancellationRate { get; set; }
    public decimal OnTimeRate { get; set; }
    public decimal AvgPickupTime { get; set; } // minutes from order to pickup
    public decimal AvgDeliveryTime { get; set; } // minutes from pickup to delivery
    public decimal AvgTotalTime { get; set; } // total order to delivery
    public decimal AvgDistanceKm { get; set; }
    public decimal AvgOrderValue { get; set; }
}

public class TimeSlotPerformanceDto
{
    public string TimeSlot { get; set; } = string.Empty; // "00:00-04:00", "04:00-08:00", etc.
    public int TotalDeliveries { get; set; }
    public decimal AvgDeliveryTime { get; set; }
    public decimal OnTimeRate { get; set; }
    public int ActiveDPs { get; set; }
    public decimal DemandSupplyRatio { get; set; }
}

public class VehicleTypePerformanceDto
{
    public string VehicleType { get; set; } = string.Empty; // BIKE, CAR, MINI_TRUCK
    public int TotalDeliveries { get; set; }
    public decimal AvgDeliveryTime { get; set; }
    public decimal AvgDistanceKm { get; set; }
    public decimal AvgEarningsPerDelivery { get; set; }
    public decimal OnTimeRate { get; set; }
}

public class DistanceRangePerformanceDto
{
    public string DistanceRange { get; set; } = string.Empty; // "0-2km", "2-5km", "5-10km", "10-15km", ">15km"
    public int TotalDeliveries { get; set; }
    public decimal AvgDeliveryTime { get; set; }
    public decimal AvgPrice { get; set; }
    public decimal OnTimeRate { get; set; }
    public decimal CancellationRate { get; set; }
}

public class DayOfWeekPerformanceDto
{
    public string DayOfWeek { get; set; } = string.Empty;
    public int DayIndex { get; set; } // 0=Sunday, 6=Saturday
    public int TotalDeliveries { get; set; }
    public decimal Revenue { get; set; }
    public decimal AvgDeliveryTime { get; set; }
    public decimal PeakHour { get; set; }
    public int PeakHourDeliveries { get; set; }
}

public class DeliveryFunnelDto
{
    public int OrdersReceived { get; set; }
    public int DPMatched { get; set; }
    public int PickedUp { get; set; }
    public int InTransit { get; set; }
    public int Delivered { get; set; }
    public int Cancelled { get; set; }
    public decimal MatchRate { get; set; }
    public decimal PickupRate { get; set; }
    public decimal DeliveryRate { get; set; }
}

// ============================================
// Financial Analytics
// ============================================

public class FinancialAnalyticsDto
{
    public FinancialSummaryDto Summary { get; set; } = new();
    public List<RevenueTrendDto> RevenueTrend { get; set; } = new();
    public List<RevenueByChannelDto> ByChannel { get; set; } = new();
    public List<RevenueByUserTypeDto> ByUserType { get; set; } = new();
    public PaymentAnalyticsDto PaymentAnalytics { get; set; } = new();
    public SettlementAnalyticsDto SettlementAnalytics { get; set; } = new();
}

public class FinancialSummaryDto
{
    public decimal GrossRevenue { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal PlatformFees { get; set; }
    public decimal DPCMCommissions { get; set; }
    public decimal DPPayouts { get; set; }
    public decimal RefundsIssued { get; set; }
    public decimal GrowthVsPreviousPeriod { get; set; }
    public decimal AvgOrderValue { get; set; }
    public decimal AvgRevenuePerUser { get; set; }
    public decimal AvgRevenuePerDP { get; set; }
}

public class RevenueTrendDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
    public decimal PlatformFees { get; set; }
    public decimal DPPayouts { get; set; }
    public decimal GrowthRate { get; set; }
}

public class RevenueByChannelDto
{
    public string Channel { get; set; } = string.Empty; // APP, WEB, API, BIDDING
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
    public decimal Percentage { get; set; }
    public decimal GrowthRate { get; set; }
}

public class RevenueByUserTypeDto
{
    public string UserType { get; set; } = string.Empty; // BC, DBC, EC
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
    public int UserCount { get; set; }
    public decimal AvgOrderValue { get; set; }
    public decimal Percentage { get; set; }
}

public class PaymentAnalyticsDto
{
    public decimal TotalCollected { get; set; }
    public decimal CashCollected { get; set; }
    public decimal OnlineCollected { get; set; }
    public decimal WalletUsed { get; set; }
    public int FailedPayments { get; set; }
    public decimal FailedAmount { get; set; }
    public decimal CollectionRate { get; set; }
    public Dictionary<string, decimal> ByPaymentMethod { get; set; } = new();
}

public class SettlementAnalyticsDto
{
    public decimal TotalSettled { get; set; }
    public decimal PendingSettlement { get; set; }
    public int SettlementCount { get; set; }
    public int PendingCount { get; set; }
    public decimal AvgSettlementTime { get; set; } // hours
    public decimal OnTimeSettlementRate { get; set; }
    public List<SettlementBucketDto> ByTimeBucket { get; set; } = new();
}

public class SettlementBucketDto
{
    public string Bucket { get; set; } = string.Empty; // "Today", "This Week", "This Month"
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

// ============================================
// User Analytics
// ============================================

public class UserAnalyticsDto
{
    public UserAcquisitionDto Acquisition { get; set; } = new();
    public UserRetentionDto Retention { get; set; } = new();
    public UserEngagementDto Engagement { get; set; } = new();
    public UserChurnDto Churn { get; set; } = new();
    public List<UserCohortDto> Cohorts { get; set; } = new();
}

public class UserAcquisitionDto
{
    public int NewUsersTotal { get; set; }
    public int NewDPs { get; set; }
    public int NewDPCMs { get; set; }
    public int NewBCs { get; set; }
    public int NewECs { get; set; }
    public List<AcquisitionTrendDto> Trend { get; set; } = new();
    public Dictionary<string, int> BySource { get; set; } = new(); // Referral, Organic, etc.
}

public class AcquisitionTrendDto
{
    public DateTime Date { get; set; }
    public int Total { get; set; }
    public int DPs { get; set; }
    public int DPCMs { get; set; }
    public int BCs { get; set; }
    public int ECs { get; set; }
}

public class UserRetentionDto
{
    public decimal Day1Retention { get; set; }
    public decimal Day7Retention { get; set; }
    public decimal Day30Retention { get; set; }
    public decimal MonthlyActiveUserRate { get; set; }
    public decimal WeeklyActiveUserRate { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public List<RetentionCohortDto> CohortAnalysis { get; set; } = new();
}

public class RetentionCohortDto
{
    public string CohortWeek { get; set; } = string.Empty;
    public int CohortSize { get; set; }
    public List<decimal> WeeklyRetention { get; set; } = new(); // Week 1, 2, 3, etc.
}

public class UserEngagementDto
{
    public decimal AvgDeliveriesPerDP { get; set; }
    public decimal AvgOrdersPerBC { get; set; }
    public decimal AvgSessionsPerUser { get; set; }
    public decimal AvgSessionDuration { get; set; } // minutes
    public int PowerUsers { get; set; } // Users with high activity
    public int CasualUsers { get; set; }
    public int DormantUsers { get; set; }
}

public class UserChurnDto
{
    public int ChurnedThisMonth { get; set; }
    public decimal ChurnRate { get; set; }
    public int AtRiskUsers { get; set; }
    public Dictionary<string, int> ChurnByRole { get; set; } = new();
    public List<ChurnReasonDto> TopChurnReasons { get; set; } = new();
}

public class ChurnReasonDto
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class UserCohortDto
{
    public string CohortMonth { get; set; } = string.Empty;
    public int Users { get; set; }
    public decimal LifetimeValue { get; set; }
    public int AvgOrdersPerUser { get; set; }
    public decimal RetainedPercentage { get; set; }
}

// ============================================
// DPCM Performance Analytics
// ============================================

public class DPCMPerformanceAnalyticsDto
{
    public List<DPCMRankingDto> Rankings { get; set; } = new();
    public DPCMBenchmarkDto Benchmark { get; set; } = new();
    public List<DPCMTrendDto> Trends { get; set; } = new();
}

public class DPCMRankingDto
{
    public Guid DPCMId { get; set; }
    public string DPCMName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int ManagedDPs { get; set; }
    public int ActiveDPs { get; set; }
    public int TotalDeliveries { get; set; }
    public decimal Revenue { get; set; }
    public decimal Earnings { get; set; }
    public decimal AvgDPRating { get; set; }
    public int Complaints { get; set; }
    public decimal Score { get; set; } // Composite score
}

public class DPCMBenchmarkDto
{
    public decimal AvgDPsPerDPCM { get; set; }
    public decimal AvgDeliveriesPerDPCM { get; set; }
    public decimal AvgRevenuePerDPCM { get; set; }
    public decimal AvgDPRating { get; set; }
    public decimal TopPerformerThreshold { get; set; }
}

public class DPCMTrendDto
{
    public DateTime Date { get; set; }
    public int TotalDPCMs { get; set; }
    public int ActiveDPCMs { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCommissions { get; set; }
}

// ============================================
// Complaint Analytics
// ============================================

public class ComplaintAnalyticsDto
{
    public ComplaintSummaryDto Summary { get; set; } = new();
    public List<ComplaintTrendDto> Trend { get; set; } = new();
    public List<ComplaintByCategoryDto> ByCategory { get; set; } = new();
    public List<ComplaintBySourceDto> BySource { get; set; } = new();
    public ResolutionAnalyticsDto Resolution { get; set; } = new();
    public SLAAnalyticsDto SLA { get; set; } = new();
}

public class ComplaintSummaryDto
{
    public int TotalComplaints { get; set; }
    public int Open { get; set; }
    public int InProgress { get; set; }
    public int Resolved { get; set; }
    public int Escalated { get; set; }
    public decimal ResolutionRate { get; set; }
    public decimal AvgResolutionTime { get; set; } // hours
    public decimal CustomerSatisfactionRate { get; set; }
}

public class ComplaintTrendDto
{
    public DateTime Date { get; set; }
    public int NewComplaints { get; set; }
    public int Resolved { get; set; }
    public int Escalated { get; set; }
    public decimal ResolutionRate { get; set; }
}

public class ComplaintByCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
    public decimal AvgResolutionTime { get; set; }
    public decimal EscalationRate { get; set; }
}

public class ComplaintBySourceDto
{
    public string Source { get; set; } = string.Empty; // BC, DP, DPCM, EC
    public int Count { get; set; }
    public decimal Percentage { get; set; }
    public List<string> TopIssues { get; set; } = new();
}

public class ResolutionAnalyticsDto
{
    public decimal AvgFirstResponseTime { get; set; } // hours
    public decimal AvgResolutionTime { get; set; }
    public int ResolvedWithin24Hours { get; set; }
    public int ResolvedWithin48Hours { get; set; }
    public int ResolvedBeyond48Hours { get; set; }
    public Dictionary<string, int> ByResolutionType { get; set; } = new(); // Refund, Replacement, etc.
}

public class SLAAnalyticsDto
{
    public decimal SLACompliance { get; set; }
    public int SLABreaches { get; set; }
    public int NearSLABreaches { get; set; }
    public List<SLABreachByCategoryDto> BreachesByCategory { get; set; } = new();
}

public class SLABreachByCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public int Breaches { get; set; }
    public decimal AvgBreachTime { get; set; } // hours over SLA
}

// ============================================
// Real-time Dashboard
// ============================================

public class RealTimeMetricsDto
{
    public DateTime Timestamp { get; set; }
    public int ActiveDeliveries { get; set; }
    public int OnlineDPs { get; set; }
    public int WaitingOrders { get; set; }
    public decimal AvgWaitTime { get; set; }
    public int DeliveriesLastHour { get; set; }
    public decimal RevenueLastHour { get; set; }
    public List<LiveOrderDto> RecentOrders { get; set; } = new();
    public List<ActiveAlertDto> ActiveAlerts { get; set; } = new();
}

public class LiveOrderDto
{
    public Guid DeliveryId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PickupCity { get; set; } = string.Empty;
    public string DropCity { get; set; } = string.Empty;
    public int MinutesSinceCreated { get; set; }
    public string? DPName { get; set; }
}

public class ActiveAlertDto
{
    public string Severity { get; set; } = string.Empty; // INFO, WARNING, CRITICAL
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
    public string ActionUrl { get; set; } = string.Empty;
}

// ============================================
// Comparison Analytics
// ============================================

public class ComparisonAnalyticsDto
{
    public ComparisonPeriodDto CurrentPeriod { get; set; } = new();
    public ComparisonPeriodDto PreviousPeriod { get; set; } = new();
    public Dictionary<string, decimal> Changes { get; set; } = new();
    public List<MetricChangeDto> KeyMetricChanges { get; set; } = new();
}

public class ComparisonPeriodDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Deliveries { get; set; }
    public decimal Revenue { get; set; }
    public int NewUsers { get; set; }
    public decimal AvgOrderValue { get; set; }
    public decimal OnTimeRate { get; set; }
    public int Complaints { get; set; }
}

public class MetricChangeDto
{
    public string MetricName { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal ChangePercentage { get; set; }
    public string Trend { get; set; } = string.Empty; // UP, DOWN, STABLE
    public bool IsPositive { get; set; }
}
