using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxableAmount).HasPrecision(18, 2);
        builder.Property(x => x.CGSTAmount).HasPrecision(18, 2);
        builder.Property(x => x.SGSTAmount).HasPrecision(18, 2);
        builder.Property(x => x.IGSTAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalTax).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.CGSTRate).HasPrecision(5, 2);
        builder.Property(x => x.SGSTRate).HasPrecision(5, 2);
        builder.Property(x => x.IGSTRate).HasPrecision(5, 2);

        builder.Property(x => x.BillingName).HasMaxLength(200);
        builder.Property(x => x.BillingAddress).HasMaxLength(1000);
        builder.Property(x => x.BillingGSTIN).HasMaxLength(20);
        builder.Property(x => x.BillingPAN).HasMaxLength(20);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.PdfUrl).HasMaxLength(500);
        builder.Property(x => x.PdfStoragePath).HasMaxLength(500);

        // Relationships
        builder.HasOne(x => x.BusinessConsumer)
            .WithMany()
            .HasForeignKey(x => x.BusinessConsumerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Payment)
            .WithMany()
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.HasIndex(x => x.BusinessConsumerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
    }
}

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("InvoiceItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.HSNCode)
            .HasMaxLength(20);

        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.DiscountPercent).HasPrecision(5, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxableAmount).HasPrecision(18, 2);
        builder.Property(x => x.CGSTAmount).HasPrecision(18, 2);
        builder.Property(x => x.SGSTAmount).HasPrecision(18, 2);
        builder.Property(x => x.IGSTAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);

        // Relationships
        builder.HasOne(x => x.Invoice)
            .WithMany(i => i.Items)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Delivery)
            .WithMany()
            .HasForeignKey(x => x.DeliveryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.InvoiceId);
    }
}
