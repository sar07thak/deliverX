using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.DTOs.Complaint;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class ComplaintService : IComplaintService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ComplaintService> _logger;

    public ComplaintService(ApplicationDbContext context, ILogger<ComplaintService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreateComplaintResponse> CreateComplaintAsync(
        Guid raiserId,
        string raiserType,
        CreateComplaintRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating complaint for delivery {DeliveryId} by {RaiserId}", request.DeliveryId, raiserId);

        // Verify delivery exists
        var delivery = await _context.Deliveries.FindAsync(new object[] { request.DeliveryId }, ct);
        if (delivery == null)
        {
            return new CreateComplaintResponse
            {
                IsSuccess = false,
                ErrorCode = "DELIVERY_NOT_FOUND",
                Message = "Delivery not found"
            };
        }

        // Generate complaint number
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.Set<Complaint>()
            .CountAsync(c => c.ComplaintNumber.StartsWith($"CMP-{today}"), ct);
        var complaintNumber = $"CMP-{today}-{(count + 1):D4}";

        // Determine severity based on category
        var severity = request.Category switch
        {
            "THEFT" or "FRAUD" => "HIGH",
            "DAMAGE" => "MEDIUM",
            "DELAY" or "BEHAVIOR" => "LOW",
            _ => "MEDIUM"
        };

        var complaint = new Complaint
        {
            Id = Guid.NewGuid(),
            ComplaintNumber = complaintNumber,
            DeliveryId = request.DeliveryId,
            RaisedById = raiserId,
            RaisedByType = raiserType,
            AgainstId = request.AgainstId,
            AgainstType = request.AgainstType,
            Category = request.Category,
            Severity = severity,
            Subject = request.Subject,
            Description = request.Description,
            Status = "OPEN",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<Complaint>().Add(complaint);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Complaint {ComplaintNumber} created successfully", complaintNumber);

        return new CreateComplaintResponse
        {
            IsSuccess = true,
            ComplaintId = complaint.Id,
            ComplaintNumber = complaint.ComplaintNumber,
            Message = "Complaint submitted successfully"
        };
    }

    public async Task<ComplaintDto?> GetComplaintAsync(Guid complaintId, CancellationToken ct = default)
    {
        var complaint = await _context.Set<Complaint>()
            .Include(c => c.Evidences)
            .Include(c => c.Comments)
            .FirstOrDefaultAsync(c => c.Id == complaintId, ct);

        if (complaint == null) return null;

        return MapToDto(complaint);
    }

    public async Task<ComplaintDto?> GetComplaintByNumberAsync(string complaintNumber, CancellationToken ct = default)
    {
        var complaint = await _context.Set<Complaint>()
            .Include(c => c.Evidences)
            .Include(c => c.Comments)
            .FirstOrDefaultAsync(c => c.ComplaintNumber == complaintNumber, ct);

        if (complaint == null) return null;

        return MapToDto(complaint);
    }

    public async Task<GetComplaintsResponse> GetComplaintsAsync(
        GetComplaintsRequest request,
        Guid? userId = null,
        string? userRole = null,
        CancellationToken ct = default)
    {
        var query = _context.Set<Complaint>().AsQueryable();

        // Filter by user if not admin/inspector
        if (userId.HasValue && userRole != "SA" && userRole != "INSPECTOR")
        {
            query = query.Where(c => c.RaisedById == userId.Value || c.AgainstId == userId.Value);
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(c => c.Status == request.Status);
        }

        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(c => c.Category == request.Category);
        }

        if (!string.IsNullOrEmpty(request.Severity))
        {
            query = query.Where(c => c.Severity == request.Severity);
        }

        if (request.AssignedToId.HasValue)
        {
            query = query.Where(c => c.AssignedToId == request.AssignedToId);
        }

        var totalCount = await query.CountAsync(ct);

        var complaints = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(c => c.Evidences)
            .ToListAsync(ct);

        return new GetComplaintsResponse
        {
            Complaints = complaints.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };
    }

    public async Task<bool> AddEvidenceAsync(Guid complaintId, Guid uploaderId, AddEvidenceRequest request, CancellationToken ct = default)
    {
        var complaint = await _context.Set<Complaint>().FindAsync(new object[] { complaintId }, ct);
        if (complaint == null) return false;

        var evidence = new ComplaintEvidence
        {
            Id = Guid.NewGuid(),
            ComplaintId = complaintId,
            Type = request.Type,
            FileName = request.FileName,
            FileUrl = request.FileUrl,
            Description = request.Description,
            UploadedById = uploaderId,
            UploadedAt = DateTime.UtcNow
        };

        _context.Set<ComplaintEvidence>().Add(evidence);
        complaint.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> AddCommentAsync(Guid complaintId, Guid authorId, AddCommentRequest request, CancellationToken ct = default)
    {
        var complaint = await _context.Set<Complaint>().FindAsync(new object[] { complaintId }, ct);
        if (complaint == null) return false;

        var comment = new ComplaintComment
        {
            Id = Guid.NewGuid(),
            ComplaintId = complaintId,
            AuthorId = authorId,
            Content = request.Content,
            IsInternal = request.IsInternal,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ComplaintComment>().Add(comment);
        complaint.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> AssignComplaintAsync(Guid complaintId, Guid inspectorId, CancellationToken ct = default)
    {
        var complaint = await _context.Set<Complaint>().FindAsync(new object[] { complaintId }, ct);
        if (complaint == null) return false;

        var inspector = await _context.Set<Inspector>().FindAsync(new object[] { inspectorId }, ct);
        if (inspector == null) return false;

        complaint.AssignedToId = inspector.UserId;
        complaint.AssignedAt = DateTime.UtcNow;
        complaint.Status = "ASSIGNED";
        complaint.UpdatedAt = DateTime.UtcNow;

        inspector.ActiveCases++;
        inspector.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Complaint {ComplaintId} assigned to inspector {InspectorId}", complaintId, inspectorId);
        return true;
    }

    public async Task<bool> UpdateStatusAsync(Guid complaintId, string status, CancellationToken ct = default)
    {
        var complaint = await _context.Set<Complaint>().FindAsync(new object[] { complaintId }, ct);
        if (complaint == null) return false;

        complaint.Status = status;
        complaint.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateSeverityAsync(Guid complaintId, string severity, CancellationToken ct = default)
    {
        var complaint = await _context.Set<Complaint>().FindAsync(new object[] { complaintId }, ct);
        if (complaint == null) return false;

        complaint.Severity = severity;
        complaint.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ResolveComplaintAsync(Guid complaintId, Guid resolverId, ResolveComplaintRequest request, CancellationToken ct = default)
    {
        var complaint = await _context.Set<Complaint>().FindAsync(new object[] { complaintId }, ct);
        if (complaint == null) return false;

        complaint.Status = "RESOLVED";
        complaint.Resolution = request.Resolution;
        complaint.ResolutionNotes = request.ResolutionNotes;
        complaint.ResolvedAt = DateTime.UtcNow;
        complaint.UpdatedAt = DateTime.UtcNow;

        // Update inspector stats
        if (complaint.AssignedToId.HasValue)
        {
            var inspector = await _context.Set<Inspector>()
                .FirstOrDefaultAsync(i => i.UserId == complaint.AssignedToId, ct);
            if (inspector != null)
            {
                inspector.ActiveCases = Math.Max(0, inspector.ActiveCases - 1);
                inspector.TotalCasesHandled++;
                inspector.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Complaint {ComplaintId} resolved with {Resolution}", complaintId, request.Resolution);
        return true;
    }

    public async Task<bool> CloseComplaintAsync(Guid complaintId, CancellationToken ct = default)
    {
        var complaint = await _context.Set<Complaint>().FindAsync(new object[] { complaintId }, ct);
        if (complaint == null) return false;

        complaint.Status = "CLOSED";
        complaint.ClosedAt = DateTime.UtcNow;
        complaint.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RejectComplaintAsync(Guid complaintId, string reason, CancellationToken ct = default)
    {
        var complaint = await _context.Set<Complaint>().FindAsync(new object[] { complaintId }, ct);
        if (complaint == null) return false;

        complaint.Status = "REJECTED";
        complaint.ResolutionNotes = reason;
        complaint.ClosedAt = DateTime.UtcNow;
        complaint.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ComplaintStatsDto> GetComplaintStatsAsync(Guid? inspectorId = null, CancellationToken ct = default)
    {
        var query = _context.Set<Complaint>().AsQueryable();

        if (inspectorId.HasValue)
        {
            var inspector = await _context.Set<Inspector>().FindAsync(new object[] { inspectorId.Value }, ct);
            if (inspector != null)
            {
                query = query.Where(c => c.AssignedToId == inspector.UserId);
            }
        }

        var complaints = await query.ToListAsync(ct);

        var resolvedComplaints = complaints.Where(c => c.ResolvedAt.HasValue).ToList();
        var avgResolutionTime = resolvedComplaints.Any()
            ? resolvedComplaints.Average(c => (c.ResolvedAt!.Value - c.CreatedAt).TotalHours)
            : 0;

        return new ComplaintStatsDto
        {
            TotalComplaints = complaints.Count,
            OpenComplaints = complaints.Count(c => c.Status == "OPEN"),
            InProgressComplaints = complaints.Count(c => c.Status == "ASSIGNED" || c.Status == "IN_PROGRESS"),
            ResolvedComplaints = complaints.Count(c => c.Status == "RESOLVED"),
            ClosedComplaints = complaints.Count(c => c.Status == "CLOSED" || c.Status == "REJECTED"),
            AverageResolutionTimeHours = (decimal)avgResolutionTime,
            ByCategory = complaints.GroupBy(c => c.Category).ToDictionary(g => g.Key, g => g.Count()),
            BySeverity = complaints.GroupBy(c => c.Severity).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<InspectorDto?> CreateInspectorAsync(CreateInspectorRequest request, CancellationToken ct = default)
    {
        var count = await _context.Set<Inspector>().CountAsync(ct);
        var inspectorCode = $"INS-{(count + 1):D4}";

        var inspector = new Inspector
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            InspectorCode = inspectorCode,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Zone = request.Zone,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<Inspector>().Add(inspector);
        await _context.SaveChangesAsync(ct);

        return MapInspectorToDto(inspector);
    }

    public async Task<InspectorDto?> GetInspectorAsync(Guid inspectorId, CancellationToken ct = default)
    {
        var inspector = await _context.Set<Inspector>().FindAsync(new object[] { inspectorId }, ct);
        return inspector != null ? MapInspectorToDto(inspector) : null;
    }

    public async Task<List<InspectorDto>> GetAvailableInspectorsAsync(CancellationToken ct = default)
    {
        var inspectors = await _context.Set<Inspector>()
            .Where(i => i.IsAvailable)
            .OrderBy(i => i.ActiveCases)
            .ToListAsync(ct);

        return inspectors.Select(MapInspectorToDto).ToList();
    }

    private ComplaintDto MapToDto(Complaint complaint)
    {
        return new ComplaintDto
        {
            Id = complaint.Id,
            ComplaintNumber = complaint.ComplaintNumber,
            DeliveryId = complaint.DeliveryId,
            RaisedById = complaint.RaisedById,
            RaisedByType = complaint.RaisedByType,
            AgainstId = complaint.AgainstId,
            AgainstType = complaint.AgainstType,
            Category = complaint.Category,
            Severity = complaint.Severity,
            Subject = complaint.Subject,
            Description = complaint.Description,
            Status = complaint.Status,
            Resolution = complaint.Resolution,
            ResolutionNotes = complaint.ResolutionNotes,
            AssignedToId = complaint.AssignedToId,
            AssignedAt = complaint.AssignedAt,
            ResolvedAt = complaint.ResolvedAt,
            ClosedAt = complaint.ClosedAt,
            CreatedAt = complaint.CreatedAt,
            UpdatedAt = complaint.UpdatedAt,
            Evidences = complaint.Evidences.Select(e => new EvidenceDto
            {
                Id = e.Id,
                Type = e.Type,
                FileName = e.FileName,
                FileUrl = e.FileUrl,
                Description = e.Description,
                UploadedAt = e.UploadedAt
            }).ToList(),
            Comments = complaint.Comments.Select(c => new CommentDto
            {
                Id = c.Id,
                AuthorId = c.AuthorId,
                Content = c.Content,
                IsInternal = c.IsInternal,
                CreatedAt = c.CreatedAt
            }).ToList()
        };
    }

    private InspectorDto MapInspectorToDto(Inspector inspector)
    {
        return new InspectorDto
        {
            Id = inspector.Id,
            UserId = inspector.UserId,
            InspectorCode = inspector.InspectorCode,
            Name = inspector.Name,
            Email = inspector.Email,
            Phone = inspector.Phone,
            Zone = inspector.Zone,
            ActiveCases = inspector.ActiveCases,
            TotalCasesHandled = inspector.TotalCasesHandled,
            ResolutionRate = inspector.ResolutionRate,
            AverageResolutionTimeHours = inspector.AverageResolutionTimeHours,
            IsAvailable = inspector.IsAvailable
        };
    }

    // ========== AUTO-ASSIGNMENT ==========

    public async Task<AutoAssignmentResult> AutoAssignComplaintAsync(Guid complaintId, CancellationToken ct = default)
    {
        var complaint = await _context.Complaints
            .Include(c => c.Delivery)
            .FirstOrDefaultAsync(c => c.Id == complaintId, ct);

        if (complaint == null)
        {
            return new AutoAssignmentResult { IsSuccess = false, Message = "Complaint not found" };
        }

        if (complaint.AssignedToId.HasValue)
        {
            return new AutoAssignmentResult { IsSuccess = false, Message = "Complaint already assigned" };
        }

        // Find best available inspector based on:
        // 1. Lowest active cases
        // 2. Best resolution rate
        var inspectors = await _context.Inspectors
            .Where(i => i.IsAvailable && i.ActiveCases < 10) // Max 10 active cases per inspector
            .OrderBy(i => i.ActiveCases) // Order by workload
            .ThenByDescending(i => i.ResolutionRate) // Then by success rate
            .ToListAsync(ct);

        if (!inspectors.Any())
        {
            return new AutoAssignmentResult { IsSuccess = false, Message = "No available inspectors" };
        }

        var bestInspector = inspectors.First();

        // Assign the complaint
        complaint.AssignedToId = bestInspector.UserId;
        complaint.AssignedAt = DateTime.UtcNow;
        complaint.Status = "ASSIGNED";
        complaint.UpdatedAt = DateTime.UtcNow;

        bestInspector.ActiveCases++;
        bestInspector.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Auto-assigned complaint {ComplaintId} to inspector {InspectorId}", complaintId, bestInspector.Id);

        return new AutoAssignmentResult
        {
            IsSuccess = true,
            AssignedInspectorId = bestInspector.Id,
            InspectorName = bestInspector.Name,
            Message = "Complaint assigned successfully",
            Reason = $"Best available with {bestInspector.ActiveCases} active cases and {bestInspector.ResolutionRate:P0} resolution rate"
        };
    }

    // ========== FIELD VISIT OPERATIONS ==========

    public async Task<FieldVisitDto?> ScheduleFieldVisitAsync(Guid inspectorId, ScheduleFieldVisitRequest request, CancellationToken ct = default)
    {
        var inspector = await _context.Inspectors.FirstOrDefaultAsync(i => i.UserId == inspectorId, ct);
        if (inspector == null)
        {
            _logger.LogWarning("Inspector not found for user {UserId}", inspectorId);
            return null;
        }

        var complaint = await _context.Complaints.FindAsync(new object[] { request.ComplaintId }, ct);
        if (complaint == null)
        {
            _logger.LogWarning("Complaint not found {ComplaintId}", request.ComplaintId);
            return null;
        }

        var fieldVisit = new FieldVisit
        {
            Id = Guid.NewGuid(),
            ComplaintId = request.ComplaintId,
            InspectorId = inspector.Id,
            ScheduledAt = request.ScheduledAt,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Notes = request.Notes,
            Status = "SCHEDULED",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.FieldVisits.Add(fieldVisit);

        // Update complaint status if needed
        if (complaint.Status == "ASSIGNED")
        {
            complaint.Status = "IN_PROGRESS";
            complaint.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Field visit scheduled for complaint {ComplaintId} at {ScheduledAt}", request.ComplaintId, request.ScheduledAt);

        return MapFieldVisitToDto(fieldVisit, complaint.ComplaintNumber, inspector.Name);
    }

    public async Task<FieldVisitDto?> GetFieldVisitAsync(Guid fieldVisitId, CancellationToken ct = default)
    {
        var visit = await _context.FieldVisits
            .Include(v => v.Complaint)
            .Include(v => v.Inspector)
            .Include(v => v.Evidences)
            .FirstOrDefaultAsync(v => v.Id == fieldVisitId, ct);

        if (visit == null) return null;

        return MapFieldVisitToDto(visit, visit.Complaint?.ComplaintNumber, visit.Inspector?.Name);
    }

    public async Task<List<FieldVisitDto>> GetFieldVisitsForComplaintAsync(Guid complaintId, CancellationToken ct = default)
    {
        var visits = await _context.FieldVisits
            .Include(v => v.Inspector)
            .Include(v => v.Evidences)
            .Where(v => v.ComplaintId == complaintId)
            .OrderByDescending(v => v.ScheduledAt)
            .ToListAsync(ct);

        var complaint = await _context.Complaints.FindAsync(new object[] { complaintId }, ct);

        return visits.Select(v => MapFieldVisitToDto(v, complaint?.ComplaintNumber, v.Inspector?.Name)).ToList();
    }

    public async Task<List<FieldVisitDto>> GetFieldVisitsForInspectorAsync(Guid inspectorId, DateTime? fromDate = null, CancellationToken ct = default)
    {
        var inspector = await _context.Inspectors.FirstOrDefaultAsync(i => i.UserId == inspectorId, ct);
        if (inspector == null) return new List<FieldVisitDto>();

        var query = _context.FieldVisits
            .Include(v => v.Complaint)
            .Include(v => v.Evidences)
            .Where(v => v.InspectorId == inspector.Id);

        if (fromDate.HasValue)
        {
            query = query.Where(v => v.ScheduledAt >= fromDate.Value);
        }

        var visits = await query.OrderByDescending(v => v.ScheduledAt).ToListAsync(ct);

        return visits.Select(v => MapFieldVisitToDto(v, v.Complaint?.ComplaintNumber, inspector.Name)).ToList();
    }

    public async Task<bool> StartFieldVisitAsync(Guid fieldVisitId, decimal latitude, decimal longitude, CancellationToken ct = default)
    {
        var visit = await _context.FieldVisits.FindAsync(new object[] { fieldVisitId }, ct);
        if (visit == null || visit.Status != "SCHEDULED") return false;

        visit.Status = "IN_PROGRESS";
        visit.StartedAt = DateTime.UtcNow;
        visit.Latitude = latitude;
        visit.Longitude = longitude;
        visit.UpdatedAt = DateTime.UtcNow;

        // Add GPS evidence
        var gpsEvidence = new FieldVisitEvidence
        {
            Id = Guid.NewGuid(),
            FieldVisitId = fieldVisitId,
            Type = "GPS_LOCATION",
            Latitude = latitude,
            Longitude = longitude,
            Description = "Field visit start location",
            CapturedAt = DateTime.UtcNow
        };
        _context.FieldVisitEvidences.Add(gpsEvidence);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Field visit {FieldVisitId} started at ({Lat}, {Lng})", fieldVisitId, latitude, longitude);
        return true;
    }

    public async Task<bool> CompleteFieldVisitAsync(Guid fieldVisitId, CompleteFieldVisitRequest request, CancellationToken ct = default)
    {
        var visit = await _context.FieldVisits.FindAsync(new object[] { fieldVisitId }, ct);
        if (visit == null || visit.Status != "IN_PROGRESS") return false;

        visit.Status = "COMPLETED";
        visit.CompletedAt = DateTime.UtcNow;
        visit.Notes = request.Notes;
        visit.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Field visit {FieldVisitId} completed", fieldVisitId);
        return true;
    }

    public async Task<bool> CancelFieldVisitAsync(Guid fieldVisitId, CancelFieldVisitRequest request, CancellationToken ct = default)
    {
        var visit = await _context.FieldVisits.FindAsync(new object[] { fieldVisitId }, ct);
        if (visit == null || visit.Status == "COMPLETED") return false;

        visit.Status = "CANCELLED";
        visit.CancellationReason = request.Reason;
        visit.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Field visit {FieldVisitId} cancelled: {Reason}", fieldVisitId, request.Reason);
        return true;
    }

    public async Task<bool> AddFieldVisitEvidenceAsync(Guid fieldVisitId, AddFieldVisitEvidenceRequest request, CancellationToken ct = default)
    {
        var visit = await _context.FieldVisits.FindAsync(new object[] { fieldVisitId }, ct);
        if (visit == null) return false;

        var evidence = new FieldVisitEvidence
        {
            Id = Guid.NewGuid(),
            FieldVisitId = fieldVisitId,
            Type = request.Type,
            FileName = request.FileName,
            FileUrl = request.FileUrl,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Description = request.Description,
            CapturedAt = DateTime.UtcNow
        };

        _context.FieldVisitEvidences.Add(evidence);
        visit.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Evidence added to field visit {FieldVisitId}: {Type}", fieldVisitId, request.Type);
        return true;
    }

    // ========== INVESTIGATION REPORT OPERATIONS ==========

    public async Task<InvestigationReportDto?> SubmitInvestigationReportAsync(Guid inspectorId, SubmitInvestigationReportRequest request, CancellationToken ct = default)
    {
        var inspector = await _context.Inspectors.FirstOrDefaultAsync(i => i.UserId == inspectorId, ct);
        if (inspector == null)
        {
            _logger.LogWarning("Inspector not found for user {UserId}", inspectorId);
            return null;
        }

        var complaint = await _context.Complaints.FindAsync(new object[] { request.ComplaintId }, ct);
        if (complaint == null)
        {
            _logger.LogWarning("Complaint not found {ComplaintId}", request.ComplaintId);
            return null;
        }

        // Check if report already exists
        var existingReport = await _context.InvestigationReports.FirstOrDefaultAsync(r => r.ComplaintId == request.ComplaintId, ct);
        if (existingReport != null)
        {
            _logger.LogWarning("Investigation report already exists for complaint {ComplaintId}", request.ComplaintId);
            return null;
        }

        var report = new InvestigationReport
        {
            Id = Guid.NewGuid(),
            ComplaintId = request.ComplaintId,
            InspectorId = inspector.Id,
            Findings = request.Findings,
            Verdict = request.Verdict,
            VerdictReason = request.VerdictReason,
            RecommendedAction = request.RecommendedAction,
            CompensationAmount = request.CompensationAmount,
            PenaltyType = request.PenaltyType,
            PenaltyAmount = request.PenaltyAmount,
            PenaltyAppliedToId = request.PenaltyAppliedToId,
            IsApproved = false,
            SubmittedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.InvestigationReports.Add(report);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Investigation report submitted for complaint {ComplaintId} with verdict {Verdict}", request.ComplaintId, request.Verdict);

        return MapReportToDto(report, complaint.ComplaintNumber, inspector.Name, null, null);
    }

    public async Task<InvestigationReportDto?> GetInvestigationReportAsync(Guid complaintId, CancellationToken ct = default)
    {
        var report = await _context.InvestigationReports
            .Include(r => r.Complaint)
            .Include(r => r.Inspector)
            .Include(r => r.ApprovedBy)
            .FirstOrDefaultAsync(r => r.ComplaintId == complaintId, ct);

        if (report == null) return null;

        string? penaltyUserName = null;
        if (report.PenaltyAppliedToId.HasValue)
        {
            var user = await _context.Users.FindAsync(new object[] { report.PenaltyAppliedToId.Value }, ct);
            penaltyUserName = user?.FullName;
        }

        return MapReportToDto(report, report.Complaint?.ComplaintNumber, report.Inspector?.Name, report.ApprovedBy?.FullName, penaltyUserName);
    }

    public async Task<bool> ApproveInvestigationReportAsync(Guid reportId, Guid approverId, ApproveInvestigationReportRequest request, CancellationToken ct = default)
    {
        var report = await _context.InvestigationReports
            .Include(r => r.Complaint)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);

        if (report == null) return false;

        report.IsApproved = request.Approved;
        report.ApprovedById = approverId;
        report.ApprovedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        if (request.Approved && report.Complaint != null)
        {
            // Resolve the complaint based on the approved verdict
            report.Complaint.Status = "RESOLVED";
            report.Complaint.Resolution = report.RecommendedAction;
            report.Complaint.ResolutionNotes = $"Verdict: {report.Verdict}. {report.VerdictReason}";
            report.Complaint.ResolvedAt = DateTime.UtcNow;
            report.Complaint.UpdatedAt = DateTime.UtcNow;

            // Update inspector stats
            var inspector = await _context.Inspectors.FindAsync(new object[] { report.InspectorId }, ct);
            if (inspector != null)
            {
                inspector.ActiveCases = Math.Max(0, inspector.ActiveCases - 1);
                inspector.TotalCasesHandled++;
                inspector.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Investigation report {ReportId} {Status} by {ApproverId}", reportId, request.Approved ? "approved" : "rejected", approverId);
        return true;
    }

    // ========== SLA MANAGEMENT ==========

    public async Task<SLAStatusDto?> GetSLAStatusAsync(Guid complaintId, CancellationToken ct = default)
    {
        var complaint = await _context.Complaints.FindAsync(new object[] { complaintId }, ct);
        if (complaint == null) return null;

        // Get SLA config for this complaint's category and severity
        var slaConfig = await _context.ComplaintSLAConfigs
            .FirstOrDefaultAsync(s => s.Category == complaint.Category && s.Severity == complaint.Severity && s.IsActive, ct);

        // Default SLA if not configured
        var responseTimeHours = slaConfig?.ResponseTimeHours ?? 24;
        var resolutionTimeHours = slaConfig?.ResolutionTimeHours ?? 72;

        var elapsedHours = (int)(DateTime.UtcNow - complaint.CreatedAt).TotalHours;
        var hasResponse = complaint.AssignedAt.HasValue;
        var responseHours = hasResponse ? (int)(complaint.AssignedAt!.Value - complaint.CreatedAt).TotalHours : elapsedHours;

        return new SLAStatusDto
        {
            ComplaintId = complaintId,
            ResponseTimeHours = responseTimeHours,
            ResolutionTimeHours = resolutionTimeHours,
            ElapsedHours = elapsedHours,
            ResponseSLABreached = responseHours > responseTimeHours,
            ResolutionSLABreached = elapsedHours > resolutionTimeHours && complaint.ResolvedAt == null,
            HoursUntilResponseBreach = hasResponse ? 0 : Math.Max(0, responseTimeHours - elapsedHours),
            HoursUntilResolutionBreach = complaint.ResolvedAt.HasValue ? 0 : Math.Max(0, resolutionTimeHours - elapsedHours)
        };
    }

    public async Task<List<SLABreachDto>> GetSLABreachesAsync(DateTime? fromDate = null, bool pendingOnly = false, CancellationToken ct = default)
    {
        var query = _context.SLABreaches
            .Include(s => s.Complaint)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.BreachedAt >= fromDate.Value);
        }

        if (pendingOnly)
        {
            query = query.Where(s => !s.IsEscalated);
        }

        var breaches = await query.OrderByDescending(s => s.BreachedAt).ToListAsync(ct);

        var result = new List<SLABreachDto>();
        foreach (var breach in breaches)
        {
            string? escalatedToName = null;
            if (breach.EscalatedToId.HasValue)
            {
                var user = await _context.Users.FindAsync(new object[] { breach.EscalatedToId.Value }, ct);
                escalatedToName = user?.FullName;
            }

            result.Add(new SLABreachDto
            {
                Id = breach.Id,
                ComplaintId = breach.ComplaintId,
                ComplaintNumber = breach.Complaint?.ComplaintNumber,
                BreachType = breach.BreachType,
                ExpectedHours = breach.ExpectedHours,
                ActualHours = breach.ActualHours,
                BreachedAt = breach.BreachedAt,
                IsEscalated = breach.IsEscalated,
                EscalatedToId = breach.EscalatedToId,
                EscalatedToName = escalatedToName,
                EscalatedAt = breach.EscalatedAt
            });
        }

        return result;
    }

    public async Task<bool> EscalateSLABreachAsync(Guid breachId, Guid escalateToId, CancellationToken ct = default)
    {
        var breach = await _context.SLABreaches.FindAsync(new object[] { breachId }, ct);
        if (breach == null) return false;

        breach.IsEscalated = true;
        breach.EscalatedToId = escalateToId;
        breach.EscalatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("SLA breach {BreachId} escalated to {UserId}", breachId, escalateToId);
        return true;
    }

    // ========== HELPER METHODS ==========

    private FieldVisitDto MapFieldVisitToDto(FieldVisit visit, string? complaintNumber, string? inspectorName)
    {
        return new FieldVisitDto
        {
            Id = visit.Id,
            ComplaintId = visit.ComplaintId,
            ComplaintNumber = complaintNumber,
            InspectorId = visit.InspectorId,
            InspectorName = inspectorName,
            ScheduledAt = visit.ScheduledAt,
            StartedAt = visit.StartedAt,
            CompletedAt = visit.CompletedAt,
            Status = visit.Status,
            Address = visit.Address,
            Latitude = visit.Latitude,
            Longitude = visit.Longitude,
            Notes = visit.Notes,
            CancellationReason = visit.CancellationReason,
            CreatedAt = visit.CreatedAt,
            Evidences = visit.Evidences.Select(e => new FieldVisitEvidenceDto
            {
                Id = e.Id,
                Type = e.Type,
                FileName = e.FileName,
                FileUrl = e.FileUrl,
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                Description = e.Description,
                CapturedAt = e.CapturedAt
            }).ToList()
        };
    }

    private InvestigationReportDto MapReportToDto(InvestigationReport report, string? complaintNumber, string? inspectorName, string? approvedByName, string? penaltyUserName)
    {
        return new InvestigationReportDto
        {
            Id = report.Id,
            ComplaintId = report.ComplaintId,
            ComplaintNumber = complaintNumber,
            InspectorId = report.InspectorId,
            InspectorName = inspectorName,
            Findings = report.Findings,
            Verdict = report.Verdict,
            VerdictReason = report.VerdictReason,
            RecommendedAction = report.RecommendedAction,
            CompensationAmount = report.CompensationAmount,
            PenaltyType = report.PenaltyType,
            PenaltyAmount = report.PenaltyAmount,
            PenaltyAppliedToId = report.PenaltyAppliedToId,
            PenaltyAppliedToName = penaltyUserName,
            IsApproved = report.IsApproved,
            ApprovedById = report.ApprovedById,
            ApprovedByName = approvedByName,
            ApprovedAt = report.ApprovedAt,
            SubmittedAt = report.SubmittedAt
        };
    }
}
