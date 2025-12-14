using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryDost.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewFeaturesV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiRateLimitEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiCredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WindowKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiRateLimitEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BCApiCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessConsumerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiKeyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApiKeyHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RateLimitPerMinute = table.Column<int>(type: "int", nullable: false),
                    RateLimitPerDay = table.Column<int>(type: "int", nullable: false),
                    AllowedIps = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BCApiCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BCApiCredentials_BusinessConsumerProfiles_BusinessConsumerId",
                        column: x => x.BusinessConsumerId,
                        principalTable: "BusinessConsumerProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BCWebhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessConsumerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebhookUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Events = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Secret = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    LastTriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSuccessAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BCWebhooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BCWebhooks_BusinessConsumerProfiles_BusinessConsumerId",
                        column: x => x.BusinessConsumerId,
                        principalTable: "BusinessConsumerProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourierPartners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApiBaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApiKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApiSecret = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccountId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SupportsExpress = table.Column<bool>(type: "bit", nullable: false),
                    SupportsStandard = table.Column<bool>(type: "bit", nullable: false),
                    SupportsCOD = table.Column<bool>(type: "bit", nullable: false),
                    SupportsReverse = table.Column<bool>(type: "bit", nullable: false),
                    MaxWeightKg = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    MaxValueAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PlatformMarginPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MinChargeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierPartners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DPLocationHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DPId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    Speed = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Heading = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Accuracy = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CurrentDeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DPLocationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DPLocationHistories_Users_DPId",
                        column: x => x.DPId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FieldVisits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldVisits_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FieldVisits_Inspectors_InspectorId",
                        column: x => x.InspectorId,
                        principalTable: "Inspectors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FleetVehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VehicleType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Make = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Year = table.Column<int>(type: "int", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    MaxWeightKg = table.Column<int>(type: "int", nullable: false),
                    MaxVolumeCubicFt = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    InsuranceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InsuranceExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PermitNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PermitExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FitnessNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FitnessExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetVehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FleetVehicles_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InvestigationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Findings = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Verdict = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VerdictReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RecommendedAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompensationAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PenaltyType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PenaltyAppliedToId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestigationReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestigationReports_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvestigationReports_Inspectors_InspectorId",
                        column: x => x.InspectorId,
                        principalTable: "Inspectors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvestigationReports_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BusinessConsumerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CGSTAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SGSTAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IGSTAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CGSTRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SGSTRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IGSTRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsInterState = table.Column<bool>(type: "bit", nullable: false),
                    BillingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BillingAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BillingGSTIN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BillingPAN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PdfUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PdfStoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invoices_Users_BusinessConsumerId",
                        column: x => x.BusinessConsumerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NewsAnnouncements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TargetAudience = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    PublishAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsAnnouncements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsAnnouncements_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NotificationCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TargetAudience = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TargetCriteria = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalRecipients = table.Column<int>(type: "int", nullable: false),
                    SentCount = table.Column<int>(type: "int", nullable: false),
                    DeliveredCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    OpenedCount = table.Column<int>(type: "int", nullable: false),
                    ClickedCount = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationCampaigns_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnablePushNotifications = table.Column<bool>(type: "bit", nullable: false),
                    EnableSmsNotifications = table.Column<bool>(type: "bit", nullable: false),
                    EnableEmailNotifications = table.Column<bool>(type: "bit", nullable: false),
                    DeliveryStatusUpdates = table.Column<bool>(type: "bit", nullable: false),
                    PaymentNotifications = table.Column<bool>(type: "bit", nullable: false),
                    PromotionalMessages = table.Column<bool>(type: "bit", nullable: false),
                    NewsAndAnnouncements = table.Column<bool>(type: "bit", nullable: false),
                    RatingReminders = table.Column<bool>(type: "bit", nullable: false),
                    ComplaintUpdates = table.Column<bool>(type: "bit", nullable: false),
                    QuietHoursStart = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    QuietHoursEnd = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TitleTemplate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BodyTemplate = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DefaultImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ActionUrlTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoolRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartPincode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EndPincode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StartLat = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    StartLng = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    EndLat = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    EndLng = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxDeliveries = table.Column<int>(type: "int", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PricePerKm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ScheduleType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScheduleDays = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DepartureTimes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PushDeviceRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeviceModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AppVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushDeviceRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushDeviceRegistrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RouteOptimizationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartLat = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    StartLng = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    DeliveryIds = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    OptimizedOrder = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TotalDistanceKm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    TotalDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteOptimizationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteOptimizationRequests_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SLABreaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BreachType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpectedHours = table.Column<int>(type: "int", nullable: false),
                    ActualHours = table.Column<int>(type: "int", nullable: false),
                    BreachedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsEscalated = table.Column<bool>(type: "bit", nullable: false),
                    EscalatedToId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EscalatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SLABreaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SLABreaches_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SLABreaches_Users_EscalatedToId",
                        column: x => x.EscalatedToId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSent = table.Column<bool>(type: "bit", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SendError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApiKeyUsageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiCredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: false),
                    RequestIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeyUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeyUsageLogs_BCApiCredentials_ApiCredentialId",
                        column: x => x.ApiCredentialId,
                        principalTable: "BCApiCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BCOAuthTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiCredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessTokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IssuedToIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BCOAuthTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BCOAuthTokens_BCApiCredentials_ApiCredentialId",
                        column: x => x.ApiCredentialId,
                        principalTable: "BCApiCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveryLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebhookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseBody = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveryLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookDeliveryLogs_BCWebhooks_WebhookId",
                        column: x => x.WebhookId,
                        principalTable: "BCWebhooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourierRateQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourierPartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BaseRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FuelSurcharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CODCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCourierCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PlatformMargin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedDays = table.Column<int>(type: "int", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuotedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierRateQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierRateQuotes_CourierPartners_CourierPartnerId",
                        column: x => x.CourierPartnerId,
                        principalTable: "CourierPartners",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourierRateQuotes_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourierShipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourierPartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AWBNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CourierOrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Dimensions = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CourierCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PlatformCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsCOD = table.Column<bool>(type: "bit", nullable: false),
                    CODAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CourierStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StatusReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PickupScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PickedUpAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryProofUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReceiverName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastTrackingResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastTrackedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierShipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierShipments_CourierPartners_CourierPartnerId",
                        column: x => x.CourierPartnerId,
                        principalTable: "CourierPartners",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourierShipments_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FieldVisitEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldVisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldVisitEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldVisitEvidences_FieldVisits_FieldVisitId",
                        column: x => x.FieldVisitId,
                        principalTable: "FieldVisits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HSNCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CGSTAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SGSTAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IGSTAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewsReadStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsReadStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsReadStatuses_NewsAnnouncements_NewsId",
                        column: x => x.NewsId,
                        principalTable: "NewsAnnouncements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NewsReadStatuses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PoolRouteStops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StopOrder = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Pincode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    EstimatedArrivalMinutes = table.Column<int>(type: "int", nullable: false),
                    IsPickupPoint = table.Column<bool>(type: "bit", nullable: false),
                    IsDropPoint = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolRouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoolRouteStops_PoolRoutes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "PoolRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoolRouteTrips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedDPId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TripNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ScheduledDeparture = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualDeparture = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualArrival = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalDeliveries = table.Column<int>(type: "int", nullable: false),
                    CompletedDeliveries = table.Column<int>(type: "int", nullable: false),
                    FailedDeliveries = table.Column<int>(type: "int", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DPEarning = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolRouteTrips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoolRouteTrips_PoolRoutes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "PoolRoutes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PoolRouteTrips_Users_AssignedDPId",
                        column: x => x.AssignedDPId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PoolTripDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PickupStopOrder = table.Column<int>(type: "int", nullable: false),
                    DropStopOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PickedUpAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolTripDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoolTripDeliveries_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PoolTripDeliveries_PoolRouteTrips_TripId",
                        column: x => x.TripId,
                        principalTable: "PoolRouteTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyUsageLogs_ApiCredentialId",
                table: "ApiKeyUsageLogs",
                column: "ApiCredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyUsageLogs_RequestedAt",
                table: "ApiKeyUsageLogs",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRateLimitEntries_ApiCredentialId_WindowKey",
                table: "ApiRateLimitEntries",
                columns: new[] { "ApiCredentialId", "WindowKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiRateLimitEntries_ExpiresAt",
                table: "ApiRateLimitEntries",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_BCApiCredentials_ApiKeyId",
                table: "BCApiCredentials",
                column: "ApiKeyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BCApiCredentials_BusinessConsumerId",
                table: "BCApiCredentials",
                column: "BusinessConsumerId");

            migrationBuilder.CreateIndex(
                name: "IX_BCApiCredentials_BusinessConsumerId_Environment",
                table: "BCApiCredentials",
                columns: new[] { "BusinessConsumerId", "Environment" });

            migrationBuilder.CreateIndex(
                name: "IX_BCOAuthTokens_ApiCredentialId",
                table: "BCOAuthTokens",
                column: "ApiCredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_BCOAuthTokens_ExpiresAt",
                table: "BCOAuthTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_BCWebhooks_BusinessConsumerId",
                table: "BCWebhooks",
                column: "BusinessConsumerId");

            migrationBuilder.CreateIndex(
                name: "IX_BCWebhooks_IsActive",
                table: "BCWebhooks",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CourierPartners_Code",
                table: "CourierPartners",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourierPartners_IsActive",
                table: "CourierPartners",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CourierRateQuotes_CourierPartnerId",
                table: "CourierRateQuotes",
                column: "CourierPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierRateQuotes_DeliveryId",
                table: "CourierRateQuotes",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierRateQuotes_QuotedAt",
                table: "CourierRateQuotes",
                column: "QuotedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CourierShipments_AWBNumber",
                table: "CourierShipments",
                column: "AWBNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourierShipments_CourierPartnerId",
                table: "CourierShipments",
                column: "CourierPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierShipments_CreatedAt",
                table: "CourierShipments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CourierShipments_DeliveryId",
                table: "CourierShipments",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierShipments_Status",
                table: "CourierShipments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DPLocationHistories_DPId",
                table: "DPLocationHistories",
                column: "DPId");

            migrationBuilder.CreateIndex(
                name: "IX_DPLocationHistories_DPId_RecordedAt",
                table: "DPLocationHistories",
                columns: new[] { "DPId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DPLocationHistories_RecordedAt",
                table: "DPLocationHistories",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FieldVisitEvidences_FieldVisitId",
                table: "FieldVisitEvidences",
                column: "FieldVisitId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldVisits_ComplaintId",
                table: "FieldVisits",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldVisits_InspectorId",
                table: "FieldVisits",
                column: "InspectorId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldVisits_ScheduledAt",
                table: "FieldVisits",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_FieldVisits_Status",
                table: "FieldVisits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FleetVehicles_OwnerId",
                table: "FleetVehicles",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetVehicles_VehicleNumber",
                table: "FleetVehicles",
                column: "VehicleNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FleetVehicles_VehicleType",
                table: "FleetVehicles",
                column: "VehicleType");

            migrationBuilder.CreateIndex(
                name: "IX_InvestigationReports_ApprovedById",
                table: "InvestigationReports",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_InvestigationReports_ComplaintId",
                table: "InvestigationReports",
                column: "ComplaintId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvestigationReports_InspectorId",
                table: "InvestigationReports",
                column: "InspectorId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestigationReports_Verdict",
                table: "InvestigationReports",
                column: "Verdict");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_DeliveryId",
                table: "InvoiceItems",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                table: "InvoiceItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BusinessConsumerId",
                table: "Invoices",
                column: "BusinessConsumerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedAt",
                table: "Invoices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PaymentId",
                table: "Invoices",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status",
                table: "Invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NewsAnnouncements_CreatedAt",
                table: "NewsAnnouncements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NewsAnnouncements_CreatedById",
                table: "NewsAnnouncements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_NewsAnnouncements_IsPublished",
                table: "NewsAnnouncements",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_NewsAnnouncements_PublishAt",
                table: "NewsAnnouncements",
                column: "PublishAt");

            migrationBuilder.CreateIndex(
                name: "IX_NewsAnnouncements_TargetAudience",
                table: "NewsAnnouncements",
                column: "TargetAudience");

            migrationBuilder.CreateIndex(
                name: "IX_NewsReadStatuses_NewsId_UserId",
                table: "NewsReadStatuses",
                columns: new[] { "NewsId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsReadStatuses_UserId",
                table: "NewsReadStatuses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationCampaigns_CreatedById",
                table: "NotificationCampaigns",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationCampaigns_ScheduledAt",
                table: "NotificationCampaigns",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationCampaigns_Status",
                table: "NotificationCampaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_TemplateCode_Channel",
                table: "NotificationTemplates",
                columns: new[] { "TemplateCode", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoolRoutes_IsActive",
                table: "PoolRoutes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PoolRoutes_RouteCode",
                table: "PoolRoutes",
                column: "RouteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoolRouteStops_RouteId",
                table: "PoolRouteStops",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_PoolRouteStops_RouteId_StopOrder",
                table: "PoolRouteStops",
                columns: new[] { "RouteId", "StopOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PoolRouteTrips_AssignedDPId",
                table: "PoolRouteTrips",
                column: "AssignedDPId");

            migrationBuilder.CreateIndex(
                name: "IX_PoolRouteTrips_RouteId",
                table: "PoolRouteTrips",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_PoolRouteTrips_ScheduledDeparture",
                table: "PoolRouteTrips",
                column: "ScheduledDeparture");

            migrationBuilder.CreateIndex(
                name: "IX_PoolRouteTrips_Status",
                table: "PoolRouteTrips",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PoolRouteTrips_TripNumber",
                table: "PoolRouteTrips",
                column: "TripNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoolTripDeliveries_DeliveryId",
                table: "PoolTripDeliveries",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_PoolTripDeliveries_TripId",
                table: "PoolTripDeliveries",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_PushDeviceRegistrations_DeviceToken",
                table: "PushDeviceRegistrations",
                column: "DeviceToken");

            migrationBuilder.CreateIndex(
                name: "IX_PushDeviceRegistrations_UserId",
                table: "PushDeviceRegistrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PushDeviceRegistrations_UserId_IsActive",
                table: "PushDeviceRegistrations",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteOptimizationRequests_RequestedAt",
                table: "RouteOptimizationRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RouteOptimizationRequests_RequestedById",
                table: "RouteOptimizationRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_SLABreaches_BreachedAt",
                table: "SLABreaches",
                column: "BreachedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SLABreaches_ComplaintId",
                table: "SLABreaches",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_SLABreaches_EscalatedToId",
                table: "SLABreaches",
                column: "EscalatedToId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_CreatedAt",
                table: "UserNotifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_IsRead",
                table: "UserNotifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId",
                table: "UserNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_IsRead",
                table: "UserNotifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveryLogs_AttemptedAt",
                table: "WebhookDeliveryLogs",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveryLogs_WebhookId",
                table: "WebhookDeliveryLogs",
                column: "WebhookId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeyUsageLogs");

            migrationBuilder.DropTable(
                name: "ApiRateLimitEntries");

            migrationBuilder.DropTable(
                name: "BCOAuthTokens");

            migrationBuilder.DropTable(
                name: "CourierRateQuotes");

            migrationBuilder.DropTable(
                name: "CourierShipments");

            migrationBuilder.DropTable(
                name: "DPLocationHistories");

            migrationBuilder.DropTable(
                name: "FieldVisitEvidences");

            migrationBuilder.DropTable(
                name: "FleetVehicles");

            migrationBuilder.DropTable(
                name: "InvestigationReports");

            migrationBuilder.DropTable(
                name: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "NewsReadStatuses");

            migrationBuilder.DropTable(
                name: "NotificationCampaigns");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.DropTable(
                name: "PoolRouteStops");

            migrationBuilder.DropTable(
                name: "PoolTripDeliveries");

            migrationBuilder.DropTable(
                name: "PushDeviceRegistrations");

            migrationBuilder.DropTable(
                name: "RouteOptimizationRequests");

            migrationBuilder.DropTable(
                name: "SLABreaches");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "WebhookDeliveryLogs");

            migrationBuilder.DropTable(
                name: "BCApiCredentials");

            migrationBuilder.DropTable(
                name: "CourierPartners");

            migrationBuilder.DropTable(
                name: "FieldVisits");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "NewsAnnouncements");

            migrationBuilder.DropTable(
                name: "PoolRouteTrips");

            migrationBuilder.DropTable(
                name: "BCWebhooks");

            migrationBuilder.DropTable(
                name: "PoolRoutes");
        }
    }
}
