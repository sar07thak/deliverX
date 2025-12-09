using System;
using System.Threading;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs.Rating;

namespace DeliveryDost.Application.Services;

public interface IRatingService
{
    Task<CreateRatingResponse> CreateRatingAsync(Guid raterId, string raterType, CreateRatingRequest request, CancellationToken ct = default);
    Task<RatingSummaryResponse> GetRatingSummaryAsync(Guid targetId, CancellationToken ct = default);
    Task<GetRatingsResponse> GetRatingsAsync(GetRatingsRequest request, CancellationToken ct = default);
    Task<BehaviorIndexResponse> GetBehaviorIndexAsync(Guid userId, CancellationToken ct = default);
    Task<BehaviorIndexResponse> RecalculateBehaviorIndexAsync(Guid userId, CancellationToken ct = default);
    Task<bool> HasRatedDeliveryAsync(Guid raterId, Guid deliveryId, Guid targetId, CancellationToken ct = default);
}
