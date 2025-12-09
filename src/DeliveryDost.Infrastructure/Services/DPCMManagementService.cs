using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.DTOs.DPCM;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class DPCMManagementService : IDPCMManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DPCMManagementService> _logger;
    private const decimal GST_RATE = 0.18m; // 18% GST

    public DPCMManagementService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DPCMManagementService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    #region DPCM Registration

    public async Task<CreateDPCMResponse> CreateDPCMAsync(CreateDPCMRequest request, Guid adminUserId, CancellationToken cancellationToken)
    {
        var response = new CreateDPCMResponse();

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Check if phone already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == request.PhoneNumber, cancellationToken);

            if (existingUser != null)
            {
                response.Errors.Add($"Phone number {request.PhoneNumber} is already registered");
                return response;
            }

            // Check if PAN already used for DPCM
            var existingPAN = await _context.DPCManagers
                .AnyAsync(d => d.PAN == request.PersonalPAN, cancellationToken);

            if (existingPAN)
            {
                response.Errors.Add($"PAN {request.PersonalPAN} is already registered as DPCM");
                return response;
            }

            // Create User
            var user = new User
            {
                Id = Guid.NewGuid(),
                Phone = request.PhoneNumber,
                FullName = request.FullName,
                Email = request.Email,
                Role = "DPCM",
                IsActive = true,
                IsPhoneVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            // Create DPCM Profile
            var dpcm = new DPCManager
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationName = request.OrganizationName ?? request.FullName,
                ContactPersonName = request.FullName,
                PAN = request.PersonalPAN,
                ServiceRegions = System.Text.Json.JsonSerializer.Serialize(new { State = request.State, City = request.City }),
                CommissionType = request.CommissionType,
                CommissionValue = request.CommissionPercentage,
                MinCommissionAmount = request.MinCommissionAmount,
                SecurityDeposit = request.SecurityDeposit,
                SecurityDepositStatus = request.SecurityDeposit > 0 ? "PENDING" : null,
                SecurityDepositTransactionRef = request.SecurityDepositTransactionRef,
                AgreementDocumentUrl = request.AgreementDocumentUrl,
                AgreementVersion = request.AgreementVersion,
                AgreementSignedAt = !string.IsNullOrEmpty(request.AgreementDocumentUrl) ? DateTime.UtcNow : null,
                BankAccountEncrypted = System.Text.Json.JsonSerializer.Serialize(new
                {
                    AccountHolderName = request.AccountHolderName,
                    AccountNumber = request.AccountNumber,
                    IFSCCode = request.IFSCCode,
                    BankName = request.BankName,
                    BranchName = request.BranchName
                }),
                IsActive = true,
                ActivatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DPCManagers.Add(dpcm);

            // Mark security deposit as received if transaction ref provided
            if (!string.IsNullOrEmpty(request.SecurityDepositTransactionRef) && request.SecurityDeposit > 0)
            {
                dpcm.SecurityDepositStatus = "RECEIVED";
                dpcm.SecurityDepositReceivedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Assign pincodes if provided
            int pincodesAssigned = 0;
            if (request.ServicePincodes.Any())
            {
                var assignRequest = new AssignPincodesToDPCMRequest
                {
                    DPCMId = dpcm.Id,
                    Pincodes = request.ServicePincodes,
                    OverrideExisting = false
                };

                var assignResult = await AssignPincodesToDPCMAsync(assignRequest, adminUserId, cancellationToken);
                pincodesAssigned = assignResult.SuccessfullyAssigned;
            }

            await transaction.CommitAsync(cancellationToken);

            response.IsSuccess = true;
            response.DPCMId = dpcm.Id;
            response.UserId = user.Id;
            response.PincodesAssigned = pincodesAssigned;
            response.Message = $"DPCM created successfully with {pincodesAssigned} pincodes assigned";

            _logger.LogInformation("DPCM created: {DPCMId} by Admin {AdminId}", dpcm.Id, adminUserId);

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating DPCM for phone {Phone}", request.PhoneNumber);
            response.Errors.Add("Failed to create DPCM: " + ex.Message);
            return response;
        }
    }

    public async Task<bool> UpdateDPCMAsync(Guid dpcmId, UpdateDPCMRequest request, Guid adminUserId, CancellationToken cancellationToken)
    {
        var dpcm = await _context.DPCManagers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == dpcmId, cancellationToken);

        if (dpcm == null)
            return false;

        if (!string.IsNullOrEmpty(request.OrganizationName))
            dpcm.OrganizationName = request.OrganizationName;

        if (!string.IsNullOrEmpty(request.ContactPersonName))
            dpcm.ContactPersonName = request.ContactPersonName;

        if (!string.IsNullOrEmpty(request.Email) && dpcm.User != null)
            dpcm.User.Email = request.Email;

        if (request.SecurityDeposit.HasValue)
            dpcm.SecurityDeposit = request.SecurityDeposit.Value;

        if (!string.IsNullOrEmpty(request.SecurityDepositStatus))
            dpcm.SecurityDepositStatus = request.SecurityDepositStatus;

        if (!string.IsNullOrEmpty(request.SecurityDepositTransactionRef))
            dpcm.SecurityDepositTransactionRef = request.SecurityDepositTransactionRef;

        if (!string.IsNullOrEmpty(request.AgreementDocumentUrl))
        {
            dpcm.AgreementDocumentUrl = request.AgreementDocumentUrl;
            dpcm.AgreementSignedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrEmpty(request.AgreementVersion))
            dpcm.AgreementVersion = request.AgreementVersion;

        if (!string.IsNullOrEmpty(request.CommissionType))
            dpcm.CommissionType = request.CommissionType;

        if (request.CommissionPercentage.HasValue)
            dpcm.CommissionValue = request.CommissionPercentage;

        if (request.MinCommissionAmount.HasValue)
            dpcm.MinCommissionAmount = request.MinCommissionAmount;

        if (request.IsActive.HasValue)
        {
            dpcm.IsActive = request.IsActive.Value;
            if (request.IsActive.Value && !dpcm.ActivatedAt.HasValue)
                dpcm.ActivatedAt = DateTime.UtcNow;
        }

        dpcm.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("DPCM {DPCMId} updated by Admin {AdminId}", dpcmId, adminUserId);

        return true;
    }

    public async Task<bool> SetDPCMStatusAsync(Guid dpcmId, bool isActive, Guid adminUserId, string? reason, CancellationToken cancellationToken)
    {
        var dpcm = await _context.DPCManagers.FindAsync(new object[] { dpcmId }, cancellationToken);

        if (dpcm == null)
            return false;

        dpcm.IsActive = isActive;
        dpcm.UpdatedAt = DateTime.UtcNow;

        if (isActive && !dpcm.ActivatedAt.HasValue)
            dpcm.ActivatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("DPCM {DPCMId} status set to {IsActive} by Admin {AdminId}. Reason: {Reason}",
            dpcmId, isActive, adminUserId, reason);

        return true;
    }

    public async Task<DPCMListItem?> GetDPCMByIdAsync(Guid dpcmId, CancellationToken cancellationToken)
    {
        return await _context.DPCManagers
            .Include(d => d.User)
            .Where(d => d.Id == dpcmId)
            .Select(d => new DPCMListItem
            {
                Id = d.Id,
                UserId = d.UserId,
                Name = d.ContactPersonName,
                OrganizationName = d.OrganizationName,
                Phone = d.User.Phone ?? "",
                Email = d.User.Email,
                IsActive = d.IsActive,
                TotalPincodes = d.PincodeMappings.Count(pm => pm.IsActive),
                TotalDPs = d.DeliveryPartners.Count,
                SecurityDeposit = d.SecurityDeposit,
                SecurityDepositStatus = d.SecurityDepositStatus,
                HasAgreement = !string.IsNullOrEmpty(d.AgreementDocumentUrl),
                CommissionType = d.CommissionType,
                CommissionValue = d.CommissionValue,
                CreatedAt = d.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ListDPCMsResponse> ListDPCMsAsync(ListDPCMsRequest request, CancellationToken cancellationToken)
    {
        var query = _context.DPCManagers
            .Include(d => d.User)
            .AsQueryable();

        // Search
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(d =>
                d.ContactPersonName.ToLower().Contains(term) ||
                d.OrganizationName.ToLower().Contains(term) ||
                (d.User.Phone != null && d.User.Phone.Contains(term)) ||
                d.PAN.ToLower().Contains(term));
        }

        // Status filter
        if (!string.IsNullOrEmpty(request.Status))
        {
            bool isActive = request.Status.ToUpper() == "ACTIVE";
            query = query.Where(d => d.IsActive == isActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DPCMListItem
            {
                Id = d.Id,
                UserId = d.UserId,
                Name = d.ContactPersonName,
                OrganizationName = d.OrganizationName,
                Phone = d.User.Phone ?? "",
                Email = d.User.Email,
                IsActive = d.IsActive,
                TotalPincodes = d.PincodeMappings.Count(pm => pm.IsActive),
                TotalDPs = d.DeliveryPartners.Count,
                SecurityDeposit = d.SecurityDeposit,
                SecurityDepositStatus = d.SecurityDepositStatus,
                HasAgreement = !string.IsNullOrEmpty(d.AgreementDocumentUrl),
                CommissionType = d.CommissionType,
                CommissionValue = d.CommissionValue,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new ListDPCMsResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    #endregion

    #region Pincode-DPCM Mapping

    public async Task<AssignPincodesToDPCMResponse> AssignPincodesToDPCMAsync(AssignPincodesToDPCMRequest request, Guid adminUserId, CancellationToken cancellationToken)
    {
        var response = new AssignPincodesToDPCMResponse
        {
            TotalRequested = request.Pincodes.Count
        };

        var dpcm = await _context.DPCManagers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == request.DPCMId, cancellationToken);

        if (dpcm == null)
        {
            response.Results.Add(new PincodeAssignmentResult
            {
                IsSuccess = false,
                Message = "DPCM not found"
            });
            return response;
        }

        foreach (var pincode in request.Pincodes.Distinct())
        {
            var result = new PincodeAssignmentResult { Pincode = pincode };

            // Check existing mapping
            var existingMapping = await _context.PincodeDPCMMappings
                .Include(pm => pm.DPCM)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(pm => pm.Pincode == pincode && pm.IsActive, cancellationToken);

            if (existingMapping != null)
            {
                if (existingMapping.DPCMId == request.DPCMId)
                {
                    result.IsSuccess = false;
                    result.Message = "Already assigned to this DPCM";
                    response.AlreadyAssignedToSame++;
                }
                else if (request.OverrideExisting)
                {
                    // Deactivate existing mapping
                    existingMapping.IsActive = false;
                    existingMapping.DeactivatedAt = DateTime.UtcNow;
                    existingMapping.DeactivationReason = $"Reassigned to DPCM {dpcm.ContactPersonName}";

                    // Create new mapping
                    await CreatePincodeMappingAsync(pincode, request.DPCMId, adminUserId, cancellationToken);

                    result.IsSuccess = true;
                    result.Message = $"Reassigned from {existingMapping.DPCM.ContactPersonName}";
                    response.SuccessfullyAssigned++;
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = "Already assigned to another DPCM";
                    result.ExistingDPCMName = existingMapping.DPCM.ContactPersonName;
                    response.AlreadyAssignedToOthers++;
                }
            }
            else
            {
                // Create new mapping
                await CreatePincodeMappingAsync(pincode, request.DPCMId, adminUserId, cancellationToken);

                result.IsSuccess = true;
                result.Message = "Successfully assigned";
                response.SuccessfullyAssigned++;
            }

            response.Results.Add(result);
        }

        await _context.SaveChangesAsync(cancellationToken);

        response.IsSuccess = response.SuccessfullyAssigned > 0;

        _logger.LogInformation("Assigned {Count} pincodes to DPCM {DPCMId} by Admin {AdminId}",
            response.SuccessfullyAssigned, request.DPCMId, adminUserId);

        return response;
    }

    private async Task CreatePincodeMappingAsync(string pincode, Guid dpcmId, Guid adminUserId, CancellationToken cancellationToken)
    {
        // Try to get state/district from PincodeMaster
        var pincodeInfo = await _context.PincodeMasters
            .FirstOrDefaultAsync(p => p.Pincode == pincode, cancellationToken);

        var mapping = new PincodeDPCMMapping
        {
            Id = Guid.NewGuid(),
            Pincode = pincode,
            DPCMId = dpcmId,
            StateName = pincodeInfo?.StateName,
            DistrictName = pincodeInfo?.DistrictName,
            IsActive = true,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = adminUserId
        };

        _context.PincodeDPCMMappings.Add(mapping);
    }

    public async Task<bool> UnassignPincodesAsync(UnassignPincodesRequest request, Guid adminUserId, CancellationToken cancellationToken)
    {
        var mappings = await _context.PincodeDPCMMappings
            .Where(pm => pm.DPCMId == request.DPCMId &&
                        request.Pincodes.Contains(pm.Pincode) &&
                        pm.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var mapping in mappings)
        {
            mapping.IsActive = false;
            mapping.DeactivatedAt = DateTime.UtcNow;
            mapping.DeactivationReason = request.Reason ?? "Unassigned by admin";
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Unassigned {Count} pincodes from DPCM {DPCMId} by Admin {AdminId}",
            mappings.Count, request.DPCMId, adminUserId);

        return true;
    }

    public async Task<GetDPCMByPincodeResponse> GetDPCMByPincodeAsync(string pincode, CancellationToken cancellationToken)
    {
        var mapping = await _context.PincodeDPCMMappings
            .Include(pm => pm.DPCM)
            .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(pm => pm.Pincode == pincode && pm.IsActive, cancellationToken);

        if (mapping == null)
        {
            return new GetDPCMByPincodeResponse
            {
                HasDPCM = false,
                Pincode = pincode
            };
        }

        return new GetDPCMByPincodeResponse
        {
            HasDPCM = true,
            Pincode = pincode,
            DPCMId = mapping.DPCMId,
            DPCMName = mapping.DPCM.ContactPersonName,
            OrganizationName = mapping.DPCM.OrganizationName,
            Phone = mapping.DPCM.User.Phone,
            AssignedAt = mapping.AssignedAt
        };
    }

    public async Task<DPCMPincodeListResponse> GetDPCMPincodesAsync(Guid dpcmId, CancellationToken cancellationToken)
    {
        var dpcm = await _context.DPCManagers
            .FirstOrDefaultAsync(d => d.Id == dpcmId, cancellationToken);

        if (dpcm == null)
        {
            return new DPCMPincodeListResponse { DPCMId = dpcmId };
        }

        var pincodes = await _context.PincodeDPCMMappings
            .Where(pm => pm.DPCMId == dpcmId && pm.IsActive)
            .OrderBy(pm => pm.Pincode)
            .Select(pm => new DPCMPincodeItem
            {
                Pincode = pm.Pincode,
                StateName = pm.StateName,
                DistrictName = pm.DistrictName,
                AssignedAt = pm.AssignedAt,
                IsActive = pm.IsActive
            })
            .ToListAsync(cancellationToken);

        return new DPCMPincodeListResponse
        {
            DPCMId = dpcmId,
            DPCMName = dpcm.ContactPersonName,
            TotalPincodes = pincodes.Count,
            Pincodes = pincodes
        };
    }

    public async Task<bool> IsPincodeAssignedAsync(string pincode, CancellationToken cancellationToken)
    {
        return await _context.PincodeDPCMMappings
            .AnyAsync(pm => pm.Pincode == pincode && pm.IsActive, cancellationToken);
    }

    public async Task<List<string>> GetAvailablePincodesAsync(string? state, string? district, CancellationToken cancellationToken)
    {
        var assignedPincodes = await _context.PincodeDPCMMappings
            .Where(pm => pm.IsActive)
            .Select(pm => pm.Pincode)
            .ToListAsync(cancellationToken);

        var query = _context.PincodeMasters
            .Where(p => p.IsActive && !assignedPincodes.Contains(p.Pincode));

        if (!string.IsNullOrEmpty(state))
            query = query.Where(p => p.StateName == state);

        if (!string.IsNullOrEmpty(district))
            query = query.Where(p => p.DistrictName == district);

        return await query
            .Select(p => p.Pincode)
            .Distinct()
            .OrderBy(p => p)
            .Take(500) // Limit results
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Commission Configuration

    public async Task<CalculateDPCMCommissionResponse> CalculateCommissionAsync(CalculateDPCMCommissionRequest request, CancellationToken cancellationToken)
    {
        var dpcm = await _context.DPCManagers.FindAsync(new object[] { request.DPCMId }, cancellationToken);

        if (dpcm == null)
        {
            return new CalculateDPCMCommissionResponse
            {
                DeliveryAmount = request.DeliveryAmount,
                FinalCommission = 0
            };
        }

        var response = new CalculateDPCMCommissionResponse
        {
            DeliveryAmount = request.DeliveryAmount,
            CommissionType = dpcm.CommissionType ?? "HYBRID",
            CommissionPercentage = dpcm.CommissionValue ?? 0,
            MinCommissionAmount = dpcm.MinCommissionAmount ?? 0
        };

        // Calculate percentage amount
        response.CalculatedPercentageAmount = request.DeliveryAmount * (response.CommissionPercentage / 100);

        // Determine final commission based on type
        switch (dpcm.CommissionType?.ToUpper())
        {
            case "PERCENTAGE":
                response.FinalCommission = response.CalculatedPercentageAmount;
                response.AppliedMethod = "PERCENTAGE";
                break;

            case "FLAT":
                response.FinalCommission = response.MinCommissionAmount;
                response.AppliedMethod = "FLAT_AMOUNT";
                break;

            case "HYBRID":
            default:
                // "Amount or % whichever is higher"
                if (response.CalculatedPercentageAmount >= response.MinCommissionAmount)
                {
                    response.FinalCommission = response.CalculatedPercentageAmount;
                    response.AppliedMethod = "PERCENTAGE";
                }
                else
                {
                    response.FinalCommission = response.MinCommissionAmount;
                    response.AppliedMethod = "MIN_AMOUNT";
                }
                break;
        }

        // Calculate GST (18%)
        response.GSTAmount = response.FinalCommission * GST_RATE;
        response.TotalWithGST = response.FinalCommission + response.GSTAmount;

        return response;
    }

    public async Task<bool> UpdateCommissionConfigAsync(UpdateDPCMCommissionRequest request, Guid adminUserId, CancellationToken cancellationToken)
    {
        var dpcm = await _context.DPCManagers.FindAsync(new object[] { request.DPCMId }, cancellationToken);

        if (dpcm == null)
            return false;

        dpcm.CommissionType = request.CommissionType;
        dpcm.CommissionValue = request.CommissionPercentage;
        dpcm.MinCommissionAmount = request.MinCommissionAmount;
        dpcm.UpdatedAt = DateTime.UtcNow;

        // Also update or create commission config record for history
        var config = new DPCMCommissionConfig
        {
            Id = Guid.NewGuid(),
            DPCMId = request.DPCMId,
            CommissionType = request.CommissionType,
            CommissionValue = request.CommissionPercentage,
            MinCommissionAmount = request.MinCommissionAmount,
            EffectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.DPCMCommissionConfigs.Add(config);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated commission config for DPCM {DPCMId} by Admin {AdminId}: Type={Type}, Value={Value}%, Min={Min}",
            request.DPCMId, adminUserId, request.CommissionType, request.CommissionPercentage, request.MinCommissionAmount);

        return true;
    }

    #endregion

    #region Security Deposit Management

    public async Task<bool> RecordSecurityDepositAsync(Guid dpcmId, decimal amount, string transactionRef, Guid adminUserId, CancellationToken cancellationToken)
    {
        var dpcm = await _context.DPCManagers.FindAsync(new object[] { dpcmId }, cancellationToken);

        if (dpcm == null)
            return false;

        dpcm.SecurityDeposit = amount;
        dpcm.SecurityDepositStatus = "RECEIVED";
        dpcm.SecurityDepositReceivedAt = DateTime.UtcNow;
        dpcm.SecurityDepositTransactionRef = transactionRef;
        dpcm.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Security deposit {Amount} recorded for DPCM {DPCMId} by Admin {AdminId}",
            amount, dpcmId, adminUserId);

        return true;
    }

    public async Task<bool> RefundSecurityDepositAsync(Guid dpcmId, string reason, Guid adminUserId, CancellationToken cancellationToken)
    {
        var dpcm = await _context.DPCManagers.FindAsync(new object[] { dpcmId }, cancellationToken);

        if (dpcm == null)
            return false;

        dpcm.SecurityDepositStatus = "REFUNDED";
        dpcm.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Security deposit refunded for DPCM {DPCMId} by Admin {AdminId}. Reason: {Reason}",
            dpcmId, adminUserId, reason);

        return true;
    }

    #endregion

    #region Agreement Management

    public async Task<bool> UpdateAgreementAsync(Guid dpcmId, string documentUrl, string version, Guid adminUserId, CancellationToken cancellationToken)
    {
        var dpcm = await _context.DPCManagers.FindAsync(new object[] { dpcmId }, cancellationToken);

        if (dpcm == null)
            return false;

        dpcm.AgreementDocumentUrl = documentUrl;
        dpcm.AgreementVersion = version;
        dpcm.AgreementSignedAt = DateTime.UtcNow;
        dpcm.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Agreement updated for DPCM {DPCMId} by Admin {AdminId}. Version: {Version}",
            dpcmId, adminUserId, version);

        return true;
    }

    #endregion
}
