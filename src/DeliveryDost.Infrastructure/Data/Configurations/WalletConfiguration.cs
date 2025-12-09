using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.WalletType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(w => w.Balance)
            .HasPrecision(18, 2);

        builder.Property(w => w.HoldBalance)
            .HasPrecision(18, 2);

        builder.Property(w => w.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("INR");

        builder.HasIndex(w => w.UserId).IsUnique();

        builder.HasOne(w => w.User)
            .WithOne()
            .HasForeignKey<Wallet>(w => w.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("WalletTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TransactionType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Category)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Property(t => t.BalanceBefore)
            .HasPrecision(18, 2);

        builder.Property(t => t.BalanceAfter)
            .HasPrecision(18, 2);

        builder.Property(t => t.ReferenceId)
            .HasMaxLength(100);

        builder.Property(t => t.ReferenceType)
            .HasMaxLength(30);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.Status)
            .HasMaxLength(20);

        builder.HasIndex(t => t.WalletId);
        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => new { t.ReferenceId, t.ReferenceType });

        builder.HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PaymentNumber)
            .HasMaxLength(25)
            .IsRequired();

        builder.Property(p => p.PaymentType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Property(p => p.PlatformFee)
            .HasPrecision(18, 2);

        builder.Property(p => p.Tax)
            .HasPrecision(18, 2);

        builder.Property(p => p.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(p => p.PaymentMethod)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.PaymentGateway)
            .HasMaxLength(30);

        builder.Property(p => p.GatewayTransactionId)
            .HasMaxLength(100);

        builder.Property(p => p.GatewayOrderId)
            .HasMaxLength(100);

        builder.Property(p => p.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.FailureReason)
            .HasMaxLength(500);

        builder.HasIndex(p => p.PaymentNumber).IsUnique();
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.DeliveryId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CreatedAt);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.Delivery)
            .WithMany()
            .HasForeignKey(p => p.DeliveryId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("Settlements");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SettlementNumber)
            .HasMaxLength(25)
            .IsRequired();

        builder.Property(s => s.BeneficiaryType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.GrossAmount)
            .HasPrecision(18, 2);

        builder.Property(s => s.TdsAmount)
            .HasPrecision(18, 2);

        builder.Property(s => s.NetAmount)
            .HasPrecision(18, 2);

        builder.Property(s => s.BankAccountNumber)
            .HasMaxLength(30);

        builder.Property(s => s.BankIfscCode)
            .HasMaxLength(15);

        builder.Property(s => s.UpiId)
            .HasMaxLength(100);

        builder.Property(s => s.PayoutMethod)
            .HasMaxLength(20);

        builder.Property(s => s.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.PayoutReference)
            .HasMaxLength(100);

        builder.Property(s => s.FailureReason)
            .HasMaxLength(500);

        builder.HasIndex(s => s.SettlementNumber).IsUnique();
        builder.HasIndex(s => s.BeneficiaryId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.SettlementDate);

        builder.HasOne(s => s.Beneficiary)
            .WithMany()
            .HasForeignKey(s => s.BeneficiaryId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class SettlementItemConfiguration : IEntityTypeConfiguration<SettlementItem>
{
    public void Configure(EntityTypeBuilder<SettlementItem> builder)
    {
        builder.ToTable("SettlementItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.EarningAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.CommissionAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.NetAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(i => i.SettlementId);
        builder.HasIndex(i => i.DeliveryId);

        builder.HasOne(i => i.Settlement)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.SettlementId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(i => i.Delivery)
            .WithMany()
            .HasForeignKey(i => i.DeliveryId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class CommissionRecordConfiguration : IEntityTypeConfiguration<CommissionRecord>
{
    public void Configure(EntityTypeBuilder<CommissionRecord> builder)
    {
        builder.ToTable("CommissionRecords");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.DeliveryAmount)
            .HasPrecision(18, 2);

        builder.Property(c => c.DPEarning)
            .HasPrecision(18, 2);

        builder.Property(c => c.DPCMCommission)
            .HasPrecision(18, 2);

        builder.Property(c => c.PlatformFee)
            .HasPrecision(18, 2);

        builder.Property(c => c.Status)
            .HasMaxLength(20);

        builder.HasIndex(c => c.DeliveryId).IsUnique();
        builder.HasIndex(c => c.DPId);
        builder.HasIndex(c => c.Status);

        builder.HasOne(c => c.Delivery)
            .WithMany()
            .HasForeignKey(c => c.DeliveryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.DP)
            .WithMany()
            .HasForeignKey(c => c.DPId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.DPCM)
            .WithMany()
            .HasForeignKey(c => c.DPCMId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
