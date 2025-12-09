using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryDost.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroup2Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgreementDocumentUrl",
                table: "DPCManagers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AgreementSignedAt",
                table: "DPCManagers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgreementVersion",
                table: "DPCManagers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinCommissionAmount",
                table: "DPCManagers",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SecurityDeposit",
                table: "DPCManagers",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecurityDepositReceivedAt",
                table: "DPCManagers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityDepositStatus",
                table: "DPCManagers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityDepositTransactionRef",
                table: "DPCManagers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessConstitution",
                table: "BusinessConsumerProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GSTRegistrationType",
                table: "BusinessConsumerProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupLocationsJson",
                table: "BusinessConsumerProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionStartDate",
                table: "BusinessConsumerProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BCPickupLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessConsumerProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Pincode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BCPickupLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BCPickupLocations_BusinessConsumerProfiles_BusinessConsumerProfileId",
                        column: x => x.BusinessConsumerProfileId,
                        principalTable: "BusinessConsumerProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PincodeDPCMMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Pincode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    DPCMId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DistrictName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeactivationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PincodeDPCMMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PincodeDPCMMappings_DPCManagers_DPCMId",
                        column: x => x.DPCMId,
                        principalTable: "DPCManagers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PincodeDPCMMappings_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessConsumerProfiles_SubscriptionPlanId",
                table: "BusinessConsumerProfiles",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_BCPickupLocations_BusinessConsumerProfileId",
                table: "BCPickupLocations",
                column: "BusinessConsumerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_BCPickupLocations_Pincode",
                table: "BCPickupLocations",
                column: "Pincode");

            migrationBuilder.CreateIndex(
                name: "IX_PincodeDPCMMappings_AssignedByUserId",
                table: "PincodeDPCMMappings",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PincodeDPCMMappings_DPCMId",
                table: "PincodeDPCMMappings",
                column: "DPCMId");

            migrationBuilder.CreateIndex(
                name: "IX_PincodeDPCMMappings_Pincode_IsActive",
                table: "PincodeDPCMMappings",
                columns: new[] { "Pincode", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessConsumerProfiles_SubscriptionPlans_SubscriptionPlanId",
                table: "BusinessConsumerProfiles",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessConsumerProfiles_SubscriptionPlans_SubscriptionPlanId",
                table: "BusinessConsumerProfiles");

            migrationBuilder.DropTable(
                name: "BCPickupLocations");

            migrationBuilder.DropTable(
                name: "PincodeDPCMMappings");

            migrationBuilder.DropIndex(
                name: "IX_BusinessConsumerProfiles_SubscriptionPlanId",
                table: "BusinessConsumerProfiles");

            migrationBuilder.DropColumn(
                name: "AgreementDocumentUrl",
                table: "DPCManagers");

            migrationBuilder.DropColumn(
                name: "AgreementSignedAt",
                table: "DPCManagers");

            migrationBuilder.DropColumn(
                name: "AgreementVersion",
                table: "DPCManagers");

            migrationBuilder.DropColumn(
                name: "MinCommissionAmount",
                table: "DPCManagers");

            migrationBuilder.DropColumn(
                name: "SecurityDeposit",
                table: "DPCManagers");

            migrationBuilder.DropColumn(
                name: "SecurityDepositReceivedAt",
                table: "DPCManagers");

            migrationBuilder.DropColumn(
                name: "SecurityDepositStatus",
                table: "DPCManagers");

            migrationBuilder.DropColumn(
                name: "SecurityDepositTransactionRef",
                table: "DPCManagers");

            migrationBuilder.DropColumn(
                name: "BusinessConstitution",
                table: "BusinessConsumerProfiles");

            migrationBuilder.DropColumn(
                name: "GSTRegistrationType",
                table: "BusinessConsumerProfiles");

            migrationBuilder.DropColumn(
                name: "PickupLocationsJson",
                table: "BusinessConsumerProfiles");

            migrationBuilder.DropColumn(
                name: "SubscriptionStartDate",
                table: "BusinessConsumerProfiles");
        }
    }
}
