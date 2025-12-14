using Microsoft.EntityFrameworkCore;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<OTPVerification> OTPVerifications => Set<OTPVerification>();
    public DbSet<AuthAuditLog> AuthAuditLogs => Set<AuthAuditLog>();

    // KYC & Registration
    public DbSet<KYCRequest> KYCRequests => Set<KYCRequest>();
    public DbSet<AadhaarVerification> AadhaarVerifications => Set<AadhaarVerification>();
    public DbSet<PANVerification> PANVerifications => Set<PANVerification>();
    public DbSet<BankVerification> BankVerifications => Set<BankVerification>();
    public DbSet<PoliceVerification> PoliceVerifications => Set<PoliceVerification>();
    public DbSet<VehicleLicenseVerification> VehicleLicenseVerifications => Set<VehicleLicenseVerification>();
    public DbSet<DeliveryPartnerProfile> DeliveryPartnerProfiles => Set<DeliveryPartnerProfile>();
    public DbSet<DPCManager> DPCManagers => Set<DPCManager>();
    public DbSet<BusinessConsumerProfile> BusinessConsumerProfiles => Set<BusinessConsumerProfile>();

    // Service Area & Geofencing
    public DbSet<ServiceArea> ServiceAreas => Set<ServiceArea>();

    // Pricing & Commission
    public DbSet<DPPricingConfig> DPPricingConfigs => Set<DPPricingConfig>();
    public DbSet<DPCMCommissionConfig> DPCMCommissionConfigs => Set<DPCMCommissionConfig>();
    public DbSet<PlatformFeeConfig> PlatformFeeConfigs => Set<PlatformFeeConfig>();
    public DbSet<DeliveryPricing> DeliveryPricings => Set<DeliveryPricing>();

    // Delivery & Matching
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryEvent> DeliveryEvents => Set<DeliveryEvent>();
    public DbSet<DeliveryMatchingHistory> DeliveryMatchingHistories => Set<DeliveryMatchingHistory>();
    public DbSet<DPAvailability> DPAvailabilities => Set<DPAvailability>();

    // POD (Proof of Delivery)
    public DbSet<ProofOfDelivery> ProofOfDeliveries => Set<ProofOfDelivery>();

    // Ratings & Behavior Index
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<BehaviorIndex> BehaviorIndexes => Set<BehaviorIndex>();

    // Complaints & Inspector
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintEvidence> ComplaintEvidences => Set<ComplaintEvidence>();
    public DbSet<ComplaintComment> ComplaintComments => Set<ComplaintComment>();
    public DbSet<Inspector> Inspectors => Set<Inspector>();
    public DbSet<ComplaintSLAConfig> ComplaintSLAConfigs => Set<ComplaintSLAConfig>();
    public DbSet<FieldVisit> FieldVisits => Set<FieldVisit>();
    public DbSet<FieldVisitEvidence> FieldVisitEvidences => Set<FieldVisitEvidence>();
    public DbSet<InvestigationReport> InvestigationReports => Set<InvestigationReport>();
    public DbSet<SLABreach> SLABreaches => Set<SLABreach>();

    // Wallet, Payments & Settlements
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Settlement> Settlements => Set<Settlement>();
    public DbSet<SettlementItem> SettlementItems => Set<SettlementItem>();
    public DbSet<CommissionRecord> CommissionRecords => Set<CommissionRecord>();

    // Subscriptions & Billing
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<SubscriptionInvoice> SubscriptionInvoices => Set<SubscriptionInvoice>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<PromoCodeUsage> PromoCodeUsages => Set<PromoCodeUsage>();

    // Referrals & Donations
    public DbSet<ReferralCode> ReferralCodes => Set<ReferralCode>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<Charity> Charities => Set<Charity>();
    public DbSet<DonationPreference> DonationPreferences => Set<DonationPreference>();

    // Admin Dashboard & System Config
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    // Masters (Pincode, State, District)
    public DbSet<PincodeMaster> PincodeMasters => Set<PincodeMaster>();
    public DbSet<StateMaster> StateMasters => Set<StateMaster>();
    public DbSet<DistrictMaster> DistrictMasters => Set<DistrictMaster>();

    // Pincode-DPCM Mapping
    public DbSet<PincodeDPCMMapping> PincodeDPCMMappings => Set<PincodeDPCMMapping>();

    // BC Pickup Locations
    public DbSet<BCPickupLocation> BCPickupLocations => Set<BCPickupLocation>();

    // Saved Addresses (Group 3 Feature)
    public DbSet<SavedAddress> SavedAddresses => Set<SavedAddress>();

    // Bidding Platform (Group 4 Feature)
    public DbSet<DeliveryBid> DeliveryBids => Set<DeliveryBid>();
    public DbSet<BiddingConfig> BiddingConfigs => Set<BiddingConfig>();

    // Courier Partners (for >15km deliveries)
    public DbSet<CourierPartner> CourierPartners => Set<CourierPartner>();
    public DbSet<CourierShipment> CourierShipments => Set<CourierShipment>();
    public DbSet<CourierRateQuote> CourierRateQuotes => Set<CourierRateQuote>();

    // Invoicing (for Business Consumers)
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    // BC API Integration
    public DbSet<BCApiCredential> BCApiCredentials => Set<BCApiCredential>();
    public DbSet<BCOAuthToken> BCOAuthTokens => Set<BCOAuthToken>();
    public DbSet<ApiKeyUsageLog> ApiKeyUsageLogs => Set<ApiKeyUsageLog>();
    public DbSet<BCWebhook> BCWebhooks => Set<BCWebhook>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();
    public DbSet<ApiRateLimitEntry> ApiRateLimitEntries => Set<ApiRateLimitEntry>();

    // Pool Routes & Fleet Management
    public DbSet<PoolRoute> PoolRoutes => Set<PoolRoute>();
    public DbSet<PoolRouteStop> PoolRouteStops => Set<PoolRouteStop>();
    public DbSet<PoolRouteTrip> PoolRouteTrips => Set<PoolRouteTrip>();
    public DbSet<PoolTripDelivery> PoolTripDeliveries => Set<PoolTripDelivery>();
    public DbSet<FleetVehicle> FleetVehicles => Set<FleetVehicle>();
    public DbSet<DPLocationHistory> DPLocationHistories => Set<DPLocationHistory>();
    public DbSet<RouteOptimizationRequest> RouteOptimizationRequests => Set<RouteOptimizationRequest>();

    // News & Notifications
    public DbSet<NewsAnnouncement> NewsAnnouncements => Set<NewsAnnouncement>();
    public DbSet<NewsReadStatus> NewsReadStatuses => Set<NewsReadStatus>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<PushDeviceRegistration> PushDeviceRegistrations => Set<PushDeviceRegistration>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationCampaign> NotificationCampaigns => Set<NotificationCampaign>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
