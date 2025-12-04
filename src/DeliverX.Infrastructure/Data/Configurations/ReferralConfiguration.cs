using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliverX.Domain.Entities;

namespace DeliverX.Infrastructure.Data.Configurations;

public class ReferralCodeConfiguration : IEntityTypeConfiguration<ReferralCode>
{
    public void Configure(EntityTypeBuilder<ReferralCode> builder)
    {
        builder.ToTable("ReferralCodes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.ReferrerReward)
            .HasPrecision(18, 2);

        builder.Property(r => r.RefereeReward)
            .HasPrecision(18, 2);

        builder.Property(r => r.TotalEarnings)
            .HasPrecision(18, 2);

        builder.HasIndex(r => r.Code).IsUnique();
        builder.HasIndex(r => r.UserId);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.ToTable("Referrals");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReferralCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.ReferrerReward)
            .HasPrecision(18, 2);

        builder.Property(r => r.RefereeReward)
            .HasPrecision(18, 2);

        builder.HasIndex(r => r.ReferrerId);
        builder.HasIndex(r => r.RefereeId);
        builder.HasIndex(r => r.ReferralCode);

        builder.HasOne(r => r.Referrer)
            .WithMany()
            .HasForeignKey(r => r.ReferrerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Referee)
            .WithMany()
            .HasForeignKey(r => r.RefereeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.ToTable("Donations");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DonationNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(d => d.CharityName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Amount)
            .HasPrecision(18, 2);

        builder.Property(d => d.Source)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(d => d.Message)
            .HasMaxLength(500);

        builder.HasIndex(d => d.DonationNumber).IsUnique();
        builder.HasIndex(d => d.DonorId);
        builder.HasIndex(d => d.CharityId);

        builder.HasOne(d => d.Donor)
            .WithMany()
            .HasForeignKey(d => d.DonorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Charity)
            .WithMany()
            .HasForeignKey(d => d.CharityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.Delivery)
            .WithMany()
            .HasForeignKey(d => d.DeliveryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class CharityConfiguration : IEntityTypeConfiguration<Charity>
{
    public void Configure(EntityTypeBuilder<Charity> builder)
    {
        builder.ToTable("Charities");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.LogoUrl)
            .HasMaxLength(500);

        builder.Property(c => c.WebsiteUrl)
            .HasMaxLength(500);

        builder.Property(c => c.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.TotalReceived)
            .HasPrecision(18, 2);

        builder.HasIndex(c => c.RegistrationNumber).IsUnique();
        builder.HasIndex(c => c.IsActive);
    }
}

public class DonationPreferenceConfiguration : IEntityTypeConfiguration<DonationPreference>
{
    public void Configure(EntityTypeBuilder<DonationPreference> builder)
    {
        builder.ToTable("DonationPreferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.MonthlyLimit)
            .HasPrecision(18, 2);

        builder.Property(p => p.CurrentMonthTotal)
            .HasPrecision(18, 2);

        builder.HasIndex(p => p.UserId).IsUnique();

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.PreferredCharity)
            .WithMany()
            .HasForeignKey(p => p.PreferredCharityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
