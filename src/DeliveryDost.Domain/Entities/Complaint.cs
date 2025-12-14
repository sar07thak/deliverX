using System;
using System.Collections.Generic;

namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Complaint entity for tracking issues raised by users
/// </summary>
public class Complaint
{
    public Guid Id { get; set; }
    public string ComplaintNumber { get; set; } = string.Empty; // CMP-YYYYMMDD-XXXX
    public Guid DeliveryId { get; set; }
    public Guid RaisedById { get; set; }
    public string RaisedByType { get; set; } = string.Empty; // EC, BC, DP
    public Guid? AgainstId { get; set; }
    public string? AgainstType { get; set; } // DP, EC, BC
    public string Category { get; set; } = string.Empty; // DAMAGE, THEFT, DELAY, BEHAVIOR, FRAUD, OTHER
    public string Severity { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH, CRITICAL
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "OPEN"; // OPEN, ASSIGNED, IN_PROGRESS, RESOLVED, CLOSED, REJECTED
    public string? Resolution { get; set; }
    public string? ResolutionNotes { get; set; }
    public Guid? AssignedToId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Delivery? Delivery { get; set; }
    public User? RaisedBy { get; set; }
    public User? Against { get; set; }
    public User? AssignedTo { get; set; }
    public ICollection<ComplaintEvidence> Evidences { get; set; } = new List<ComplaintEvidence>();
    public ICollection<ComplaintComment> Comments { get; set; } = new List<ComplaintComment>();
}

/// <summary>
/// Evidence attached to a complaint
/// </summary>
public class ComplaintEvidence
{
    public Guid Id { get; set; }
    public Guid ComplaintId { get; set; }
    public string Type { get; set; } = string.Empty; // PHOTO, VIDEO, DOCUMENT, AUDIO
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid UploadedById { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Complaint? Complaint { get; set; }
    public User? UploadedBy { get; set; }
}

/// <summary>
/// Comments on a complaint (investigation notes)
/// </summary>
public class ComplaintComment
{
    public Guid Id { get; set; }
    public Guid ComplaintId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; } // Only visible to staff
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Complaint? Complaint { get; set; }
    public User? Author { get; set; }
}

/// <summary>
/// Inspector entity - staff who handle complaints
/// </summary>
public class Inspector
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string InspectorCode { get; set; } = string.Empty; // INS-XXXX
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Zone { get; set; } // Assigned zone/area
    public int ActiveCases { get; set; }
    public int TotalCasesHandled { get; set; }
    public decimal ResolutionRate { get; set; }
    public decimal AverageResolutionTimeHours { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}

/// <summary>
/// SLA configuration for complaint resolution
/// </summary>
public class ComplaintSLAConfig
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int ResponseTimeHours { get; set; }
    public int ResolutionTimeHours { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Field visit scheduled by inspector for investigation
/// </summary>
public class FieldVisit
{
    public Guid Id { get; set; }
    public Guid ComplaintId { get; set; }
    public Guid InspectorId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "SCHEDULED"; // SCHEDULED, IN_PROGRESS, COMPLETED, CANCELLED
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Complaint? Complaint { get; set; }
    public Inspector? Inspector { get; set; }
    public ICollection<FieldVisitEvidence> Evidences { get; set; } = new List<FieldVisitEvidence>();
}

/// <summary>
/// Evidence collected during field visit (photos, GPS, etc.)
/// </summary>
public class FieldVisitEvidence
{
    public Guid Id { get; set; }
    public Guid FieldVisitId { get; set; }
    public string Type { get; set; } = string.Empty; // PHOTO, GPS_LOCATION, SIGNATURE, AUDIO, VIDEO
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Description { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public FieldVisit? FieldVisit { get; set; }
}

/// <summary>
/// Investigation report and verdict for a complaint
/// </summary>
public class InvestigationReport
{
    public Guid Id { get; set; }
    public Guid ComplaintId { get; set; }
    public Guid InspectorId { get; set; }
    public string Findings { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty; // VALID, INVALID, PARTIALLY_VALID, INCONCLUSIVE
    public string? VerdictReason { get; set; }
    public string? RecommendedAction { get; set; } // REFUND, COMPENSATION, WARNING, SUSPENSION, NO_ACTION
    public decimal? CompensationAmount { get; set; }
    public string? PenaltyType { get; set; } // WARNING, FINE, SUSPENSION, TERMINATION
    public decimal? PenaltyAmount { get; set; }
    public Guid? PenaltyAppliedToId { get; set; } // User who gets penalized
    public bool IsApproved { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Complaint? Complaint { get; set; }
    public Inspector? Inspector { get; set; }
    public User? ApprovedBy { get; set; }
}

/// <summary>
/// SLA breach tracking for complaints
/// </summary>
public class SLABreach
{
    public Guid Id { get; set; }
    public Guid ComplaintId { get; set; }
    public string BreachType { get; set; } = string.Empty; // RESPONSE_TIME, RESOLUTION_TIME
    public int ExpectedHours { get; set; }
    public int ActualHours { get; set; }
    public DateTime BreachedAt { get; set; } = DateTime.UtcNow;
    public bool IsEscalated { get; set; }
    public Guid? EscalatedToId { get; set; }
    public DateTime? EscalatedAt { get; set; }

    // Navigation
    public Complaint? Complaint { get; set; }
    public User? EscalatedTo { get; set; }
}
