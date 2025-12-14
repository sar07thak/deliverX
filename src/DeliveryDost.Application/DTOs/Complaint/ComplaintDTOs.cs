using System;
using System.Collections.Generic;

namespace DeliveryDost.Application.DTOs.Complaint;

public class CreateComplaintRequest
{
    public Guid DeliveryId { get; set; }
    public Guid? AgainstId { get; set; }
    public string? AgainstType { get; set; }
    public string Category { get; set; } = string.Empty; // DAMAGE, THEFT, DELAY, BEHAVIOR, FRAUD, OTHER
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CreateComplaintResponse
{
    public bool IsSuccess { get; set; }
    public Guid? ComplaintId { get; set; }
    public string? ComplaintNumber { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
}

public class ComplaintDto
{
    public Guid Id { get; set; }
    public string ComplaintNumber { get; set; } = string.Empty;
    public Guid DeliveryId { get; set; }
    public string? DeliveryTrackingNumber { get; set; }
    public Guid RaisedById { get; set; }
    public string? RaisedByName { get; set; }
    public string RaisedByType { get; set; } = string.Empty;
    public Guid? AgainstId { get; set; }
    public string? AgainstName { get; set; }
    public string? AgainstType { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Resolution { get; set; }
    public string? ResolutionNotes { get; set; }
    public Guid? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<EvidenceDto> Evidences { get; set; } = new();
    public List<CommentDto> Comments { get; set; } = new();
}

public class EvidenceDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddEvidenceRequest
{
    public string Type { get; set; } = string.Empty; // PHOTO, VIDEO, DOCUMENT, AUDIO
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AddCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}

public class AssignComplaintRequest
{
    public Guid InspectorId { get; set; }
}

public class ResolveComplaintRequest
{
    public string Resolution { get; set; } = string.Empty; // REFUND, REPLACEMENT, WARNING, SUSPENSION, NO_ACTION
    public string ResolutionNotes { get; set; } = string.Empty;
}

public class UpdateSeverityRequest
{
    public string Severity { get; set; } = string.Empty; // LOW, MEDIUM, HIGH, CRITICAL
}

public class GetComplaintsRequest
{
    public string? Status { get; set; }
    public string? Category { get; set; }
    public string? Severity { get; set; }
    public Guid? AssignedToId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetComplaintsResponse
{
    public List<ComplaintDto> Complaints { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class ComplaintStatsDto
{
    public int TotalComplaints { get; set; }
    public int OpenComplaints { get; set; }
    public int InProgressComplaints { get; set; }
    public int ResolvedComplaints { get; set; }
    public int ClosedComplaints { get; set; }
    public decimal AverageResolutionTimeHours { get; set; }
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public Dictionary<string, int> BySeverity { get; set; } = new();
}

// Inspector DTOs
public class InspectorDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string InspectorCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Zone { get; set; }
    public int ActiveCases { get; set; }
    public int TotalCasesHandled { get; set; }
    public decimal ResolutionRate { get; set; }
    public decimal AverageResolutionTimeHours { get; set; }
    public bool IsAvailable { get; set; }
}

public class CreateInspectorRequest
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Zone { get; set; }
}

// Field Visit DTOs
public class ScheduleFieldVisitRequest
{
    public Guid ComplaintId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Notes { get; set; }
}

public class FieldVisitDto
{
    public Guid Id { get; set; }
    public Guid ComplaintId { get; set; }
    public string? ComplaintNumber { get; set; }
    public Guid InspectorId { get; set; }
    public string? InspectorName { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<FieldVisitEvidenceDto> Evidences { get; set; } = new();
}

public class FieldVisitEvidenceDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Description { get; set; }
    public DateTime CapturedAt { get; set; }
}

public class AddFieldVisitEvidenceRequest
{
    public string Type { get; set; } = string.Empty; // PHOTO, GPS_LOCATION, SIGNATURE, AUDIO, VIDEO
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Description { get; set; }
}

public class CompleteFieldVisitRequest
{
    public string Notes { get; set; } = string.Empty;
}

public class CancelFieldVisitRequest
{
    public string Reason { get; set; } = string.Empty;
}

// Investigation Report DTOs
public class SubmitInvestigationReportRequest
{
    public Guid ComplaintId { get; set; }
    public string Findings { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty; // VALID, INVALID, PARTIALLY_VALID, INCONCLUSIVE
    public string? VerdictReason { get; set; }
    public string? RecommendedAction { get; set; } // REFUND, COMPENSATION, WARNING, SUSPENSION, NO_ACTION
    public decimal? CompensationAmount { get; set; }
    public string? PenaltyType { get; set; } // WARNING, FINE, SUSPENSION, TERMINATION
    public decimal? PenaltyAmount { get; set; }
    public Guid? PenaltyAppliedToId { get; set; }
}

public class InvestigationReportDto
{
    public Guid Id { get; set; }
    public Guid ComplaintId { get; set; }
    public string? ComplaintNumber { get; set; }
    public Guid InspectorId { get; set; }
    public string? InspectorName { get; set; }
    public string Findings { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty;
    public string? VerdictReason { get; set; }
    public string? RecommendedAction { get; set; }
    public decimal? CompensationAmount { get; set; }
    public string? PenaltyType { get; set; }
    public decimal? PenaltyAmount { get; set; }
    public Guid? PenaltyAppliedToId { get; set; }
    public string? PenaltyAppliedToName { get; set; }
    public bool IsApproved { get; set; }
    public Guid? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class ApproveInvestigationReportRequest
{
    public bool Approved { get; set; }
    public string? Comments { get; set; }
}

// SLA DTOs
public class SLABreachDto
{
    public Guid Id { get; set; }
    public Guid ComplaintId { get; set; }
    public string? ComplaintNumber { get; set; }
    public string BreachType { get; set; } = string.Empty;
    public int ExpectedHours { get; set; }
    public int ActualHours { get; set; }
    public DateTime BreachedAt { get; set; }
    public bool IsEscalated { get; set; }
    public Guid? EscalatedToId { get; set; }
    public string? EscalatedToName { get; set; }
    public DateTime? EscalatedAt { get; set; }
}

public class SLAStatusDto
{
    public Guid ComplaintId { get; set; }
    public int ResponseTimeHours { get; set; }
    public int ResolutionTimeHours { get; set; }
    public int ElapsedHours { get; set; }
    public bool ResponseSLABreached { get; set; }
    public bool ResolutionSLABreached { get; set; }
    public int HoursUntilResponseBreach { get; set; }
    public int HoursUntilResolutionBreach { get; set; }
}

// Auto-assignment DTOs
public class AutoAssignmentResult
{
    public bool IsSuccess { get; set; }
    public Guid? AssignedInspectorId { get; set; }
    public string? InspectorName { get; set; }
    public string? Message { get; set; }
    public string? Reason { get; set; } // Why this inspector was chosen
}
