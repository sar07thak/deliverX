using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliverX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKYCTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AadhaarVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AadhaarHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AadhaarReferenceId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    NameAsPerAadhaar = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    DOB = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    AddressEncrypted = table.Column<string>(type: "TEXT", nullable: true),
                    VerificationMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AadhaarVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AadhaarVerifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountNumberEncrypted = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    AccountNumberHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IFSCCode = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    AccountHolderName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    BranchName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    VerificationMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TransactionId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    NameMatchScore = table.Column<int>(type: "INTEGER", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankVerifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessConsumerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ContactPersonName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    GSTIN = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    PAN = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    BusinessCategory = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BusinessAddress = table.Column<string>(type: "TEXT", nullable: true),
                    BankAccountEncrypted = table.Column<string>(type: "TEXT", nullable: true),
                    SubscriptionPlanId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ActivatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessConsumerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessConsumerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DPCManagers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganizationName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ContactPersonName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    PAN = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    RegistrationCertificateUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ServiceRegions = table.Column<string>(type: "TEXT", nullable: true),
                    CommissionType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CommissionValue = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    BankAccountEncrypted = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ActivatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DPCManagers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DPCManagers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KYCRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VerificationType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "PENDING"),
                    Method = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RequestData = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseData = table.Column<string>(type: "TEXT", nullable: true),
                    DocumentUrls = table.Column<string>(type: "TEXT", nullable: true),
                    VerifiedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    InitiatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KYCRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KYCRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KYCRequests_Users_VerifiedBy",
                        column: x => x.VerifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PANVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PAN = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    NameAsPerPAN = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    DOB = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PANStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NameMatchScore = table.Column<int>(type: "INTEGER", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PANVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PANVerifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoliceVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VerificationAgency = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    AddressForVerification = table.Column<string>(type: "TEXT", nullable: true),
                    RequestDocumentUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ClearanceDocumentUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "PENDING"),
                    InitiatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliceVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoliceVerifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleLicenseVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LicenseNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    LicenseDocumentUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LicenseValidUpto = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VehicleNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    VehicleRCDocumentUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    VehicleType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    VehicleOwnerName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleLicenseVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleLicenseVerifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryPartnerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DPCMId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ProfilePhotoUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DOB = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    VehicleType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Languages = table.Column<string>(type: "TEXT", nullable: true),
                    Availability = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ServiceAreaCenterLat = table.Column<decimal>(type: "TEXT", precision: 10, scale: 8, nullable: true),
                    ServiceAreaCenterLng = table.Column<decimal>(type: "TEXT", precision: 11, scale: 8, nullable: true),
                    ServiceAreaRadiusKm = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    PerKmRate = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    PerKgRate = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    MinCharge = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    MaxDistanceKm = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ActivatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryPartnerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryPartnerProfiles_DPCManagers_DPCMId",
                        column: x => x.DPCMId,
                        principalTable: "DPCManagers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeliveryPartnerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AadhaarVerifications_AadhaarHash",
                table: "AadhaarVerifications",
                column: "AadhaarHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AadhaarVerifications_UserId",
                table: "AadhaarVerifications",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankVerifications_AccountNumberHash",
                table: "BankVerifications",
                column: "AccountNumberHash");

            migrationBuilder.CreateIndex(
                name: "IX_BankVerifications_UserId",
                table: "BankVerifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessConsumerProfiles_UserId",
                table: "BusinessConsumerProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPartnerProfiles_DPCMId",
                table: "DeliveryPartnerProfiles",
                column: "DPCMId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPartnerProfiles_IsActive",
                table: "DeliveryPartnerProfiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPartnerProfiles_UserId",
                table: "DeliveryPartnerProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DPCManagers_UserId",
                table: "DPCManagers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KYCRequests_Status",
                table: "KYCRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_KYCRequests_UserId",
                table: "KYCRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_KYCRequests_VerificationType_Status",
                table: "KYCRequests",
                columns: new[] { "VerificationType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_KYCRequests_VerifiedBy",
                table: "KYCRequests",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PANVerifications_PAN",
                table: "PANVerifications",
                column: "PAN");

            migrationBuilder.CreateIndex(
                name: "IX_PANVerifications_UserId",
                table: "PANVerifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PoliceVerifications_Status",
                table: "PoliceVerifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PoliceVerifications_UserId",
                table: "PoliceVerifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLicenseVerifications_UserId",
                table: "VehicleLicenseVerifications",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AadhaarVerifications");

            migrationBuilder.DropTable(
                name: "BankVerifications");

            migrationBuilder.DropTable(
                name: "BusinessConsumerProfiles");

            migrationBuilder.DropTable(
                name: "DeliveryPartnerProfiles");

            migrationBuilder.DropTable(
                name: "KYCRequests");

            migrationBuilder.DropTable(
                name: "PANVerifications");

            migrationBuilder.DropTable(
                name: "PoliceVerifications");

            migrationBuilder.DropTable(
                name: "VehicleLicenseVerifications");

            migrationBuilder.DropTable(
                name: "DPCManagers");
        }
    }
}
