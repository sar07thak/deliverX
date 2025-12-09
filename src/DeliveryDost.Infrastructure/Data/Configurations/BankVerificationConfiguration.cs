using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class BankVerificationConfiguration : IEntityTypeConfiguration<BankVerification>
{
    public void Configure(EntityTypeBuilder<BankVerification> builder)
    {
        builder.ToTable("BankVerifications");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.AccountNumberEncrypted)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(b => b.AccountNumberHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(b => b.IFSCCode)
            .IsRequired()
            .HasMaxLength(11);

        builder.Property(b => b.AccountHolderName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(b => b.BankName)
            .HasMaxLength(255);

        builder.Property(b => b.BranchName)
            .HasMaxLength(255);

        builder.Property(b => b.VerificationMethod)
            .HasMaxLength(50);

        builder.Property(b => b.TransactionId)
            .HasMaxLength(255);

        builder.Property(b => b.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(b => b.UserId);
        builder.HasIndex(b => b.AccountNumberHash);

        // Relationships
        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
