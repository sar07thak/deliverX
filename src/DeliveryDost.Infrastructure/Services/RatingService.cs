using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.DTOs.Rating;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class RatingService : IRatingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RatingService> _logger;

    public RatingService(ApplicationDbContext context, ILogger<RatingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreateRatingResponse> CreateRatingAsync(
        Guid raterId,
        string raterType,
        CreateRatingRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating rating from {RaterId} to {TargetId} for delivery {DeliveryId}",
            raterId, request.TargetId, request.DeliveryId);

        // Validate score
        if (request.Score < 1 || request.Score > 5)
        {
            return new CreateRatingResponse
            {
                IsSuccess = false,
                ErrorCode = "INVALID_SCORE",
                Message = "Score must be between 1 and 5"
            };
        }

        // Check if already rated
        var existingRating = await _context.Ratings
            .FirstOrDefaultAsync(r => r.DeliveryId == request.DeliveryId &&
                                     r.RaterId == raterId &&
                                     r.TargetId == request.TargetId, ct);

        if (existingRating != null)
        {
            return new CreateRatingResponse
            {
                IsSuccess = false,
                ErrorCode = "ALREADY_RATED",
                Message = "You have already rated this user for this delivery"
            };
        }

        // Verify delivery exists and is completed
        var delivery = await _context.Deliveries.FindAsync(new object[] { request.DeliveryId }, ct);
        if (delivery == null)
        {
            return new CreateRatingResponse
            {
                IsSuccess = false,
                ErrorCode = "DELIVERY_NOT_FOUND",
                Message = "Delivery not found"
            };
        }

        if (delivery.Status != "DELIVERED" && delivery.Status != "CLOSED")
        {
            return new CreateRatingResponse
            {
                IsSuccess = false,
                ErrorCode = "DELIVERY_NOT_COMPLETED",
                Message = "Can only rate after delivery is completed"
            };
        }

        // Create rating
        var rating = new Rating
        {
            Id = Guid.NewGuid(),
            DeliveryId = request.DeliveryId,
            RaterId = raterId,
            RaterType = raterType,
            TargetId = request.TargetId,
            TargetType = request.TargetType,
            Score = request.Score,
            Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null,
            Comment = request.Comment,
            IsAnonymous = request.IsAnonymous,
            CreatedAt = DateTime.UtcNow
        };

        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync(ct);

        // Trigger behavior index recalculation
        await RecalculateBehaviorIndexAsync(request.TargetId, ct);

        _logger.LogInformation("Rating {RatingId} created successfully", rating.Id);

        return new CreateRatingResponse
        {
            IsSuccess = true,
            RatingId = rating.Id,
            Message = "Rating submitted successfully"
        };
    }

    public async Task<RatingSummaryResponse> GetRatingSummaryAsync(Guid targetId, CancellationToken ct = default)
    {
        var ratings = await _context.Ratings
            .Where(r => r.TargetId == targetId)
            .ToListAsync(ct);

        if (!ratings.Any())
        {
            return new RatingSummaryResponse
            {
                TargetId = targetId,
                AverageRating = 0,
                TotalRatings = 0,
                Distribution = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } }
            };
        }

        var averageRating = (decimal)ratings.Average(r => r.Score);
        var distribution = ratings.GroupBy(r => r.Score)
            .ToDictionary(g => g.Key, g => g.Count());

        // Fill in missing scores
        for (int i = 1; i <= 5; i++)
        {
            if (!distribution.ContainsKey(i))
                distribution[i] = 0;
        }

        // Calculate top tags
        var allTags = ratings
            .Where(r => !string.IsNullOrEmpty(r.Tags))
            .SelectMany(r => JsonSerializer.Deserialize<List<string>>(r.Tags!) ?? new List<string>())
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        // Get behavior index
        var behaviorIndex = await _context.BehaviorIndexes
            .FirstOrDefaultAsync(b => b.UserId == targetId, ct);

        // Get recent ratings
        var recentRatings = await _context.Ratings
            .Where(r => r.TargetId == targetId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new RatingDetail
            {
                Id = r.Id,
                Score = r.Score,
                Tags = string.IsNullOrEmpty(r.Tags) ? null : JsonSerializer.Deserialize<List<string>>(r.Tags),
                Comment = r.Comment,
                RaterName = r.IsAnonymous ? null : "User", // Would join with User table in production
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return new RatingSummaryResponse
        {
            TargetId = targetId,
            AverageRating = Math.Round(averageRating, 2),
            TotalRatings = ratings.Count,
            Distribution = distribution.OrderByDescending(d => d.Key).ToDictionary(d => d.Key, d => d.Value),
            BehaviorIndex = behaviorIndex?.BehaviorScore ?? 0,
            TopTags = allTags,
            RecentRatings = recentRatings
        };
    }

    public async Task<GetRatingsResponse> GetRatingsAsync(GetRatingsRequest request, CancellationToken ct = default)
    {
        var query = _context.Ratings.Where(r => r.TargetId == request.TargetId);

        if (!string.IsNullOrEmpty(request.TargetType))
        {
            query = query.Where(r => r.TargetType == request.TargetType);
        }

        var totalCount = await query.CountAsync(ct);

        var ratings = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new RatingDetail
            {
                Id = r.Id,
                Score = r.Score,
                Tags = string.IsNullOrEmpty(r.Tags) ? null : JsonSerializer.Deserialize<List<string>>(r.Tags),
                Comment = r.Comment,
                RaterName = r.IsAnonymous ? null : "User",
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return new GetRatingsResponse
        {
            Ratings = ratings,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };
    }

    public async Task<BehaviorIndexResponse> GetBehaviorIndexAsync(Guid userId, CancellationToken ct = default)
    {
        var behaviorIndex = await _context.BehaviorIndexes
            .FirstOrDefaultAsync(b => b.UserId == userId, ct);

        if (behaviorIndex == null)
        {
            // Calculate if doesn't exist
            return await RecalculateBehaviorIndexAsync(userId, ct);
        }

        return new BehaviorIndexResponse
        {
            UserId = behaviorIndex.UserId,
            AverageRating = behaviorIndex.AverageRating,
            TotalRatings = behaviorIndex.TotalRatings,
            CompletionRate = behaviorIndex.CompletionRate,
            PunctualityRate = behaviorIndex.PunctualityRate,
            ComplaintFreeRate = behaviorIndex.ComplaintFreeRate,
            BehaviorScore = behaviorIndex.BehaviorScore,
            BehaviorGrade = GetGrade(behaviorIndex.BehaviorScore),
            LastCalculatedAt = behaviorIndex.LastCalculatedAt
        };
    }

    public async Task<BehaviorIndexResponse> RecalculateBehaviorIndexAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Recalculating behavior index for user {UserId}", userId);

        // Get ratings
        var ratings = await _context.Ratings
            .Where(r => r.TargetId == userId)
            .ToListAsync(ct);

        var averageRating = ratings.Any() ? (decimal)ratings.Average(r => r.Score) : 0m;
        var totalRatings = ratings.Count;

        // Calculate completion rate (deliveries completed / deliveries assigned)
        var assignedDeliveries = await _context.Deliveries
            .Where(d => d.AssignedDPId == userId)
            .CountAsync(ct);

        var completedDeliveries = await _context.Deliveries
            .Where(d => d.AssignedDPId == userId &&
                       (d.Status == "DELIVERED" || d.Status == "CLOSED"))
            .CountAsync(ct);

        var completionRate = assignedDeliveries > 0
            ? (decimal)completedDeliveries / assignedDeliveries * 100
            : 100m;

        // For MVP, assume 90% punctuality and 95% complaint-free
        var punctualityRate = 90m;
        var complaintFreeRate = 95m;

        // Calculate behavior score
        // (Average Rating × 20) × 0.6 + (Completion Rate) × 0.2 + (Punctuality) × 0.1 + (Complaint-Free) × 0.1
        var behaviorScore = (averageRating * 20m) * 0.6m +
                           completionRate * 0.2m +
                           punctualityRate * 0.1m +
                           complaintFreeRate * 0.1m;

        behaviorScore = Math.Min(100, Math.Max(0, behaviorScore));

        // Upsert behavior index
        var existing = await _context.BehaviorIndexes
            .FirstOrDefaultAsync(b => b.UserId == userId, ct);

        if (existing == null)
        {
            existing = new BehaviorIndex
            {
                UserId = userId
            };
            _context.BehaviorIndexes.Add(existing);
        }

        existing.AverageRating = Math.Round(averageRating, 2);
        existing.TotalRatings = totalRatings;
        existing.CompletionRate = Math.Round(completionRate, 2);
        existing.PunctualityRate = punctualityRate;
        existing.ComplaintFreeRate = complaintFreeRate;
        existing.BehaviorScore = Math.Round(behaviorScore, 2);
        existing.LastCalculatedAt = DateTime.UtcNow;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return new BehaviorIndexResponse
        {
            UserId = userId,
            AverageRating = existing.AverageRating,
            TotalRatings = existing.TotalRatings,
            CompletionRate = existing.CompletionRate,
            PunctualityRate = existing.PunctualityRate,
            ComplaintFreeRate = existing.ComplaintFreeRate,
            BehaviorScore = existing.BehaviorScore,
            BehaviorGrade = GetGrade(existing.BehaviorScore),
            LastCalculatedAt = existing.LastCalculatedAt
        };
    }

    public async Task<bool> HasRatedDeliveryAsync(Guid raterId, Guid deliveryId, Guid targetId, CancellationToken ct = default)
    {
        return await _context.Ratings
            .AnyAsync(r => r.RaterId == raterId &&
                          r.DeliveryId == deliveryId &&
                          r.TargetId == targetId, ct);
    }

    private static string GetGrade(decimal score)
    {
        return score switch
        {
            >= 90 => "A",
            >= 80 => "B",
            >= 70 => "C",
            >= 60 => "D",
            _ => "F"
        };
    }
}
