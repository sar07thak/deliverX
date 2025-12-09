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
