using System;

namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Rating entity for multi-directional ratings between users
/// </summary>
public class Rating
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public Guid RaterId { get; set; }
    public string RaterType { get; set; } = string.Empty; // DP, EC, BC, DPCM
    public Guid TargetId { get; set; }
    public string TargetType { get; set; } = string.Empty; // DP, EC, BC, DPCM
    public int Score { get; set; } // 1-5 stars
    public string? Tags { get; set; } // JSON array: ["Punctual", "Polite"]
    public string? Comment { get; set; }
    public bool IsAnonymous { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Delivery? Delivery { get; set; }
    public User? Rater { get; set; }
    public User? Target { get; set; }
}

/// <summary>
/// Behavior index entity for tracking user performance metrics
/// </summary>
public class BehaviorIndex
{
    public Guid UserId { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public decimal CompletionRate { get; set; } // 0-100
    public decimal PunctualityRate { get; set; } // 0-100
    public decimal ComplaintFreeRate { get; set; } // 0-100
    public decimal BehaviorScore { get; set; } // 0-100 computed
    public DateTime LastCalculatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}
