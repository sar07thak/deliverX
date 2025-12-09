using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data.Configurations;

public class SavedAddressConfiguration : IEntityTypeConfiguration<SavedAddress>
{
    public void Configure(EntityTypeBuilder<SavedAddress> builder)
    {
        builder.ToTable("SavedAddresses");

        builder.HasKey(x => x.Id);

        // Address Name
        builder.Property(x => x.AddressName)
            .HasMaxLength(100)
            .IsRequired();

        // Address Fields
        builder.Property(x => x.AddressLine1)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.AddressLine2)
            .HasMaxLength(255);

        builder.Property(x => x.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.State)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Pincode)
            .HasMaxLength(10)
            .IsRequired();

        // Geolocation
        builder.Property(x => x.Latitude)
            .HasPrecision(10, 8)
            .IsRequired();

        builder.Property(x => x.Longitude)
            .HasPrecision(11, 8)
            .IsRequired();

        // Full Address
        builder.Property(x => x.FullAddress)
            .HasMaxLength(500)
            .IsRequired();

        // Contact Details
        builder.Property(x => x.ContactName)
            .HasMaxLength(100);

        builder.Property(x => x.ContactPhone)
            .HasMaxLength(15);

        builder.Property(x => x.AlternatePhone)
            .HasMaxLength(15);

        builder.Property(x => x.ContactEmail)
            .HasMaxLength(255);

        builder.Property(x => x.WhatsAppNumber)
            .HasMaxLength(15);

        // Address Type
        builder.Property(x => x.AddressType)
            .HasMaxLength(20)
            .HasDefaultValue("OTHER");

        // Special Instructions
        builder.Property(x => x.DefaultInstructions)
            .HasMaxLength(500);

        builder.Property(x => x.Landmark)
            .HasMaxLength(255);

        // Indexes
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.IsDefault });
        builder.HasIndex(x => new { x.UserId, x.AddressType });
        builder.HasIndex(x => x.Pincode);

        // Relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
