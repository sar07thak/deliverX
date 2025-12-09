using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.Common;
using DeliveryDost.Application.DTOs.SavedAddress;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class SavedAddressService : ISavedAddressService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SavedAddressService> _logger;

    public SavedAddressService(ApplicationDbContext context, ILogger<SavedAddressService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<GetSavedAddressesResponse>> GetUserAddressesAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var addresses = await _context.SavedAddresses
                .Where(a => a.UserId == userId && a.IsActive)
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.AddressName)
                .Select(a => new SavedAddressListDto
                {
                    Id = a.Id,
                    AddressName = a.AddressName,
                    AddressType = a.AddressType,
                    ShortAddress = $"{a.AddressLine1}, {a.City} - {a.Pincode}",
                    IsDefault = a.IsDefault,
                    IsPickupAddress = a.IsPickupAddress,
                    IsDropAddress = a.IsDropAddress,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    ContactName = a.ContactName,
                    ContactPhone = a.ContactPhone
                })
                .ToListAsync(cancellationToken);

            return Result<GetSavedAddressesResponse>.Success(new GetSavedAddressesResponse
            {
                Addresses = addresses,
                TotalCount = addresses.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching saved addresses for user {UserId}", userId);
            return Result<GetSavedAddressesResponse>.Failure("Failed to fetch saved addresses");
        }
    }

    public async Task<Result<GetSavedAddressesResponse>> GetUserAddressesByTypeAsync(Guid userId, bool? isPickup, bool? isDrop, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.SavedAddresses
                .Where(a => a.UserId == userId && a.IsActive);

            if (isPickup.HasValue && isPickup.Value)
                query = query.Where(a => a.IsPickupAddress);

            if (isDrop.HasValue && isDrop.Value)
                query = query.Where(a => a.IsDropAddress);

            var addresses = await query
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.AddressName)
                .Select(a => new SavedAddressListDto
                {
                    Id = a.Id,
                    AddressName = a.AddressName,
                    AddressType = a.AddressType,
                    ShortAddress = $"{a.AddressLine1}, {a.City} - {a.Pincode}",
                    IsDefault = a.IsDefault,
                    IsPickupAddress = a.IsPickupAddress,
                    IsDropAddress = a.IsDropAddress,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    ContactName = a.ContactName,
                    ContactPhone = a.ContactPhone
                })
                .ToListAsync(cancellationToken);

            return Result<GetSavedAddressesResponse>.Success(new GetSavedAddressesResponse
            {
                Addresses = addresses,
                TotalCount = addresses.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching saved addresses by type for user {UserId}", userId);
            return Result<GetSavedAddressesResponse>.Failure("Failed to fetch saved addresses");
        }
    }

    public async Task<Result<SavedAddressDto>> GetAddressByIdAsync(Guid addressId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var address = await _context.SavedAddresses
                .Where(a => a.Id == addressId && a.UserId == userId && a.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (address == null)
                return Result<SavedAddressDto>.Failure("Address not found");

            return Result<SavedAddressDto>.Success(MapToDto(address));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching address {AddressId} for user {UserId}", addressId, userId);
            return Result<SavedAddressDto>.Failure("Failed to fetch address");
        }
    }

    public async Task<Result<SavedAddressDto>> CreateAddressAsync(Guid userId, CreateSavedAddressRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // If this is the default address, unset other defaults
            if (request.IsDefault)
            {
                await UnsetDefaultAddresses(userId, cancellationToken);
            }

            var fullAddress = BuildFullAddress(request.AddressLine1, request.AddressLine2, request.City, request.State, request.Pincode);

            var address = new SavedAddress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AddressName = request.AddressName,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                State = request.State,
                Pincode = request.Pincode,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                FullAddress = fullAddress,
                ContactName = request.ContactName,
                ContactPhone = request.ContactPhone,
                AlternatePhone = request.AlternatePhone,
                ContactEmail = request.ContactEmail,
                WhatsAppNumber = request.WhatsAppNumber,
                AddressType = request.AddressType,
                IsDefault = request.IsDefault,
                IsPickupAddress = request.IsPickupAddress,
                IsDropAddress = request.IsDropAddress,
                DefaultInstructions = request.DefaultInstructions,
                Landmark = request.Landmark,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SavedAddresses.Add(address);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created saved address {AddressId} for user {UserId}", address.Id, userId);

            return Result<SavedAddressDto>.Success(MapToDto(address));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating saved address for user {UserId}", userId);
            return Result<SavedAddressDto>.Failure("Failed to create address");
        }
    }

    public async Task<Result<SavedAddressDto>> UpdateAddressAsync(Guid userId, UpdateSavedAddressRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var address = await _context.SavedAddresses
                .Where(a => a.Id == request.Id && a.UserId == userId && a.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (address == null)
                return Result<SavedAddressDto>.Failure("Address not found");

            // If this is becoming the default address, unset other defaults
            if (request.IsDefault && !address.IsDefault)
            {
                await UnsetDefaultAddresses(userId, cancellationToken);
            }

            var fullAddress = BuildFullAddress(request.AddressLine1, request.AddressLine2, request.City, request.State, request.Pincode);

            address.AddressName = request.AddressName;
            address.AddressLine1 = request.AddressLine1;
            address.AddressLine2 = request.AddressLine2;
            address.City = request.City;
            address.State = request.State;
            address.Pincode = request.Pincode;
            address.Latitude = request.Latitude;
            address.Longitude = request.Longitude;
            address.FullAddress = fullAddress;
            address.ContactName = request.ContactName;
            address.ContactPhone = request.ContactPhone;
            address.AlternatePhone = request.AlternatePhone;
            address.ContactEmail = request.ContactEmail;
            address.WhatsAppNumber = request.WhatsAppNumber;
            address.AddressType = request.AddressType;
            address.IsDefault = request.IsDefault;
            address.IsPickupAddress = request.IsPickupAddress;
            address.IsDropAddress = request.IsDropAddress;
            address.DefaultInstructions = request.DefaultInstructions;
            address.Landmark = request.Landmark;
            address.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated saved address {AddressId} for user {UserId}", address.Id, userId);

            return Result<SavedAddressDto>.Success(MapToDto(address));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating address {AddressId} for user {UserId}", request.Id, userId);
            return Result<SavedAddressDto>.Failure("Failed to update address");
        }
    }

    public async Task<Result<bool>> DeleteAddressAsync(Guid addressId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var address = await _context.SavedAddresses
                .Where(a => a.Id == addressId && a.UserId == userId && a.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (address == null)
                return Result<bool>.Failure("Address not found");

            // Soft delete
            address.IsActive = false;
            address.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted saved address {AddressId} for user {UserId}", addressId, userId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting address {AddressId} for user {UserId}", addressId, userId);
            return Result<bool>.Failure("Failed to delete address");
        }
    }

    public async Task<Result<bool>> SetDefaultAddressAsync(Guid addressId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var address = await _context.SavedAddresses
                .Where(a => a.Id == addressId && a.UserId == userId && a.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (address == null)
                return Result<bool>.Failure("Address not found");

            // Unset other defaults
            await UnsetDefaultAddresses(userId, cancellationToken);

            // Set this as default
            address.IsDefault = true;
            address.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Set default address {AddressId} for user {UserId}", addressId, userId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default address {AddressId} for user {UserId}", addressId, userId);
            return Result<bool>.Failure("Failed to set default address");
        }
    }

    public async Task<Result<SavedAddressDto?>> GetDefaultAddressAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var address = await _context.SavedAddresses
                .Where(a => a.UserId == userId && a.IsActive && a.IsDefault)
                .FirstOrDefaultAsync(cancellationToken);

            if (address == null)
                return Result<SavedAddressDto?>.Success(null);

            return Result<SavedAddressDto?>.Success(MapToDto(address));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching default address for user {UserId}", userId);
            return Result<SavedAddressDto?>.Failure("Failed to fetch default address");
        }
    }

    #region Private Helpers

    private async Task UnsetDefaultAddresses(Guid userId, CancellationToken cancellationToken)
    {
        var defaultAddresses = await _context.SavedAddresses
            .Where(a => a.UserId == userId && a.IsActive && a.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var addr in defaultAddresses)
        {
            addr.IsDefault = false;
            addr.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static string BuildFullAddress(string line1, string? line2, string city, string state, string pincode)
    {
        var parts = new List<string> { line1 };
        if (!string.IsNullOrEmpty(line2))
            parts.Add(line2);
        parts.Add(city);
        parts.Add(state);
        parts.Add(pincode);
        return string.Join(", ", parts);
    }

    private static SavedAddressDto MapToDto(SavedAddress address)
    {
        return new SavedAddressDto
        {
            Id = address.Id,
            UserId = address.UserId,
            AddressName = address.AddressName,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            State = address.State,
            Pincode = address.Pincode,
            Latitude = address.Latitude,
            Longitude = address.Longitude,
            FullAddress = address.FullAddress,
            ContactName = address.ContactName,
            ContactPhone = address.ContactPhone,
            AlternatePhone = address.AlternatePhone,
            ContactEmail = address.ContactEmail,
            WhatsAppNumber = address.WhatsAppNumber,
            AddressType = address.AddressType,
            IsDefault = address.IsDefault,
            IsPickupAddress = address.IsPickupAddress,
            IsDropAddress = address.IsDropAddress,
            DefaultInstructions = address.DefaultInstructions,
            Landmark = address.Landmark,
            IsActive = address.IsActive,
            CreatedAt = address.CreatedAt,
            UpdatedAt = address.UpdatedAt
        };
    }

    #endregion
}
