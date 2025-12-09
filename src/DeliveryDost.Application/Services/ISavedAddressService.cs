using DeliveryDost.Application.Common;
using DeliveryDost.Application.DTOs.SavedAddress;

namespace DeliveryDost.Application.Services;

/// <summary>
/// Service for managing user saved addresses
/// </summary>
public interface ISavedAddressService
{
    /// <summary>
    /// Get all saved addresses for a user
    /// </summary>
    Task<Result<GetSavedAddressesResponse>> GetUserAddressesAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Get saved addresses filtered by type (pickup/drop)
    /// </summary>
    Task<Result<GetSavedAddressesResponse>> GetUserAddressesByTypeAsync(Guid userId, bool? isPickup, bool? isDrop, CancellationToken cancellationToken);

    /// <summary>
    /// Get a single saved address by ID
    /// </summary>
    Task<Result<SavedAddressDto>> GetAddressByIdAsync(Guid addressId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Create a new saved address
    /// </summary>
    Task<Result<SavedAddressDto>> CreateAddressAsync(Guid userId, CreateSavedAddressRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Update an existing saved address
    /// </summary>
    Task<Result<SavedAddressDto>> UpdateAddressAsync(Guid userId, UpdateSavedAddressRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Delete a saved address (soft delete)
    /// </summary>
    Task<Result<bool>> DeleteAddressAsync(Guid addressId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Set an address as default
    /// </summary>
    Task<Result<bool>> SetDefaultAddressAsync(Guid addressId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Get default address for a user
    /// </summary>
    Task<Result<SavedAddressDto?>> GetDefaultAddressAsync(Guid userId, CancellationToken cancellationToken);
}
