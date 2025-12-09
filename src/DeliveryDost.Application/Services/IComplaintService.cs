using System;
using System.Threading;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs.Complaint;

namespace DeliveryDost.Application.Services;

public interface IComplaintService
{
    // Complaint operations
    Task<CreateComplaintResponse> CreateComplaintAsync(Guid raiserId, string raiserType, CreateComplaintRequest request, CancellationToken ct = default);
    Task<ComplaintDto?> GetComplaintAsync(Guid complaintId, CancellationToken ct = default);
    Task<GetComplaintsResponse> GetComplaintsAsync(GetComplaintsRequest request, Guid? userId = null, string? userRole = null, CancellationToken ct = default);
    Task<ComplaintDto?> GetComplaintByNumberAsync(string complaintNumber, CancellationToken ct = default);

    // Evidence & Comments
    Task<bool> AddEvidenceAsync(Guid complaintId, Guid uploaderId, AddEvidenceRequest request, CancellationToken ct = default);
    Task<bool> AddCommentAsync(Guid complaintId, Guid authorId, AddCommentRequest request, CancellationToken ct = default);

    // Inspector operations
    Task<bool> AssignComplaintAsync(Guid complaintId, Guid inspectorId, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(Guid complaintId, string status, CancellationToken ct = default);
    Task<bool> UpdateSeverityAsync(Guid complaintId, string severity, CancellationToken ct = default);
    Task<bool> ResolveComplaintAsync(Guid complaintId, Guid resolverId, ResolveComplaintRequest request, CancellationToken ct = default);
    Task<bool> CloseComplaintAsync(Guid complaintId, CancellationToken ct = default);
    Task<bool> RejectComplaintAsync(Guid complaintId, string reason, CancellationToken ct = default);

    // Statistics
    Task<ComplaintStatsDto> GetComplaintStatsAsync(Guid? inspectorId = null, CancellationToken ct = default);

    // Inspector management
    Task<InspectorDto?> CreateInspectorAsync(CreateInspectorRequest request, CancellationToken ct = default);
    Task<InspectorDto?> GetInspectorAsync(Guid inspectorId, CancellationToken ct = default);
    Task<List<InspectorDto>> GetAvailableInspectorsAsync(CancellationToken ct = default);
}
