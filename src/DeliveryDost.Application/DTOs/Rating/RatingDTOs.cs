using System;
using System.Collections.Generic;

namespace DeliveryDost.Application.DTOs.Rating;

public class CreateRatingRequest
{
    public Guid DeliveryId { get; set; }
    public Guid TargetId { get; set; }
    public string TargetType { get; set; } = string.Empty; // DP, EC, BC
    public int Score { get; set; } // 1-5
    public List<string>? Tags { get; set; }
    public string? Comment { get; set; }
    public bool IsAnonymous { get; set; }
}

public class CreateRatingResponse
{
    public bool IsSuccess { get; set; }
    public Guid? RatingId { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
}

public class RatingSummaryResponse
{
    public Guid TargetId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public Dictionary<int, int> Distribution { get; set; } = new(); // {5: 250, 4: 60, ...}
    public decimal BehaviorIndex { get; set; }
    public List<string> TopTags { get; set; } = new();
    public List<RatingDetail> RecentRatings { get; set; } = new();
}

public class RatingDetail
{
    public Guid Id { get; set; }
    public int Score { get; set; }
    public List<string>? Tags { get; set; }
    public string? Comment { get; set; }
    public string? RaterName { get; set; } // null if anonymous
    public DateTime CreatedAt { get; set; }
}

public class BehaviorIndexResponse
{
    public Guid UserId { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal PunctualityRate { get; set; }
    public decimal ComplaintFreeRate { get; set; }
    public decimal BehaviorScore { get; set; }
    public string BehaviorGrade { get; set; } = string.Empty; // A, B, C, D, F
    public DateTime LastCalculatedAt { get; set; }
}

public class GetRatingsRequest
{
    public Guid TargetId { get; set; }
    public string? TargetType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetRatingsResponse
{
    public List<RatingDetail> Ratings { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
