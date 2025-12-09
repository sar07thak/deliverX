using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.PlanType).HasMaxLength(10).IsRequired();
        builder.Property(p => p.BillingCycle).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.DiscountedPrice).HasPrecision(18, 2);
        builder.Property(p => p.PerDeliveryDiscount).HasPrecision(5, 2);
        builder.Property(p => p.Features).HasMaxLength(2000);

        builder.HasIndex(p => new { p.PlanType, p.IsActive });
    }
}

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("UserSubscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status).HasMaxLength(20).IsRequired();
        builder.Property(s => s.PaymentMethod).HasMaxLength(20);
        builder.Property(s => s.CancellationReason).HasMaxLength(500);
        builder.Property(s => s.AmountPaid).HasPrecision(18, 2);

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.EndDate);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class SubscriptionInvoiceConfiguration : IEntityTypeConfiguration<SubscriptionInvoice>
{
    public void Configure(EntityTypeBuilder<SubscriptionInvoice> builder)
    {
        builder.ToTable("SubscriptionInvoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber).HasMaxLength(25).IsRequired();
        builder.Property(i => i.BillingPeriod).HasMaxLength(50);
        builder.Property(i => i.Subtotal).HasPrecision(18, 2);
        builder.Property(i => i.Discount).HasPrecision(18, 2);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.Status).HasMaxLength(20).IsRequired();

        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.UserId);
        builder.HasIndex(i => i.Status);

        builder.HasOne(i => i.Subscription)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.SubscriptionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.ToTable("PromoCodes");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(200);
        builder.Property(p => p.DiscountType).HasMaxLength(20).IsRequired();
        builder.Property(p => p.DiscountValue).HasPrecision(18, 2);
        builder.Property(p => p.MaxDiscountAmount).HasPrecision(18, 2);
        builder.Property(p => p.MinOrderAmount).HasPrecision(18, 2);
        builder.Property(p => p.ApplicableTo).HasMaxLength(20);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.IsActive);
    }
}

public class PromoCodeUsageConfiguration : IEntityTypeConfiguration<PromoCodeUsage>
{
    public void Configure(EntityTypeBuilder<PromoCodeUsage> builder)
    {
        builder.ToTable("PromoCodeUsages");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UsedFor).HasMaxLength(20);
        builder.Property(u => u.DiscountApplied).HasPrecision(18, 2);

        builder.HasIndex(u => new { u.PromoCodeId, u.UserId });

        builder.HasOne(u => u.PromoCode)
            .WithMany()
            .HasForeignKey(u => u.PromoCodeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
