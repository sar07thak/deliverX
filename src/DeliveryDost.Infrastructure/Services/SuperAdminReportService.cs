using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.DTOs.Reports;
using DeliveryDost.Application.Services;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class SuperAdminReportService : ISuperAdminReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SuperAdminReportService> _logger;

    public SuperAdminReportService(ApplicationDbContext context, ILogger<SuperAdminReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region End Consumer Report

    public async Task<ReportResponse<EndConsumerReportItem>> GetEndConsumerReportAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Users
                .Where(u => u.Role == "EC") // End Consumer
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(searchTerm)) ||
                    (u.Phone != null && u.Phone.Contains(searchTerm)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                var isActive = request.Status.ToUpper() == "ACTIVE";
                query = query.Where(u => u.IsActive == isActive);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(u => u.CreatedAt <= request.ToDate.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDesc ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
                "phone" => request.SortDesc ? query.OrderByDescending(u => u.Phone) : query.OrderBy(u => u.Phone),
                "createdat" => request.SortDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                "lastlogin" => request.SortDesc ? query.OrderByDescending(u => u.LastLoginAt) : query.OrderBy(u => u.LastLoginAt),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };

            // Pagination
            var users = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Get delivery counts
            var userIds = users.Select(u => u.Id).ToList();
            var deliveryCounts = await _context.Deliveries
                .Where(d => userIds.Contains(d.RequesterId))
                .GroupBy(d => d.RequesterId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

            var items = users.Select(u => new EndConsumerReportItem
            {
                Id = u.Id,
                Name = u.FullName ?? "N/A",
                MobileNumber = u.Phone ?? "",
                MobileNumberMasked = DataMaskingHelper.MaskPhone(u.Phone),
                EmailId = u.Email,
                EmailIdMasked = DataMaskingHelper.MaskEmail(u.Email),
                StateName = null, // Would need to parse from address JSON
                DistrictName = null,
                Pincode = null,
                Address = null,
                DateOfBirth = null,
                DateOfJoining = u.CreatedAt,
                Status = u.IsActive ? "ACTIVE" : "INACTIVE",
                LastServiceAccessDate = u.LastLoginAt,
                TotalDeliveries = deliveryCounts.GetValueOrDefault(u.Id, 0)
            }).ToList();

            return new ReportResponse<EndConsumerReportItem>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating End Consumer report");
            throw;
        }
    }

    #endregion

    #region Business Consumer Report

    public async Task<ReportResponse<BusinessConsumerReportItem>> GetBusinessConsumerReportAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = from u in _context.Users
                        join bc in _context.BusinessConsumerProfiles on u.Id equals bc.UserId into bcJoin
                        from bc in bcJoin.DefaultIfEmpty()
                        where u.Role == "BC" || u.Role == "DBC"
                        select new { User = u, Profile = bc };

            // Apply filters
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(x =>
                    (x.User.FullName != null && x.User.FullName.ToLower().Contains(searchTerm)) ||
                    (x.Profile != null && x.Profile.BusinessName.ToLower().Contains(searchTerm)) ||
                    (x.User.Phone != null && x.User.Phone.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                var isActive = request.Status.ToUpper() == "ACTIVE";
                query = query.Where(x => x.User.IsActive == isActive);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            query = query.OrderByDescending(x => x.User.CreatedAt);

            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Get verification statuses
            var userIds = data.Select(x => x.User.Id).ToList();

            var panVerifications = await _context.PANVerifications
                .Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p.VerifiedAt.HasValue ? "VERIFIED" : "NOT_VERIFIED", cancellationToken);

            var aadhaarVerifications = await _context.AadhaarVerifications
                .Where(a => userIds.Contains(a.UserId))
                .ToDictionaryAsync(a => a.UserId, a => a.VerifiedAt.HasValue ? "VERIFIED" : "NOT_VERIFIED", cancellationToken);

            // Get subscription info
            var subscriptions = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .Where(s => userIds.Contains(s.UserId) && s.Status == "ACTIVE")
                .ToDictionaryAsync(s => s.UserId, s => s, cancellationToken);

            var items = data.Select(x => new BusinessConsumerReportItem
            {
                Id = x.User.Id,
                Name = x.User.FullName ?? "N/A",
                BusinessName = x.Profile?.BusinessName,
                PersonalPAN = x.Profile?.PAN,
                PersonalPANMasked = DataMaskingHelper.MaskPAN(x.Profile?.PAN),
                PersonalPANVerificationStatus = panVerifications.GetValueOrDefault(x.User.Id, "NOT_VERIFIED"),
                BusinessPAN = null, // Would need separate field
                BusinessPANMasked = null,
                BusinessPANVerificationStatus = null,
                AadhaarNumber = null, // Get from AadhaarVerification
                AadhaarNumberMasked = null,
                AadhaarVerificationStatus = aadhaarVerifications.GetValueOrDefault(x.User.Id, "NOT_VERIFIED"),
                MobileNumber = x.User.Phone ?? "",
                MobileNumberMasked = DataMaskingHelper.MaskPhone(x.User.Phone),
                EmailId = x.User.Email,
                EmailIdMasked = DataMaskingHelper.MaskEmail(x.User.Email),
                StateName = null,
                DistrictName = null,
                Pincode = null,
                Address = x.Profile?.BusinessAddress,
                DateOfBirth = null,
                DateOfJoining = x.User.CreatedAt,
                NumberOfPickupLocations = 0, // Would need separate table
                Status = x.User.IsActive ? "ACTIVE" : "INACTIVE",
                LastServiceAccessDate = x.User.LastLoginAt,
                GSTIN = x.Profile?.GSTIN,
                BusinessCategory = x.Profile?.BusinessCategory,
                SubscriptionPlanName = subscriptions.GetValueOrDefault(x.User.Id)?.Plan?.Name,
                SubscriptionExpiry = subscriptions.GetValueOrDefault(x.User.Id)?.EndDate
            }).ToList();

            return new ReportResponse<BusinessConsumerReportItem>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Business Consumer report");
            throw;
        }
    }

    #endregion

    #region Delivery Partner Report

    public async Task<ReportResponse<DeliveryPartnerReportItem>> GetDeliveryPartnerReportAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = from u in _context.Users
                        join dp in _context.DeliveryPartnerProfiles on u.Id equals dp.UserId into dpJoin
                        from dp in dpJoin.DefaultIfEmpty()
                        where u.Role == "DP"
                        select new { User = u, Profile = dp };

            // Apply filters
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(x =>
                    (x.User.FullName != null && x.User.FullName.ToLower().Contains(searchTerm)) ||
                    (x.Profile != null && x.Profile.FullName.ToLower().Contains(searchTerm)) ||
                    (x.User.Phone != null && x.User.Phone.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                var isActive = request.Status.ToUpper() == "ACTIVE";
                query = query.Where(x => x.User.IsActive == isActive);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            query = query.OrderByDescending(x => x.User.CreatedAt);

            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var userIds = data.Select(x => x.User.Id).ToList();

            // Get verification statuses
            var panVerifications = await _context.PANVerifications
                .Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p.VerifiedAt.HasValue ? "VERIFIED" : "NOT_VERIFIED", cancellationToken);

            var aadhaarVerifications = await _context.AadhaarVerifications
                .Where(a => userIds.Contains(a.UserId))
                .ToDictionaryAsync(a => a.UserId, a => a.VerifiedAt.HasValue ? "VERIFIED" : "NOT_VERIFIED", cancellationToken);

            // Get delivery counts
            var deliveryCounts = await _context.Deliveries
                .Where(d => userIds.Contains(d.AssignedDPId ?? Guid.Empty) && d.Status == "DELIVERED")
                .GroupBy(d => d.AssignedDPId)
                .Select(g => new { DPId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DPId ?? Guid.Empty, x => x.Count, cancellationToken);

            // Get ratings
            var ratings = await _context.Ratings
                .Where(r => userIds.Contains(r.TargetId))
                .GroupBy(r => r.TargetId)
                .Select(g => new { UserId = g.Key, AvgRating = g.Average(r => r.Score) })
                .ToDictionaryAsync(x => x.UserId, x => (decimal)x.AvgRating, cancellationToken);

            // Get DPCM names
            var dpcmIds = data.Where(x => x.Profile?.DPCMId != null).Select(x => x.Profile!.DPCMId!.Value).Distinct().ToList();
            var dpcmNames = await _context.DPCManagers
                .Where(d => dpcmIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.OrganizationName, cancellationToken);

            var items = data.Select(x => new DeliveryPartnerReportItem
            {
                Id = x.User.Id,
                Name = x.Profile?.FullName ?? x.User.FullName ?? "N/A",
                PersonalPAN = null,
                PersonalPANMasked = null,
                PersonalPANVerificationStatus = panVerifications.GetValueOrDefault(x.User.Id, "NOT_VERIFIED"),
                AadhaarNumber = null,
                AadhaarNumberMasked = null,
                AadhaarVerificationStatus = aadhaarVerifications.GetValueOrDefault(x.User.Id, "NOT_VERIFIED"),
                MobileNumber = x.User.Phone ?? "",
                MobileNumberMasked = DataMaskingHelper.MaskPhone(x.User.Phone),
                EmailId = x.User.Email,
                EmailIdMasked = DataMaskingHelper.MaskEmail(x.User.Email),
                StateName = null,
                DistrictName = null,
                Pincode = null,
                Address = x.Profile?.Address,
                DateOfBirth = x.Profile?.DOB,
                DateOfJoining = x.User.CreatedAt,
                ServiceAreaDescription = x.Profile?.ServiceAreaRadiusKm.HasValue == true
                    ? $"{x.Profile.ServiceAreaRadiusKm} km radius"
                    : null,
                ServiceAreaRadiusKm = x.Profile?.ServiceAreaRadiusKm,
                PerKgRate = x.Profile?.PerKgRate,
                PerKmRate = x.Profile?.PerKmRate,
                MinCharge = x.Profile?.MinCharge,
                Status = x.User.IsActive ? "ACTIVE" : "INACTIVE",
                LastServiceAccessDate = x.User.LastLoginAt,
                VehicleType = x.Profile?.VehicleType,
                TotalDeliveriesCompleted = deliveryCounts.GetValueOrDefault(x.User.Id, 0),
                AverageRating = ratings.GetValueOrDefault(x.User.Id, 0),
                DPCMName = x.Profile?.DPCMId.HasValue == true
                    ? dpcmNames.GetValueOrDefault(x.Profile.DPCMId.Value)
                    : null
            }).ToList();

            return new ReportResponse<DeliveryPartnerReportItem>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Delivery Partner report");
            throw;
        }
    }

    #endregion

    #region DPCM Report

    public async Task<ReportResponse<DPCMReportItem>> GetDPCMReportAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = from u in _context.Users
                        join dpcm in _context.DPCManagers on u.Id equals dpcm.UserId into dpcmJoin
                        from dpcm in dpcmJoin.DefaultIfEmpty()
                        where u.Role == "DPCM"
                        select new { User = u, Manager = dpcm };

            // Apply filters
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(x =>
                    (x.User.FullName != null && x.User.FullName.ToLower().Contains(searchTerm)) ||
                    (x.Manager != null && x.Manager.OrganizationName.ToLower().Contains(searchTerm)) ||
                    (x.User.Phone != null && x.User.Phone.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                var isActive = request.Status.ToUpper() == "ACTIVE";
                query = query.Where(x => x.User.IsActive == isActive);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            query = query.OrderByDescending(x => x.User.CreatedAt);

            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var userIds = data.Select(x => x.User.Id).ToList();
            var dpcmIds = data.Where(x => x.Manager != null).Select(x => x.Manager!.Id).ToList();

            // Get verification statuses
            var panVerifications = await _context.PANVerifications
                .Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p.VerifiedAt.HasValue ? "VERIFIED" : "NOT_VERIFIED", cancellationToken);

            var aadhaarVerifications = await _context.AadhaarVerifications
                .Where(a => userIds.Contains(a.UserId))
                .ToDictionaryAsync(a => a.UserId, a => a.VerifiedAt.HasValue ? "VERIFIED" : "NOT_VERIFIED", cancellationToken);

            // Get DP counts per DPCM
            var dpCounts = await _context.DeliveryPartnerProfiles
                .Where(dp => dp.DPCMId != null && dpcmIds.Contains(dp.DPCMId.Value))
                .GroupBy(dp => dp.DPCMId)
                .Select(g => new { DPCMId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DPCMId ?? Guid.Empty, x => x.Count, cancellationToken);

            // Get commission earnings
            var earnings = await _context.CommissionRecords
                .Where(c => dpcmIds.Contains(c.DPCMId ?? Guid.Empty))
                .GroupBy(c => c.DPCMId)
                .Select(g => new { DPCMId = g.Key, Total = g.Sum(c => c.DPCMCommission) })
                .ToDictionaryAsync(x => x.DPCMId ?? Guid.Empty, x => x.Total, cancellationToken);

            // Get deliveries managed
            var deliveriesManaged = await _context.Deliveries
                .Where(d => d.AssignedDPId != null)
                .Join(_context.DeliveryPartnerProfiles.Where(dp => dp.DPCMId != null && dpcmIds.Contains(dp.DPCMId.Value)),
                    d => d.AssignedDPId,
                    dp => dp.UserId,
                    (d, dp) => new { DPCMId = dp.DPCMId })
                .GroupBy(x => x.DPCMId)
                .Select(g => new { DPCMId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DPCMId ?? Guid.Empty, x => x.Count, cancellationToken);

            var items = data.Select(x => new DPCMReportItem
            {
                Id = x.User.Id,
                Name = x.Manager?.ContactPersonName ?? x.User.FullName ?? "N/A",
                BusinessName = x.Manager?.OrganizationName,
                PersonalPAN = x.Manager?.PAN,
                PersonalPANMasked = DataMaskingHelper.MaskPAN(x.Manager?.PAN),
                PersonalPANVerificationStatus = panVerifications.GetValueOrDefault(x.User.Id, "NOT_VERIFIED"),
                BusinessPAN = null,
                BusinessPANMasked = null,
                BusinessPANVerificationStatus = null,
                AadhaarNumber = null,
                AadhaarNumberMasked = null,
                AadhaarVerificationStatus = aadhaarVerifications.GetValueOrDefault(x.User.Id, "NOT_VERIFIED"),
                MobileNumber = x.User.Phone ?? "",
                MobileNumberMasked = DataMaskingHelper.MaskPhone(x.User.Phone),
                EmailId = x.User.Email,
                EmailIdMasked = DataMaskingHelper.MaskEmail(x.User.Email),
                StateName = null,
                DistrictName = null,
                Pincode = null,
                Address = null,
                DateOfBirth = null,
                DateOfJoining = x.User.CreatedAt,
                NumberOfPickupLocations = 0,
                Status = x.User.IsActive ? "ACTIVE" : "INACTIVE",
                LastServiceAccessDate = x.User.LastLoginAt,
                NumberOfBusinessUsersInArea = 0, // Would need area-based query
                NumberOfEndUsersInArea = 0,
                NumberOfDeliveryPartnersInArea = x.Manager != null ? dpCounts.GetValueOrDefault(x.Manager.Id, 0) : 0,
                CommissionType = x.Manager?.CommissionType,
                CommissionValue = x.Manager?.CommissionValue,
                ServiceRegions = x.Manager?.ServiceRegions,
                SecurityDeposit = null, // Would need separate field
                AgreementDocumentUrl = x.Manager?.RegistrationCertificateUrl,
                TotalEarnings = x.Manager != null ? earnings.GetValueOrDefault(x.Manager.Id, 0) : 0,
                TotalDeliveriesManaged = x.Manager != null ? deliveriesManaged.GetValueOrDefault(x.Manager.Id, 0) : 0
            }).ToList();

            return new ReportResponse<DPCMReportItem>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating DPCM report");
            throw;
        }
    }

    #endregion

    #region Export Methods (Placeholder)

    public Task<byte[]> ExportEndConsumerReportAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        // Would implement Excel export using a library like ClosedXML or EPPlus
        throw new NotImplementedException("Excel export not yet implemented");
    }

    public Task<byte[]> ExportBusinessConsumerReportAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Excel export not yet implemented");
    }

    public Task<byte[]> ExportDeliveryPartnerReportAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Excel export not yet implemented");
    }

    public Task<byte[]> ExportDPCMReportAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Excel export not yet implemented");
    }

    #endregion
}
