using System;
using System.Collections.Generic;

namespace DeliverX.Application.DTOs.Referral;

// Referral DTOs
public class ReferralCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal ReferrerReward { get; set; }
    public decimal RefereeReward { get; set; }
    public int TotalReferrals { get; set; }
    public int SuccessfulReferrals { get; set; }
    public decimal TotalEarnings { get; set; }
    public string ShareLink { get; set; } = string.Empty;
}

public class ReferralDto
{
    public Guid Id { get; set; }
    public Guid RefereeId { get; set; }
    public string? RefereeName { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? RewardEarned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ApplyReferralRequest
{
    public string Code { get; set; } = string.Empty;
}

public class ApplyReferralResponse
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public decimal? BonusAmount { get; set; }
}

public class ReferralStatsDto
{
    public string Code { get; set; } = string.Empty;
    public int TotalReferrals { get; set; }
    public int PendingReferrals { get; set; }
    public int CompletedReferrals { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal PendingEarnings { get; set; }
    public List<ReferralDto> RecentReferrals { get; set; } = new();
}

// Donation DTOs
public class DonationDto
{
    public Guid Id { get; set; }
    public string DonationNumber { get; set; } = string.Empty;
    public string CharityName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CharityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public decimal TotalReceived { get; set; }
}

public class MakeDonationRequest
{
    public Guid CharityId { get; set; }
    public decimal Amount { get; set; }
    public bool IsAnonymous { get; set; }
    public string? Message { get; set; }
}

public class MakeDonationResponse
{
    public bool IsSuccess { get; set; }
    public Guid? DonationId { get; set; }
    public string? DonationNumber { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
}

public class DonationPreferenceDto
{
    public bool EnableRoundUp { get; set; }
    public Guid? PreferredCharityId { get; set; }
    public string? PreferredCharityName { get; set; }
    public decimal? MonthlyLimit { get; set; }
    public decimal CurrentMonthTotal { get; set; }
}

public class UpdateDonationPreferenceRequest
{
    public bool EnableRoundUp { get; set; }
    public Guid? PreferredCharityId { get; set; }
    public decimal? MonthlyLimit { get; set; }
}

public class DonationStatsDto
{
    public decimal TotalDonated { get; set; }
    public int TotalDonations { get; set; }
    public decimal ThisMonthDonated { get; set; }
    public List<DonationDto> RecentDonations { get; set; } = new();
}
