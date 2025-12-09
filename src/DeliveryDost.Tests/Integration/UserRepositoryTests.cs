// =====================================================
// DELIVERYDOST INTEGRATION TESTS - USER REPOSITORY
// =====================================================
// Uses xUnit with clean architecture patterns
// Tests repository operations against actual database
// =====================================================

using System;
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
    /// Integration tests for User repository operations.
    /// Uses a real database connection for testing.
    /// </summary>
    public class UserRepositoryTests : IClassFixture<DatabaseFixture>, IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceScope _scope;

        public UserRepositoryTests(DatabaseFixture fixture)
        {
            _scope = fixture.ServiceProvider.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }

        // =====================================================
        // TEST: CREATE USER
        // =====================================================
        [Fact]
        public async Task CreateUser_WithValidData_ShouldSucceed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Phone = $"9999{DateTime.Now.Ticks % 1000000:D6}",
                Email = $"test_{Guid.NewGuid():N}@test.com",
                FullName = "Integration Test User",
                PasswordHash = "AQAAAAIAAYagAAAAEK...",
                Role = UserRole.EC,
                IsActive = true,
                IsPhoneVerified = true,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            await _context.Users.AddAsync(user);
            var result = await _context.SaveChangesAsync();

            // Assert
            Assert.True(result > 0);

            var savedUser = await _context.Users.FindAsync(userId);
            Assert.NotNull(savedUser);
            Assert.Equal(user.Phone, savedUser.Phone);
            Assert.Equal(user.FullName, savedUser.FullName);
            Assert.Equal(UserRole.EC, savedUser.Role);

            // Cleanup
            _context.Users.Remove(savedUser);
            await _context.SaveChangesAsync();
        }

        // =====================================================
        // TEST: GET USER BY ID
        // =====================================================
        [Fact]
        public async Task GetUserById_ExistingUser_ShouldReturnUser()
        {
            // Arrange - Get any existing user
            var existingUser = await _context.Users.FirstOrDefaultAsync();

            if (existingUser == null)
            {
                // Skip if no users exist
                return;
            }

            // Act
            var user = await _context.Users.FindAsync(existingUser.Id);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(existingUser.Id, user.Id);
            Assert.Equal(existingUser.Phone, user.Phone);
        }

        // =====================================================
        // TEST: GET USER BY PHONE
        // =====================================================
        [Fact]
        public async Task GetUserByPhone_ExistingPhone_ShouldReturnUser()
        {
            // Arrange - Get any existing user's phone
            var existingUser = await _context.Users.FirstOrDefaultAsync();

            if (existingUser == null) return;

            // Act
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == existingUser.Phone);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(existingUser.Id, user.Id);
        }

        // =====================================================
        // TEST: UPDATE USER
        // =====================================================
        [Fact]
        public async Task UpdateUser_ModifyFullName_ShouldPersist()
        {
            // Arrange - Create test user
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Phone = $"9888{DateTime.Now.Ticks % 1000000:D6}",
                Email = $"update_test_{Guid.NewGuid():N}@test.com",
                FullName = "Original Name",
                PasswordHash = "hash",
                Role = UserRole.EC,
                IsActive = true,
                IsPhoneVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act - Update the user
            user.FullName = "Updated Name";
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Assert
            _context.Entry(user).State = EntityState.Detached;
            var updatedUser = await _context.Users.FindAsync(userId);

            Assert.NotNull(updatedUser);
            Assert.Equal("Updated Name", updatedUser.FullName);

            // Cleanup
            _context.Users.Remove(updatedUser);
            await _context.SaveChangesAsync();
        }

        // =====================================================
        // TEST: DELETE USER
        // =====================================================
        [Fact]
        public async Task DeleteUser_ExistingUser_ShouldRemove()
        {
            // Arrange - Create test user
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Phone = $"9777{DateTime.Now.Ticks % 1000000:D6}",
                Email = $"delete_test_{Guid.NewGuid():N}@test.com",
                FullName = "Delete Test User",
                PasswordHash = "hash",
                Role = UserRole.EC,
                IsActive = true,
                IsPhoneVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Assert
            var deletedUser = await _context.Users.FindAsync(userId);
            Assert.Null(deletedUser);
        }

        // =====================================================
        // TEST: LIST USERS BY ROLE
        // =====================================================
        [Fact]
        public async Task ListUsersByRole_ShouldFilterCorrectly()
        {
            // Arrange
            var role = UserRole.EC;

            // Act
            var users = await _context.Users
                .Where(u => u.Role == role && u.IsActive)
                .Take(10)
                .ToListAsync();

            // Assert
            Assert.All(users, u => Assert.Equal(role, u.Role));
        }

        // =====================================================
        // TEST: UNIQUE PHONE CONSTRAINT
        // =====================================================
        [Fact]
        public async Task CreateUser_DuplicatePhone_ShouldThrowException()
        {
            // Arrange - Get existing phone
            var existingUser = await _context.Users.FirstOrDefaultAsync();

            if (existingUser == null) return;

            var duplicateUser = new User
            {
                Id = Guid.NewGuid(),
                Phone = existingUser.Phone, // Duplicate!
                Email = $"dup_test_{Guid.NewGuid():N}@test.com",
                FullName = "Duplicate Phone Test",
                PasswordHash = "hash",
                Role = UserRole.EC,
                IsActive = true,
                IsPhoneVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act & Assert
            await _context.Users.AddAsync(duplicateUser);
            await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());

            // Cleanup - detach failed entity
            _context.Entry(duplicateUser).State = EntityState.Detached;
        }

        // =====================================================
        // TEST: PAGINATION
        // =====================================================
        [Fact]
        public async Task ListUsers_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var pageSize = 10;
            var pageNumber = 1;

            // Act
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCount = await _context.Users.CountAsync();

            // Assert
            Assert.True(users.Count <= pageSize);
            Assert.True(totalCount >= users.Count);
        }

        // =====================================================
        // TEST: SEARCH BY NAME
        // =====================================================
        [Fact]
        public async Task SearchUsers_ByPartialName_ShouldFindMatches()
        {
            // Arrange
            var searchTerm = "Test";

            // Act
            var users = await _context.Users
                .Where(u => u.FullName.Contains(searchTerm))
                .Take(10)
                .ToListAsync();

            // Assert
            Assert.All(users, u => Assert.Contains(searchTerm, u.FullName, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Database fixture for integration tests.
    /// Sets up the service provider with the test database.
    /// </summary>
    public class DatabaseFixture : IDisposable
    {
        public IServiceProvider ServiceProvider { get; private set; }

        public DatabaseFixture()
        {
            var services = new ServiceCollection();

            // Configure test database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    "Server=localhost;Database=DeliveryDost_Dev;Trusted_Connection=True;TrustServerCertificate=True;",
                    b => b.MigrationsAssembly("DeliveryDost.Infrastructure")
                ));

            ServiceProvider = services.BuildServiceProvider();
        }

        public void Dispose()
        {
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
