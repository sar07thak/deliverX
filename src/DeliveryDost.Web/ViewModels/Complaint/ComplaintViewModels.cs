using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeliveryDost.Web.ViewModels.Complaint;

public class CreateComplaintViewModel
{
    [Required]
    public Guid DeliveryId { get; set; }
    public string? DeliveryNumber { get; set; }

    public Guid? AgainstId { get; set; }
    public string? AgainstType { get; set; }
    public string? AgainstName { get; set; }

    [Required(ErrorMessage = "Please select a category")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subject is required")]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public static List<SelectOption> Categories => new()
    {
        new("DAMAGE", "Package Damage"),
        new("THEFT", "Package Theft/Missing"),
        new("DELAY", "Delivery Delay"),
        new("BEHAVIOR", "Unprofessional Behavior"),
        new("FRAUD", "Fraud/Scam"),
        new("OTHER", "Other Issue")
    };
}

public class ComplaintListViewModel
{
    public List<ComplaintItemViewModel> Complaints { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string? StatusFilter { get; set; }
    public string? CategoryFilter { get; set; }
    public string ViewMode { get; set; } = "user"; // user, inspector, admin

    public static List<SelectOption> StatusOptions => new()
    {
        new("", "All Statuses"),
        new("OPEN", "Open"),
        new("IN_PROGRESS", "In Progress"),
        new("RESOLVED", "Resolved"),
        new("CLOSED", "Closed"),
        new("REJECTED", "Rejected")
    };
}

public class ComplaintItemViewModel
{
    public Guid Id { get; set; }
    public string ComplaintNumber { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public DateTime CreatedAt { get; set; }

    public string StatusBadgeClass => Status switch
    {
        "OPEN" => "bg-danger",
        "IN_PROGRESS" => "bg-warning text-dark",
        "RESOLVED" => "bg-success",
        "CLOSED" => "bg-secondary",
        "REJECTED" => "bg-dark",
        _ => "bg-secondary"
    };

    public string SeverityBadgeClass => Severity switch
    {
        "CRITICAL" => "bg-danger",
        "HIGH" => "bg-warning text-dark",
        "MEDIUM" => "bg-info",
        "LOW" => "bg-secondary",
        _ => "bg-secondary"
    };
}

public class ComplaintDetailsViewModel
{
    public Guid Id { get; set; }
    public string ComplaintNumber { get; set; } = string.Empty;
    public Guid DeliveryId { get; set; }
    public string? DeliveryNumber { get; set; }
    public string? RaisedByName { get; set; }
    public string RaisedByType { get; set; } = string.Empty;
    public string? AgainstName { get; set; }
    public string? AgainstType { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Resolution { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<EvidenceViewModel> Evidences { get; set; } = new();
    public List<CommentViewModel> Comments { get; set; } = new();

    public bool CanAddComment => Status is "OPEN" or "IN_PROGRESS";
    public bool CanResolve => Status == "IN_PROGRESS";
    public bool IsInspectorView { get; set; }

    public string StatusBadgeClass => Status switch
    {
        "OPEN" => "bg-danger",
        "IN_PROGRESS" => "bg-warning text-dark",
        "RESOLVED" => "bg-success",
        "CLOSED" => "bg-secondary",
        _ => "bg-secondary"
    };
}

public class EvidenceViewModel
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class CommentViewModel
{
    public Guid Id { get; set; }
    public string? AuthorName { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InspectorDashboardViewModel
{
    public int OpenCases { get; set; }
    public int InProgressCases { get; set; }
    public int ResolvedToday { get; set; }
    public decimal AvgResolutionHours { get; set; }
    public List<ComplaintItemViewModel> AssignedComplaints { get; set; } = new();
    public List<ComplaintItemViewModel> UnassignedComplaints { get; set; } = new();
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public Dictionary<string, int> BySeverity { get; set; } = new();
}

public class ResolveComplaintViewModel
{
    public Guid ComplaintId { get; set; }
    public string ComplaintNumber { get; set; } = string.Empty;

    [Required]
    public string Resolution { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string ResolutionNotes { get; set; } = string.Empty;

    public static List<SelectOption> Resolutions => new()
    {
        new("REFUND", "Issue Refund"),
        new("REPLACEMENT", "Replacement Delivery"),
        new("WARNING", "Warning to Party"),
        new("SUSPENSION", "Suspend Account"),
        new("NO_ACTION", "No Action Required")
    };
}

public class SelectOption
{
    public string Value { get; set; }
    public string Text { get; set; }
    public SelectOption(string value, string text) { Value = value; Text = text; }
}
