using System;
using System.Collections.Generic;

namespace DeliveryDost.Web.ViewModels.Dashboard;

public class DpDashboardViewModel
{
    public bool IsOnline { get; set; }
    public decimal TodayEarnings { get; set; }
    public decimal WeekEarnings { get; set; }
    public decimal WalletBalance { get; set; }
    public int TodayDeliveries { get; set; }
    public int ActiveDeliveries { get; set; }
    public decimal Rating { get; set; }
    public int TotalDeliveries { get; set; }
    public List<RecentDeliveryItem> RecentDeliveries { get; set; } = new();
    public List<ChartDataPoint> WeeklyEarnings { get; set; } = new();

    // Registration Status
    public bool IsRegistered { get; set; }
    public string RegistrationStatus { get; set; } = "NOT_STARTED";
    public string? KycStatus { get; set; }
}

public class DpcmDashboardViewModel
{
    public int TotalDPs { get; set; }
    public int ActiveDPs { get; set; }
    public int PendingKyc { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public int TotalDeliveries { get; set; }
    public int MonthDeliveries { get; set; }
    public decimal AvgRating { get; set; }
    public int OpenComplaints { get; set; }
    public List<DpSummaryItem> TopPerformers { get; set; } = new();
    public List<DpSummaryItem> RecentDPs { get; set; } = new();
    public List<ChartDataPoint> MonthlyDeliveries { get; set; } = new();
    public List<ChartDataPoint> RevenueByMonth { get; set; } = new();
    public List<ComplaintSummaryItem> RecentComplaints { get; set; } = new();

    // Registration Status
    public bool IsRegistered { get; set; }
    public string RegistrationStatus { get; set; } = "NOT_STARTED";
    public string? KycStatus { get; set; }
}

public class ComplaintSummaryItem
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string StatusBadgeClass => Status switch
    {
        "RESOLVED" => "bg-success", "IN_PROGRESS" => "bg-info", "ESCALATED" => "bg-danger", _ => "bg-warning"
    };
}

public class AdminDashboardViewModel
{
    // Users
    public int TotalUsers { get; set; }
    public int TotalDPs { get; set; }
    public int TotalDPCMs { get; set; }
    public int TotalRequesters { get; set; }
    // Deliveries
    public int TotalDeliveries { get; set; }
    public int TodayDeliveries { get; set; }
    public int ActiveDeliveries { get; set; }
    public decimal AvgDeliveryTime { get; set; }
    // Revenue
    public decimal TotalRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    // Support
    public int OpenComplaints { get; set; }
    public int PendingKyc { get; set; }
    // Charts
    public List<ChartDataPoint> DeliveriesByDay { get; set; } = new();
    public List<ChartDataPoint> RevenueByMonth { get; set; } = new();
    public Dictionary<string, int> DeliveriesByStatus { get; set; } = new();
    public Dictionary<string, int> UsersByRole { get; set; } = new();
}

public class ConsumerDashboardViewModel
{
    public decimal WalletBalance { get; set; }
    public int TotalDeliveries { get; set; }
    public int ActiveDeliveries { get; set; }
    public int CompletedDeliveries { get; set; }
    public decimal TotalSpent { get; set; }
    public List<RecentDeliveryItem> RecentDeliveries { get; set; } = new();

    // Registration Status
    public bool IsRegistered { get; set; }
    public string RegistrationStatus { get; set; } = "NOT_STARTED";
}

public class BusinessDashboardViewModel : ConsumerDashboardViewModel
{
    public int MonthDeliveries { get; set; }
    public decimal MonthSpent { get; set; }
    public List<ChartDataPoint> DeliveriesByMonth { get; set; } = new();
    public string? KycStatus { get; set; }
}

public class RecentDeliveryItem
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DropAddress { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public string StatusBadgeClass => Status switch
    {
        "DELIVERED" => "bg-success", "IN_TRANSIT" => "bg-info", "CANCELLED" => "bg-danger", _ => "bg-warning"
    };
}

public class DpSummaryItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Deliveries { get; set; }
    public decimal Rating { get; set; }
    public decimal Earnings { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
