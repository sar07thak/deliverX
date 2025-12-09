// =====================================================
// DELIVERYDOST INTEGRATION TESTS - DELIVERY WORKFLOW
// =====================================================
// Tests complete delivery lifecycle through repository
// Validates status transitions and business rules
// =====================================================

using System;
using System.Linq;
using System.Threading.Tasks;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Domain.Enums;
using DeliveryDost.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DeliveryDost.Tests.Integration
{
    /// <summary>
    /// Integration tests for Delivery workflow operations.
    /// Tests the complete delivery lifecycle.
    /// </summary>
    public class DeliveryWorkflowTests : IClassFixture<DatabaseFixture>, IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceScope _scope;

        public DeliveryWorkflowTests(DatabaseFixture fixture)
        {
            _scope = fixture.ServiceProvider.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }

        // =====================================================
        // TEST: CREATE DELIVERY
        // =====================================================
        [Fact]
        public async Task CreateDelivery_WithValidData_ShouldCreateWithPendingStatus()
        {
            // Arrange
            var requester = await _context.Users
                .FirstOrDefaultAsync(u => u.Role == UserRole.BC && u.IsActive);

            var packageType = await _context.MasterPackageTypes
                .FirstOrDefaultAsync(p => p.IsActive);

            if (requester == null || packageType == null)
            {
                // Skip if prerequisites not met
                return;
            }

            var deliveryId = Guid.NewGuid();
            var delivery = new Delivery
            {
                Id = deliveryId,
                DeliveryNumber = $"DEL-TEST-{DateTime.Now:yyyyMMddHHmmss}",
                RequesterId = requester.Id,
                RequesterType = "BC",
                PackageTypeId = packageType.Id,
                PackageDescription = "Integration Test Package",
                PackageWeight = 2.5m,
                PickupAddress = "Test Pickup Address",
                PickupCity = "Jaipur",
                PickupState = "Rajasthan",
                PickupPincode = "302001",
                PickupLatitude = 26.9124m,
                PickupLongitude = 75.7873m,
                PickupContactName = "Test Contact",
                PickupContactPhone = "9876543210",
                DropAddress = "Test Drop Address",
                DropCity = "Jaipur",
                DropState = "Rajasthan",
                DropPincode = "302017",
                DropLatitude = 26.8650m,
                DropLongitude = 75.8120m,
                DropContactName = "Drop Contact",
                DropContactPhone = "9876543211",
                DistanceKm = 8.5m,
                EstimatedDurationMinutes = 25,
                BasePrice = 150m,
                EstimatedPrice = 150m,
                Status = DeliveryStatus.PENDING,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            await _context.Deliveries.AddAsync(delivery);
            await _context.SaveChangesAsync();

            // Assert
            var savedDelivery = await _context.Deliveries.FindAsync(deliveryId);
            Assert.NotNull(savedDelivery);
            Assert.Equal(DeliveryStatus.PENDING, savedDelivery.Status);

            // Cleanup
            _context.Deliveries.Remove(savedDelivery);
            await _context.SaveChangesAsync();
        }

        // =====================================================
        // TEST: STATUS TRANSITIONS
        // =====================================================
        [Theory]
        [InlineData(DeliveryStatus.PENDING, DeliveryStatus.MATCHING)]
        [InlineData(DeliveryStatus.MATCHING, DeliveryStatus.ASSIGNED)]
        [InlineData(DeliveryStatus.ASSIGNED, DeliveryStatus.ACCEPTED)]
        [InlineData(DeliveryStatus.ACCEPTED, DeliveryStatus.REACHED_PICKUP)]
        [InlineData(DeliveryStatus.REACHED_PICKUP, DeliveryStatus.PICKED_UP)]
        [InlineData(DeliveryStatus.PICKED_UP, DeliveryStatus.IN_TRANSIT)]
        [InlineData(DeliveryStatus.IN_TRANSIT, DeliveryStatus.REACHED_DROP)]
        [InlineData(DeliveryStatus.REACHED_DROP, DeliveryStatus.DELIVERED)]
        public async Task StatusTransition_ValidTransition_ShouldSucceed(
            DeliveryStatus fromStatus, DeliveryStatus toStatus)
        {
            // Arrange
            var delivery = await CreateTestDelivery(fromStatus);
            if (delivery == null) return;

            // Act
            delivery.Status = toStatus;
            delivery.UpdatedAt = DateTime.UtcNow;

            // Set appropriate timestamp based on status
            SetStatusTimestamp(delivery, toStatus);

            _context.Deliveries.Update(delivery);
            await _context.SaveChangesAsync();

            // Assert
            _context.Entry(delivery).State = EntityState.Detached;
            var updatedDelivery = await _context.Deliveries.FindAsync(delivery.Id);
            Assert.Equal(toStatus, updatedDelivery.Status);

            // Cleanup
            await CleanupTestDelivery(delivery.Id);
        }

        // =====================================================
        // TEST: ASSIGN DELIVERY PARTNER
        // =====================================================
        [Fact]
        public async Task AssignDeliveryPartner_ShouldUpdateAssignment()
        {
            // Arrange
            var delivery = await CreateTestDelivery(DeliveryStatus.MATCHING);
            if (delivery == null) return;

            var dp = await _context.DeliveryPartnerProfiles
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.IsVerified && d.Status == DPStatus.ACTIVE);

            if (dp == null)
            {
                await CleanupTestDelivery(delivery.Id);
                return;
            }

            // Act
            delivery.Status = DeliveryStatus.ASSIGNED;
            delivery.AssignedDPId = dp.UserId;
            delivery.AssignedAt = DateTime.UtcNow;
            delivery.UpdatedAt = DateTime.UtcNow;

            _context.Deliveries.Update(delivery);
            await _context.SaveChangesAsync();

            // Assert
            _context.Entry(delivery).State = EntityState.Detached;
            var updatedDelivery = await _context.Deliveries.FindAsync(delivery.Id);

            Assert.Equal(DeliveryStatus.ASSIGNED, updatedDelivery.Status);
            Assert.Equal(dp.UserId, updatedDelivery.AssignedDPId);
            Assert.NotNull(updatedDelivery.AssignedAt);

            // Cleanup
            await CleanupTestDelivery(delivery.Id);
        }

        // =====================================================
        // TEST: COMPLETE DELIVERY
        // =====================================================
        [Fact]
        public async Task CompleteDelivery_ShouldSetFinalPriceAndTimestamp()
        {
            // Arrange
            var delivery = await CreateTestDelivery(DeliveryStatus.REACHED_DROP);
            if (delivery == null) return;

            // Act
            delivery.Status = DeliveryStatus.DELIVERED;
            delivery.DeliveredAt = DateTime.UtcNow;
            delivery.FinalPrice = delivery.EstimatedPrice;
            delivery.OTPVerified = true;
            delivery.UpdatedAt = DateTime.UtcNow;

            _context.Deliveries.Update(delivery);
            await _context.SaveChangesAsync();

            // Assert
            _context.Entry(delivery).State = EntityState.Detached;
            var completed = await _context.Deliveries.FindAsync(delivery.Id);

            Assert.Equal(DeliveryStatus.DELIVERED, completed.Status);
            Assert.NotNull(completed.DeliveredAt);
            Assert.NotNull(completed.FinalPrice);
            Assert.True(completed.OTPVerified);

            // Cleanup
            await CleanupTestDelivery(delivery.Id);
        }

        // =====================================================
        // TEST: CANCEL DELIVERY
        // =====================================================
        [Fact]
        public async Task CancelDelivery_FromPending_ShouldSucceed()
        {
            // Arrange
            var delivery = await CreateTestDelivery(DeliveryStatus.PENDING);
            if (delivery == null) return;

            // Act
            delivery.Status = DeliveryStatus.CANCELLED;
            delivery.CancellationReason = "Customer requested cancellation";
            delivery.CancelledAt = DateTime.UtcNow;
            delivery.UpdatedAt = DateTime.UtcNow;

            _context.Deliveries.Update(delivery);
            await _context.SaveChangesAsync();

            // Assert
            _context.Entry(delivery).State = EntityState.Detached;
            var cancelled = await _context.Deliveries.FindAsync(delivery.Id);

            Assert.Equal(DeliveryStatus.CANCELLED, cancelled.Status);
            Assert.NotNull(cancelled.CancelledAt);
            Assert.NotNull(cancelled.CancellationReason);

            // Cleanup
            await CleanupTestDelivery(delivery.Id);
        }

        // =====================================================
        // TEST: ADD DELIVERY EVENT
        // =====================================================
        [Fact]
        public async Task AddDeliveryEvent_ShouldCreateEventRecord()
        {
            // Arrange
            var delivery = await CreateTestDelivery(DeliveryStatus.PENDING);
            if (delivery == null) return;

            var eventId = Guid.NewGuid();
            var deliveryEvent = new DeliveryEvent
            {
                Id = eventId,
                DeliveryId = delivery.Id,
                Status = "PENDING",
                EventTime = DateTime.UtcNow,
                Description = "Delivery request created via integration test",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            await _context.DeliveryEvents.AddAsync(deliveryEvent);
            await _context.SaveChangesAsync();

            // Assert
            var savedEvent = await _context.DeliveryEvents.FindAsync(eventId);
            Assert.NotNull(savedEvent);
            Assert.Equal(delivery.Id, savedEvent.DeliveryId);

            // Cleanup
            _context.DeliveryEvents.Remove(savedEvent);
            await CleanupTestDelivery(delivery.Id);
        }

        // =====================================================
        // TEST: GET DELIVERIES BY STATUS
        // =====================================================
        [Fact]
        public async Task GetDeliveriesByStatus_ShouldFilterCorrectly()
        {
            // Arrange
            var status = DeliveryStatus.DELIVERED;

            // Act
            var deliveries = await _context.Deliveries
                .Where(d => d.Status == status)
                .Take(10)
                .ToListAsync();

            // Assert
            Assert.All(deliveries, d => Assert.Equal(status, d.Status));
        }

        // =====================================================
        // HELPER METHODS
        // =====================================================
        private async Task<Delivery> CreateTestDelivery(DeliveryStatus initialStatus)
        {
            var requester = await _context.Users
                .FirstOrDefaultAsync(u => u.Role == UserRole.BC && u.IsActive);

            var packageType = await _context.MasterPackageTypes
                .FirstOrDefaultAsync(p => p.IsActive);

            if (requester == null || packageType == null) return null;

            var dp = await _context.DeliveryPartnerProfiles
                .FirstOrDefaultAsync(d => d.IsVerified && d.Status == DPStatus.ACTIVE);

            var delivery = new Delivery
            {
                Id = Guid.NewGuid(),
                DeliveryNumber = $"DEL-TEST-{DateTime.Now.Ticks}",
                RequesterId = requester.Id,
                RequesterType = "BC",
                PackageTypeId = packageType.Id,
                PackageDescription = "Test Package",
                PackageWeight = 1m,
                PickupAddress = "Test Pickup",
                PickupCity = "Jaipur",
                PickupState = "Rajasthan",
                PickupPincode = "302001",
                PickupLatitude = 26.9124m,
                PickupLongitude = 75.7873m,
                PickupContactName = "Pickup",
                PickupContactPhone = "9999999999",
                DropAddress = "Test Drop",
                DropCity = "Jaipur",
                DropState = "Rajasthan",
                DropPincode = "302017",
                DropLatitude = 26.8650m,
                DropLongitude = 75.8120m,
                DropContactName = "Drop",
                DropContactPhone = "9999999998",
                DistanceKm = 5m,
                EstimatedDurationMinutes = 15,
                BasePrice = 100m,
                EstimatedPrice = 100m,
                Status = initialStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Set DP for certain statuses
            if (initialStatus >= DeliveryStatus.ASSIGNED && dp != null)
            {
                delivery.AssignedDPId = dp.UserId;
                delivery.AssignedAt = DateTime.UtcNow;
            }

            await _context.Deliveries.AddAsync(delivery);
            await _context.SaveChangesAsync();

            return delivery;
        }

        private void SetStatusTimestamp(Delivery delivery, DeliveryStatus status)
        {
            switch (status)
            {
                case DeliveryStatus.ASSIGNED:
                    delivery.AssignedAt = DateTime.UtcNow;
                    break;
                case DeliveryStatus.ACCEPTED:
                    delivery.AcceptedAt = DateTime.UtcNow;
                    break;
                case DeliveryStatus.REACHED_PICKUP:
                    delivery.ReachedPickupAt = DateTime.UtcNow;
                    break;
                case DeliveryStatus.PICKED_UP:
                    delivery.PickedUpAt = DateTime.UtcNow;
                    break;
                case DeliveryStatus.REACHED_DROP:
                    delivery.ReachedDropAt = DateTime.UtcNow;
                    break;
                case DeliveryStatus.DELIVERED:
                    delivery.DeliveredAt = DateTime.UtcNow;
                    delivery.FinalPrice = delivery.EstimatedPrice;
                    break;
            }
        }

        private async Task CleanupTestDelivery(Guid deliveryId)
        {
            var events = await _context.DeliveryEvents
                .Where(e => e.DeliveryId == deliveryId)
                .ToListAsync();
            _context.DeliveryEvents.RemoveRange(events);

            var delivery = await _context.Deliveries.FindAsync(deliveryId);
            if (delivery != null)
            {
                _context.Deliveries.Remove(delivery);
            }

            await _context.SaveChangesAsync();
        }
    }
}
