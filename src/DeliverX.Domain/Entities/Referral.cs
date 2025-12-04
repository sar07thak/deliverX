using System;

namespace DeliverX.Domain.Entities;

/// <summary>
/// Referral code for user invitations
/// </summary>
public class ReferralCode
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty; // REF-XXXXXX
    public decimal ReferrerReward { get; set; } // Reward for referrer
    public decimal RefereeReward { get; set; } // Reward for new user
    public int TotalReferrals { get; set; }
    public int SuccessfulReferrals { get; set; }
    public decimal TotalEarnings { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}

/// <summary>
/// Referral tracking
/// </summary>
public class Referral
{
    public Guid Id { get; set; }
    public Guid ReferrerId { get; set; }
    public Guid RefereeId { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING"; // PENDING, ACTIVE, COMPLETED, EXPIRED
    public decimal? ReferrerReward { get; set; }
    public decimal? RefereeReward { get; set; }
    public bool ReferrerRewarded { get; set; }
    public bool RefereeRewarded { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Referrer { get; set; }
    public User? Referee { get; set; }
}

/// <summary>
/// Donation entity for charitable giving feature
/// </summary>
public class Donation
{
    public Guid Id { get; set; }
    public string DonationNumber { get; set; } = string.Empty; // DON-YYYYMMDD-XXXX
    public Guid DonorId { get; set; }
    public Guid? CharityId { get; set; }
    public string CharityName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Source { get; set; } = string.Empty; // WALLET, DELIVERY_ROUNDUP, DIRECT
    public Guid? DeliveryId { get; set; }
    public bool IsAnonymous { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = "COMPLETED"; // PENDING, COMPLETED, FAILED
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Donor { get; set; }
    public Charity? Charity { get; set; }
    public Delivery? Delivery { get; set; }
}

/// <summary>
/// Charity/NGO for donations
/// </summary>
public class Charity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // EDUCATION, HEALTH, ENVIRONMENT, etc.
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public decimal TotalReceived { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// User donation preferences
/// </summary>
public class DonationPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool EnableRoundUp { get; set; } // Round up delivery amount
    public Guid? PreferredCharityId { get; set; }
    public decimal? MonthlyLimit { get; set; }
    public decimal CurrentMonthTotal { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public Charity? PreferredCharity { get; set; }
}
