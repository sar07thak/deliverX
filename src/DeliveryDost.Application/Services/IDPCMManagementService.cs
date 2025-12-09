using System;
using System.Threading;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs.DPCM;

namespace DeliveryDost.Application.Services;

/// <summary>
/// Service for DPCM (Delivery Partner Cluster Manager) management
/// - Manual registration by Admin
/// - Pincode-DPCM mapping (One Pincode = One DPCM)
/// - Commission configuration
/// </summary>
public interface IDPCMManagementService
{
    #region DPCM Registration (Admin Only)

    /// <summary>
    /// Manually register a new DPCM (Admin only)
    /// Creates user, DPCM profile, and assigns pincodes
    /// </summary>
    Task<CreateDPCMResponse> CreateDPCMAsync(CreateDPCMRequest request, Guid adminUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Update DPCM details
    /// </summary>
    Task<bool> UpdateDPCMAsync(Guid dpcmId, UpdateDPCMRequest request, Guid adminUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Activate/Deactivate a DPCM
    /// </summary>
    Task<bool> SetDPCMStatusAsync(Guid dpcmId, bool isActive, Guid adminUserId, string? reason, CancellationToken cancellationToken);

    /// <summary>
    /// Get DPCM details by ID
    /// </summary>
    Task<DPCMListItem?> GetDPCMByIdAsync(Guid dpcmId, CancellationToken cancellationToken);

    /// <summary>
    /// List all DPCMs with filtering
    /// </summary>
    Task<ListDPCMsResponse> ListDPCMsAsync(ListDPCMsRequest request, CancellationToken cancellationToken);

    #endregion

    #region Pincode-DPCM Mapping

    /// <summary>
    /// Assign pincodes to a DPCM
    /// Enforces one-pincode-one-DPCM rule
    /// </summary>
    Task<AssignPincodesToDPCMResponse> AssignPincodesToDPCMAsync(AssignPincodesToDPCMRequest request, Guid adminUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Unassign pincodes from a DPCM
    /// </summary>
    Task<bool> UnassignPincodesAsync(UnassignPincodesRequest request, Guid adminUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Get DPCM assigned to a specific pincode
    /// </summary>
    Task<GetDPCMByPincodeResponse> GetDPCMByPincodeAsync(string pincode, CancellationToken cancellationToken);

    /// <summary>
    /// Get all pincodes assigned to a DPCM
    /// </summary>
    Task<DPCMPincodeListResponse> GetDPCMPincodesAsync(Guid dpcmId, CancellationToken cancellationToken);

    /// <summary>
    /// Check if a pincode is already assigned to any DPCM
    /// </summary>
    Task<bool> IsPincodeAssignedAsync(string pincode, CancellationToken cancellationToken);

    /// <summary>
    /// Get available (unassigned) pincodes for a state/district
    /// </summary>
    Task<List<string>> GetAvailablePincodesAsync(string? state, string? district, CancellationToken cancellationToken);

    #endregion

    #region Commission Configuration

    /// <summary>
    /// Calculate DPCM commission for a delivery
    /// Uses "Amount or % whichever is higher" logic for HYBRID type
    /// </summary>
    Task<CalculateDPCMCommissionResponse> CalculateCommissionAsync(CalculateDPCMCommissionRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Update DPCM commission configuration
    /// </summary>
    Task<bool> UpdateCommissionConfigAsync(UpdateDPCMCommissionRequest request, Guid adminUserId, CancellationToken cancellationToken);

    #endregion

    #region Security Deposit Management

    /// <summary>
    /// Record security deposit received
    /// </summary>
    Task<bool> RecordSecurityDepositAsync(Guid dpcmId, decimal amount, string transactionRef, Guid adminUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Record security deposit refund
    /// </summary>
    Task<bool> RefundSecurityDepositAsync(Guid dpcmId, string reason, Guid adminUserId, CancellationToken cancellationToken);

    #endregion

    #region Agreement Management

    /// <summary>
    /// Upload/Update agreement document
    /// </summary>
    Task<bool> UpdateAgreementAsync(Guid dpcmId, string documentUrl, string version, Guid adminUserId, CancellationToken cancellationToken);

    #endregion
}
