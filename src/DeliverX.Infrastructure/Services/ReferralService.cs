using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliverX.Application.DTOs.Referral;
using DeliverX.Application.Services;
using DeliverX.Domain.Entities;
using DeliverX.Infrastructure.Data;

namespace DeliverX.Infrastructure.Services;

public class ReferralService : IReferralService
{
    private readonly ApplicationDbContext _context;
    private readonly IWalletService _walletService;
    private readonly ILogger<ReferralService> _logger;
    private const decimal DEFAULT_REFERRER_REWARD = 50m;
    private const decimal DEFAULT_REFEREE_REWARD = 25m;

    public ReferralService(
        ApplicationDbContext context,
        IWalletService walletService,
        ILogger<ReferralService> logger)
    {
        _context = context;
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<ReferralCodeDto> GetOrCreateReferralCodeAsync(Guid userId, CancellationToken ct = default)
    {
        var referralCode = await _context.Set<ReferralCode>()
            .FirstOrDefaultAsync(r => r.UserId == userId, ct);

        if (referralCode == null)
        {
            var code = $"REF-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
            referralCode = new ReferralCode
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Code = code,
                ReferrerReward = DEFAULT_REFERRER_REWARD,
                RefereeReward = DEFAULT_REFEREE_REWARD,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<ReferralCode>().Add(referralCode);
            await _context.SaveChangesAsync(ct);
        }

        return new ReferralCodeDto
        {
            Id = referralCode.Id,
            Code = referralCode.Code,
            ReferrerReward = referralCode.ReferrerReward,
            RefereeReward = referralCode.RefereeReward,
            TotalReferrals = referralCode.TotalReferrals,
            SuccessfulReferrals = referralCode.SuccessfulReferrals,
            TotalEarnings = referralCode.TotalEarnings,
            ShareLink = $"https://deliverx.app/join?ref={referralCode.Code}"
        };
    }

    public async Task<ApplyReferralResponse> ApplyReferralCodeAsync(Guid userId, ApplyReferralRequest request, CancellationToken ct = default)
    {
        var referralCode = await _context.Set<ReferralCode>()
            .FirstOrDefaultAsync(r => r.Code == request.Code && r.IsActive, ct);

        if (referralCode == null)
        {
            return new ApplyReferralResponse
            {
                IsSuccess = false,
                ErrorCode = "INVALID_CODE",
                Message = "Invalid referral code"
            };
        }

        if (referralCode.UserId == userId)
        {
            return new ApplyReferralResponse
            {
                IsSuccess = false,
                ErrorCode = "SELF_REFERRAL",
                Message = "You cannot use your own referral code"
            };
        }

        // Check if already referred
        var existingReferral = await _context.Set<Referral>()
            .FirstOrDefaultAsync(r => r.RefereeId == userId, ct);

        if (existingReferral != null)
        {
            return new ApplyReferralResponse
            {
                IsSuccess = false,
                ErrorCode = "ALREADY_REFERRED",
                Message = "You have already applied a referral code"
            };
        }

        var referral = new Referral
        {
            Id = Guid.NewGuid(),
            ReferrerId = referralCode.UserId,
            RefereeId = userId,
            ReferralCode = referralCode.Code,
            Status = "PENDING",
            ReferrerReward = referralCode.ReferrerReward,
            RefereeReward = referralCode.RefereeReward,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Referral>().Add(referral);
        referralCode.TotalReferrals++;

        // Credit referee bonus immediately
        await _walletService.CreditWalletAsync(userId, referralCode.RefereeReward, "REFERRAL_BONUS",
            $"Welcome bonus - Referral code {referralCode.Code}", referral.Id.ToString(), "REFERRAL", ct);

        referral.RefereeRewarded = true;

        await _context.SaveChangesAsync(ct);

        return new ApplyReferralResponse
        {
            IsSuccess = true,
            BonusAmount = referralCode.RefereeReward,
            Message = $"Referral code applied! You received {referralCode.RefereeReward:C} bonus"
        };
    }

    public async Task<ReferralStatsDto> GetReferralStatsAsync(Guid userId, CancellationToken ct = default)
    {
        var referralCode = await GetOrCreateReferralCodeAsync(userId, ct);

        var referrals = await _context.Set<Referral>()
            .Where(r => r.ReferrerId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        return new ReferralStatsDto
        {
            Code = referralCode.Code,
            TotalReferrals = referralCode.TotalReferrals,
            PendingReferrals = referrals.Count(r => r.Status == "PENDING"),
            CompletedReferrals = referralCode.SuccessfulReferrals,
            TotalEarnings = referralCode.TotalEarnings,
            PendingEarnings = referrals.Where(r => !r.ReferrerRewarded).Sum(r => r.ReferrerReward ?? 0),
            RecentReferrals = referrals.Select(r => new ReferralDto
            {
                Id = r.Id,
                RefereeId = r.RefereeId,
                Status = r.Status,
                RewardEarned = r.ReferrerRewarded ? r.ReferrerReward : null,
                CreatedAt = r.CreatedAt,
                CompletedAt = r.CompletedAt
            }).ToList()
        };
    }

    public async Task<bool> CompleteReferralAsync(Guid refereeId, CancellationToken ct = default)
    {
        var referral = await _context.Set<Referral>()
            .FirstOrDefaultAsync(r => r.RefereeId == refereeId && r.Status == "PENDING", ct);

        if (referral == null) return false;

        referral.Status = "COMPLETED";
        referral.CompletedAt = DateTime.UtcNow;

        // Credit referrer
        if (!referral.ReferrerRewarded && referral.ReferrerReward.HasValue)
        {
            await _walletService.CreditWalletAsync(referral.ReferrerId, referral.ReferrerReward.Value, "REFERRAL_BONUS",
                "Referral bonus - Your friend completed their first delivery", referral.Id.ToString(), "REFERRAL", ct);

            referral.ReferrerRewarded = true;

            var referralCode = await _context.Set<ReferralCode>()
                .FirstOrDefaultAsync(r => r.Code == referral.ReferralCode, ct);
            if (referralCode != null)
            {
                referralCode.SuccessfulReferrals++;
                referralCode.TotalEarnings += referral.ReferrerReward.Value;
            }
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<CharityDto>> GetCharitiesAsync(CancellationToken ct = default)
    {
        var charities = await _context.Set<Charity>()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        return charities.Select(c => new CharityDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Category = c.Category,
            LogoUrl = c.LogoUrl,
            WebsiteUrl = c.WebsiteUrl,
            TotalReceived = c.TotalReceived
        }).ToList();
    }

    public async Task<MakeDonationResponse> MakeDonationAsync(Guid userId, MakeDonationRequest request, CancellationToken ct = default)
    {
        var charity = await _context.Set<Charity>().FindAsync(new object[] { request.CharityId }, ct);
        if (charity == null || !charity.IsActive)
        {
            return new MakeDonationResponse
            {
                IsSuccess = false,
                ErrorCode = "CHARITY_NOT_FOUND",
                Message = "Charity not found"
            };
        }

        // Debit wallet
        var success = await _walletService.DebitWalletAsync(userId, request.Amount, "DONATION",
            $"Donation to {charity.Name}", request.CharityId.ToString(), "DONATION", ct);

        if (!success)
        {
            return new MakeDonationResponse
            {
                IsSuccess = false,
                ErrorCode = "INSUFFICIENT_BALANCE",
                Message = "Insufficient wallet balance"
            };
        }

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.Set<Donation>()
            .CountAsync(d => d.DonationNumber.StartsWith($"DON-{today}"), ct);
        var donationNumber = $"DON-{today}-{(count + 1):D4}";

        var donation = new Donation
        {
            Id = Guid.NewGuid(),
            DonationNumber = donationNumber,
            DonorId = userId,
            CharityId = charity.Id,
            CharityName = charity.Name,
            Amount = request.Amount,
            Source = "WALLET",
            IsAnonymous = request.IsAnonymous,
            Message = request.Message,
            Status = "COMPLETED",
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Donation>().Add(donation);
        charity.TotalReceived += request.Amount;

        await _context.SaveChangesAsync(ct);

        return new MakeDonationResponse
        {
            IsSuccess = true,
            DonationId = donation.Id,
            DonationNumber = donation.DonationNumber,
            Message = $"Thank you for donating to {charity.Name}!"
        };
    }

    public async Task<DonationStatsDto> GetDonationStatsAsync(Guid userId, CancellationToken ct = default)
    {
        var donations = await _context.Set<Donation>()
            .Where(d => d.DonorId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return new DonationStatsDto
        {
            TotalDonated = donations.Sum(d => d.Amount),
            TotalDonations = donations.Count,
            ThisMonthDonated = donations.Where(d => d.CreatedAt >= thisMonth).Sum(d => d.Amount),
            RecentDonations = donations.Take(5).Select(d => new DonationDto
            {
                Id = d.Id,
                DonationNumber = d.DonationNumber,
                CharityName = d.CharityName,
                Amount = d.Amount,
                Source = d.Source,
                IsAnonymous = d.IsAnonymous,
                CreatedAt = d.CreatedAt
            }).ToList()
        };
    }

    public async Task<DonationPreferenceDto> GetDonationPreferenceAsync(Guid userId, CancellationToken ct = default)
    {
        var pref = await _context.Set<DonationPreference>()
            .Include(p => p.PreferredCharity)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (pref == null)
        {
            return new DonationPreferenceDto
            {
                EnableRoundUp = false,
                PreferredCharityId = null,
                MonthlyLimit = null,
                CurrentMonthTotal = 0
            };
        }

        return new DonationPreferenceDto
        {
            EnableRoundUp = pref.EnableRoundUp,
            PreferredCharityId = pref.PreferredCharityId,
            PreferredCharityName = pref.PreferredCharity?.Name,
            MonthlyLimit = pref.MonthlyLimit,
            CurrentMonthTotal = pref.CurrentMonthTotal
        };
    }

    public async Task<bool> UpdateDonationPreferenceAsync(Guid userId, UpdateDonationPreferenceRequest request, CancellationToken ct = default)
    {
        var pref = await _context.Set<DonationPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (pref == null)
        {
            pref = new DonationPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Set<DonationPreference>().Add(pref);
        }

        pref.EnableRoundUp = request.EnableRoundUp;
        pref.PreferredCharityId = request.PreferredCharityId;
        pref.MonthlyLimit = request.MonthlyLimit;
        pref.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ProcessRoundUpDonationAsync(Guid userId, Guid deliveryId, decimal deliveryAmount, CancellationToken ct = default)
    {
        var pref = await _context.Set<DonationPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.EnableRoundUp, ct);

        if (pref == null || !pref.PreferredCharityId.HasValue) return false;

        var roundUpAmount = Math.Ceiling(deliveryAmount) - deliveryAmount;
        if (roundUpAmount <= 0) return false;

        // Check monthly limit
        if (pref.MonthlyLimit.HasValue && pref.CurrentMonthTotal + roundUpAmount > pref.MonthlyLimit.Value)
        {
            return false;
        }

        var charity = await _context.Set<Charity>().FindAsync(new object[] { pref.PreferredCharityId.Value }, ct);
        if (charity == null) return false;

        // Debit wallet
        var success = await _walletService.DebitWalletAsync(userId, roundUpAmount, "DONATION_ROUNDUP",
            $"Round-up donation to {charity.Name}", deliveryId.ToString(), "DELIVERY", ct);

        if (!success) return false;

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.Set<Donation>()
            .CountAsync(d => d.DonationNumber.StartsWith($"DON-{today}"), ct);
        var donationNumber = $"DON-{today}-{(count + 1):D4}";

        var donation = new Donation
        {
            Id = Guid.NewGuid(),
            DonationNumber = donationNumber,
            DonorId = userId,
            CharityId = charity.Id,
            CharityName = charity.Name,
            Amount = roundUpAmount,
            Source = "DELIVERY_ROUNDUP",
            DeliveryId = deliveryId,
            IsAnonymous = true,
            Status = "COMPLETED",
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Donation>().Add(donation);
        charity.TotalReceived += roundUpAmount;
        pref.CurrentMonthTotal += roundUpAmount;

        await _context.SaveChangesAsync(ct);
        return true;
    }
}
