using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class CourierPartnerConfiguration : IEntityTypeConfiguration<CourierPartner>
{
    public void Configure(EntityTypeBuilder<CourierPartner> builder)
    {
        builder.ToTable("CourierPartners");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.LogoUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ApiBaseUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ApiKey)
            .HasMaxLength(500);

        builder.Property(x => x.ApiSecret)
            .HasMaxLength(500);

        builder.Property(x => x.AccountId)
            .HasMaxLength(100);

        builder.Property(x => x.PlatformMarginPercent)
            .HasPrecision(5, 2);

        builder.Property(x => x.MaxWeightKg)
            .HasPrecision(10, 2);

        builder.Property(x => x.MaxValueAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.MinChargeAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}

public class CourierShipmentConfiguration : IEntityTypeConfiguration<CourierShipment>
{
    public void Configure(EntityTypeBuilder<CourierShipment> builder)
    {
        builder.ToTable("CourierShipments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AWBNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.OrderId)
            .HasMaxLength(50);

        builder.Property(x => x.CourierOrderId)
            .HasMaxLength(100);

        builder.Property(x => x.ServiceType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Dimensions)
            .HasMaxLength(200);

        builder.Property(x => x.WeightKg)
            .HasPrecision(10, 2);

        builder.Property(x => x.CourierCharge)
            .HasPrecision(18, 2);

        builder.Property(x => x.PlatformCharge)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalCharge)
            .HasPrecision(18, 2);

        builder.Property(x => x.CODAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.CourierStatus)
            .HasMaxLength(50);

        builder.Property(x => x.StatusReason)
            .HasMaxLength(500);

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(500);

        builder.Property(x => x.DeliveryProofUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ReceiverName)
            .HasMaxLength(200);

        // Relationships
        builder.HasOne(x => x.Delivery)
            .WithMany()
            .HasForeignKey(x => x.DeliveryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.CourierPartner)
            .WithMany(p => p.Shipments)
            .HasForeignKey(x => x.CourierPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.AWBNumber).IsUnique();
        builder.HasIndex(x => x.DeliveryId);
        builder.HasIndex(x => x.CourierPartnerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
    }
}

public class CourierRateQuoteConfiguration : IEntityTypeConfiguration<CourierRateQuote>
{
    public void Configure(EntityTypeBuilder<CourierRateQuote> builder)
    {
        builder.ToTable("CourierRateQuotes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.BaseRate)
            .HasPrecision(18, 2);

        builder.Property(x => x.FuelSurcharge)
            .HasPrecision(18, 2);

        builder.Property(x => x.CODCharge)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalCourierCharge)
            .HasPrecision(18, 2);

        builder.Property(x => x.PlatformMargin)
            .HasPrecision(18, 2);

        builder.Property(x => x.FinalRate)
            .HasPrecision(18, 2);

        // Relationships
        builder.HasOne(x => x.Delivery)
            .WithMany()
            .HasForeignKey(x => x.DeliveryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.CourierPartner)
            .WithMany()
            .HasForeignKey(x => x.CourierPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.DeliveryId);
        builder.HasIndex(x => x.CourierPartnerId);
        builder.HasIndex(x => x.QuotedAt);
    }
}
