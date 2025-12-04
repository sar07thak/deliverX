using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliverX.Application.DTOs.Complaint;
using DeliverX.Application.Services;
using DeliverX.Domain.Entities;
using DeliverX.Infrastructure.Data;

namespace DeliverX.Infrastructure.Services;

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
}
