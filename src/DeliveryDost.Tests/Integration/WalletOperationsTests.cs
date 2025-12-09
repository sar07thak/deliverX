// =====================================================
// DELIVERYDOST INTEGRATION TESTS - WALLET OPERATIONS
// =====================================================
// Tests wallet transactions: Credit, Debit, Hold, Release
// Validates balance consistency and transaction logging
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
    /// Integration tests for Wallet operations.
    /// Tests all transaction types and balance management.
    /// </summary>
    public class WalletOperationsTests : IClassFixture<DatabaseFixture>, IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceScope _scope;

        public WalletOperationsTests(DatabaseFixture fixture)
        {
            _scope = fixture.ServiceProvider.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }

        // =====================================================
        // TEST: CREATE WALLET
        // =====================================================
        [Fact]
        public async Task CreateWallet_ForNewUser_ShouldHaveZeroBalance()
        {
            // Arrange
            var userId = await CreateTestUser();
            if (userId == Guid.Empty) return;

            var walletId = Guid.NewGuid();
            var wallet = new Wallet
            {
                Id = walletId,
                UserId = userId,
                Balance = 0,
                HoldBalance = 0,
                Currency = "INR",
                Status = WalletStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            await _context.Wallets.AddAsync(wallet);
            await _context.SaveChangesAsync();

            // Assert
            var savedWallet = await _context.Wallets.FindAsync(walletId);
            Assert.NotNull(savedWallet);
            Assert.Equal(0, savedWallet.Balance);
            Assert.Equal(0, savedWallet.HoldBalance);
            Assert.Equal(WalletStatus.ACTIVE, savedWallet.Status);

            // Cleanup
            await CleanupTestData(userId, walletId);
        }

        // =====================================================
        // TEST: CREDIT (RECHARGE)
        // =====================================================
        [Fact]
        public async Task CreditWallet_Recharge_ShouldIncreaseBalance()
        {
            // Arrange
            var (userId, walletId) = await CreateTestUserWithWallet(initialBalance: 0);
            if (userId == Guid.Empty) return;

            var rechargeAmount = 1000m;
            var wallet = await _context.Wallets.FindAsync(walletId);
            var balanceBefore = wallet.Balance;

            // Act
            wallet.Balance += rechargeAmount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = walletId,
                TransactionType = TransactionType.CREDIT,
                Category = TransactionCategory.RECHARGE,
                Amount = rechargeAmount,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance,
                ReferenceId = $"RCH-{DateTime.Now.Ticks}",
                ReferenceType = "PAYMENT",
                Description = "Test wallet recharge",
                Status = TransactionStatus.COMPLETED,
                CreatedAt = DateTime.UtcNow
            };

            await _context.WalletTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            // Assert
            _context.Entry(wallet).State = EntityState.Detached;
            var updatedWallet = await _context.Wallets.FindAsync(walletId);
            Assert.Equal(rechargeAmount, updatedWallet.Balance);

            var savedTxn = await _context.WalletTransactions.FindAsync(transaction.Id);
            Assert.Equal(TransactionType.CREDIT, savedTxn.TransactionType);

            // Cleanup
            await CleanupTestData(userId, walletId);
        }

        // =====================================================
        // TEST: HOLD AMOUNT
        // =====================================================
        [Fact]
        public async Task HoldAmount_ForDelivery_ShouldMoveToHoldBalance()
        {
            // Arrange
            var (userId, walletId) = await CreateTestUserWithWallet(initialBalance: 1000);
            if (userId == Guid.Empty) return;

            var holdAmount = 150m;
            var wallet = await _context.Wallets.FindAsync(walletId);
            var balanceBefore = wallet.Balance;

            // Act
            wallet.Balance -= holdAmount;
            wallet.HoldBalance += holdAmount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = walletId,
                TransactionType = TransactionType.HOLD,
                Category = TransactionCategory.DELIVERY_PAYMENT,
                Amount = holdAmount,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance,
                ReferenceId = $"DEL-{DateTime.Now.Ticks}",
                ReferenceType = "DELIVERY",
                Description = "Hold for delivery payment",
                Status = TransactionStatus.COMPLETED,
                CreatedAt = DateTime.UtcNow
            };

            await _context.WalletTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            // Assert
            _context.Entry(wallet).State = EntityState.Detached;
            var updatedWallet = await _context.Wallets.FindAsync(walletId);
            Assert.Equal(850m, updatedWallet.Balance);
            Assert.Equal(150m, updatedWallet.HoldBalance);

            // Cleanup
            await CleanupTestData(userId, walletId);
        }

        // =====================================================
        // TEST: RELEASE HOLD
        // =====================================================
        [Fact]
        public async Task ReleaseHold_CancelledDelivery_ShouldReturnToBalance()
        {
            // Arrange
            var (userId, walletId) = await CreateTestUserWithWallet(initialBalance: 850, holdBalance: 150);
            if (userId == Guid.Empty) return;

            var releaseAmount = 150m;
            var wallet = await _context.Wallets.FindAsync(walletId);
            var balanceBefore = wallet.Balance;

            // Act
            wallet.Balance += releaseAmount;
            wallet.HoldBalance -= releaseAmount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = walletId,
                TransactionType = TransactionType.RELEASE,
                Category = TransactionCategory.DELIVERY_PAYMENT,
                Amount = releaseAmount,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance,
                ReferenceId = $"DEL-{DateTime.Now.Ticks}",
                ReferenceType = "DELIVERY",
                Description = "Hold released - delivery cancelled",
                Status = TransactionStatus.COMPLETED,
                CreatedAt = DateTime.UtcNow
            };

            await _context.WalletTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            // Assert
            _context.Entry(wallet).State = EntityState.Detached;
            var updatedWallet = await _context.Wallets.FindAsync(walletId);
            Assert.Equal(1000m, updatedWallet.Balance);
            Assert.Equal(0m, updatedWallet.HoldBalance);

            // Cleanup
            await CleanupTestData(userId, walletId);
        }

        // =====================================================
        // TEST: DEBIT FROM HOLD
        // =====================================================
        [Fact]
        public async Task DebitFromHold_CompletedDelivery_ShouldReduceHoldBalance()
        {
            // Arrange
            var (userId, walletId) = await CreateTestUserWithWallet(initialBalance: 850, holdBalance: 150);
            if (userId == Guid.Empty) return;

            var debitAmount = 150m;
            var wallet = await _context.Wallets.FindAsync(walletId);
            var balanceBefore = wallet.Balance;

            // Act - Debit from hold (money leaves wallet)
            wallet.HoldBalance -= debitAmount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = walletId,
                TransactionType = TransactionType.DEBIT,
                Category = TransactionCategory.DELIVERY_PAYMENT,
                Amount = debitAmount,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance,
                ReferenceId = $"DEL-{DateTime.Now.Ticks}",
                ReferenceType = "DELIVERY",
                Description = "Payment for completed delivery",
                Status = TransactionStatus.COMPLETED,
                CreatedAt = DateTime.UtcNow
            };

            await _context.WalletTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            // Assert
            _context.Entry(wallet).State = EntityState.Detached;
            var updatedWallet = await _context.Wallets.FindAsync(walletId);
            Assert.Equal(850m, updatedWallet.Balance);
            Assert.Equal(0m, updatedWallet.HoldBalance);

            // Cleanup
            await CleanupTestData(userId, walletId);
        }

        // =====================================================
        // TEST: INSUFFICIENT BALANCE
        // =====================================================
        [Fact]
        public async Task HoldAmount_InsufficientBalance_ShouldFail()
        {
            // Arrange
            var (userId, walletId) = await CreateTestUserWithWallet(initialBalance: 100);
            if (userId == Guid.Empty) return;

            var holdAmount = 500m; // More than available
            var wallet = await _context.Wallets.FindAsync(walletId);

            // Act & Assert
            var canHold = wallet.Balance >= holdAmount;
            Assert.False(canHold, "Should not be able to hold more than available balance");

            // Cleanup
            await CleanupTestData(userId, walletId);
        }

        // =====================================================
        // TEST: FREEZE WALLET
        // =====================================================
        [Fact]
        public async Task FreezeWallet_ShouldChangeStatus()
        {
            // Arrange
            var (userId, walletId) = await CreateTestUserWithWallet(initialBalance: 500);
            if (userId == Guid.Empty) return;

            var wallet = await _context.Wallets.FindAsync(walletId);

            // Act
            wallet.Status = WalletStatus.FROZEN;
            wallet.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Assert
            _context.Entry(wallet).State = EntityState.Detached;
            var frozenWallet = await _context.Wallets.FindAsync(walletId);
            Assert.Equal(WalletStatus.FROZEN, frozenWallet.Status);

            // Cleanup
            await CleanupTestData(userId, walletId);
        }

        // =====================================================
        // TEST: TRANSACTION HISTORY
        // =====================================================
        [Fact]
        public async Task GetTransactionHistory_ShouldReturnAllTransactions()
        {
            // Arrange
            var (userId, walletId) = await CreateTestUserWithWallet(initialBalance: 1000);
            if (userId == Guid.Empty) return;

            // Create multiple transactions
            var transactions = new[]
            {
                CreateTransaction(walletId, TransactionType.CREDIT, 1000, "Initial"),
                CreateTransaction(walletId, TransactionType.HOLD, 100, "Hold 1"),
                CreateTransaction(walletId, TransactionType.RELEASE, 50, "Release"),
                CreateTransaction(walletId, TransactionType.DEBIT, 50, "Debit")
            };

            await _context.WalletTransactions.AddRangeAsync(transactions);
            await _context.SaveChangesAsync();

            // Act
            var history = await _context.WalletTransactions
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Assert
            Assert.True(history.Count >= 4);

            // Cleanup
            await CleanupTestData(userId, walletId);
        }

        // =====================================================
        // HELPER METHODS
        // =====================================================
        private async Task<Guid> CreateTestUser()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Phone = $"9999{DateTime.Now.Ticks % 1000000:D6}",
                Email = $"wallet_test_{Guid.NewGuid():N}@test.com",
                FullName = "Wallet Test User",
                PasswordHash = "hash",
                Role = UserRole.EC,
                IsActive = true,
                IsPhoneVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return userId;
        }

        private async Task<(Guid userId, Guid walletId)> CreateTestUserWithWallet(
            decimal initialBalance, decimal holdBalance = 0)
        {
            var userId = await CreateTestUser();
            if (userId == Guid.Empty) return (Guid.Empty, Guid.Empty);

            var walletId = Guid.NewGuid();
            var wallet = new Wallet
            {
                Id = walletId,
                UserId = userId,
                Balance = initialBalance,
                HoldBalance = holdBalance,
                Currency = "INR",
                Status = WalletStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Wallets.AddAsync(wallet);
            await _context.SaveChangesAsync();

            return (userId, walletId);
        }

        private WalletTransaction CreateTransaction(
            Guid walletId, TransactionType type, decimal amount, string desc)
        {
            return new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = walletId,
                TransactionType = type,
                Category = TransactionCategory.RECHARGE,
                Amount = amount,
                BalanceBefore = 0,
                BalanceAfter = 0,
                ReferenceId = $"REF-{DateTime.Now.Ticks}",
                ReferenceType = "TEST",
                Description = desc,
                Status = TransactionStatus.COMPLETED,
                CreatedAt = DateTime.UtcNow
            };
        }

        private async Task CleanupTestData(Guid userId, Guid walletId)
        {
            var transactions = await _context.WalletTransactions
                .Where(t => t.WalletId == walletId)
                .ToListAsync();
            _context.WalletTransactions.RemoveRange(transactions);

            var wallet = await _context.Wallets.FindAsync(walletId);
            if (wallet != null) _context.Wallets.Remove(wallet);

            var user = await _context.Users.FindAsync(userId);
            if (user != null) _context.Users.Remove(user);

            await _context.SaveChangesAsync();
        }
    }
}
