using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs.Referral;

namespace DeliveryDost.Application.Services;

public interface IReferralService
{
    // Referral
    Task<ReferralCodeDto> GetOrCreateReferralCodeAsync(Guid userId, CancellationToken ct = default);
    Task<ApplyReferralResponse> ApplyReferralCodeAsync(Guid userId, ApplyReferralRequest request, CancellationToken ct = default);
    Task<ReferralStatsDto> GetReferralStatsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> CompleteReferralAsync(Guid refereeId, CancellationToken ct = default);

    // Donations
    Task<List<CharityDto>> GetCharitiesAsync(CancellationToken ct = default);
    Task<MakeDonationResponse> MakeDonationAsync(Guid userId, MakeDonationRequest request, CancellationToken ct = default);
    Task<DonationStatsDto> GetDonationStatsAsync(Guid userId, CancellationToken ct = default);
    Task<DonationPreferenceDto> GetDonationPreferenceAsync(Guid userId, CancellationToken ct = default);
    Task<bool> UpdateDonationPreferenceAsync(Guid userId, UpdateDonationPreferenceRequest request, CancellationToken ct = default);
    Task<bool> ProcessRoundUpDonationAsync(Guid userId, Guid deliveryId, decimal deliveryAmount, CancellationToken ct = default);
}
