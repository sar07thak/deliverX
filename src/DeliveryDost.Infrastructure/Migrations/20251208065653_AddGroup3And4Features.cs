using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryDost.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroup3And4Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentActiveDeliveries",
                table: "DeliveryPartnerProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DirectionAngleDegrees",
                table: "DeliveryPartnerProfiles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "DeliveryPartnerProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOnlineAt",
                table: "DeliveryPartnerProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxBidRate",
                table: "DeliveryPartnerProfiles",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentDeliveries",
                table: "DeliveryPartnerProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "OneDirectionOnly",
                table: "DeliveryPartnerProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreferredDirection",
                table: "DeliveryPartnerProfiles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceAreaPincodesJson",
                table: "DeliveryPartnerProfiles",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceAreaPolygonJson",
                table: "DeliveryPartnerProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SpecialInstructions",
                table: "Deliveries",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CautionNotes",
                table: "Deliveries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CautionType",
                table: "Deliveries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DistanceSource",
                table: "Deliveries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropAddressName",
                table: "Deliveries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropAlternatePhone",
                table: "Deliveries",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropContactEmail",
                table: "Deliveries",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DropSavedAddressId",
                table: "Deliveries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropWhatsAppNumber",
                table: "Deliveries",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHazardous",
                table: "Deliveries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PickupAddressName",
                table: "Deliveries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupAlternatePhone",
                table: "Deliveries",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupContactEmail",
                table: "Deliveries",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PickupSavedAddressId",
                table: "Deliveries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupWhatsAppNumber",
                table: "Deliveries",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresSpecialHandling",
                table: "Deliveries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RoutePolyline",
                table: "Deliveries",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BiddingConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BidExpiryMinutes = table.Column<int>(type: "int", nullable: false),
                    DeliveryBidWindowMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxBidsPerDelivery = table.Column<int>(type: "int", nullable: false),
                    MaxActiveBidsPerDP = table.Column<int>(type: "int", nullable: false),
                    MinBidPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.5m),
                    MaxBidPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 1.5m),
                    AutoSelectLowestBid = table.Column<bool>(type: "bit", nullable: false),
                    AutoSelectAfterMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiddingConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryBids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DPId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BidAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    BidNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DPLatitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: true),
                    DPLongitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: true),
                    DistanceToPickupKm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    EstimatedPickupMinutes = table.Column<int>(type: "int", nullable: true),
                    EstimatedDeliveryMinutes = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    ExceedsMaxRate = table.Column<bool>(type: "bit", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryBids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryBids_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryBids_Users_DPId",
                        column: x => x.DPId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SavedAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddressName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Pincode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: false),
                    FullAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    AlternatePhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    WhatsAppNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    AddressType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "OTHER"),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsPickupAddress = table.Column<bool>(type: "bit", nullable: false),
                    IsDropAddress = table.Column<bool>(type: "bit", nullable: false),
                    DefaultInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Landmark = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedAddresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_DropSavedAddressId",
                table: "Deliveries",
                column: "DropSavedAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_PickupSavedAddressId",
                table: "Deliveries",
                column: "PickupSavedAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBids_CreatedAt",
                table: "DeliveryBids",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBids_DeliveryId",
                table: "DeliveryBids",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBids_DeliveryId_DPId",
                table: "DeliveryBids",
                columns: new[] { "DeliveryId", "DPId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBids_DeliveryId_Status",
                table: "DeliveryBids",
                columns: new[] { "DeliveryId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBids_DPId",
                table: "DeliveryBids",
                column: "DPId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBids_Status",
                table: "DeliveryBids",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SavedAddresses_Pincode",
                table: "SavedAddresses",
                column: "Pincode");

            migrationBuilder.CreateIndex(
                name: "IX_SavedAddresses_UserId",
                table: "SavedAddresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedAddresses_UserId_AddressType",
                table: "SavedAddresses",
                columns: new[] { "UserId", "AddressType" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedAddresses_UserId_IsDefault",
                table: "SavedAddresses",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_SavedAddresses_DropSavedAddressId",
                table: "Deliveries",
                column: "DropSavedAddressId",
                principalTable: "SavedAddresses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_SavedAddresses_PickupSavedAddressId",
                table: "Deliveries",
                column: "PickupSavedAddressId",
                principalTable: "SavedAddresses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_SavedAddresses_DropSavedAddressId",
                table: "Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_SavedAddresses_PickupSavedAddressId",
                table: "Deliveries");

            migrationBuilder.DropTable(
                name: "BiddingConfigs");

            migrationBuilder.DropTable(
                name: "DeliveryBids");

            migrationBuilder.DropTable(
                name: "SavedAddresses");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_DropSavedAddressId",
                table: "Deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_PickupSavedAddressId",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "CurrentActiveDeliveries",
                table: "DeliveryPartnerProfiles");

            migrationBuilder.DropColumn(
                name: "DirectionAngleDegrees",
                table: "DeliveryPartnerProfiles");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "DeliveryPartnerProfiles");

            migrationBuilder.DropColumn(
                name: "LastOnlineAt",
                table: "DeliveryPartnerProfiles");

            migrationBuilder.DropColumn(
                name: "MaxBidRate",
                table: "DeliveryPartnerProfiles");

            migrationBuilder.DropColumn(
                name: "MaxConcurrentDeliveries",
                table: "DeliveryPartnerProfiles");

            migrationBuilder.DropColumn(
                name: "OneDirectionOnly",
                table: "DeliveryPartnerProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredDirection",
                table: "DeliveryPartnerProfiles");

            migrationBuilder.DropColumn(
                name: "ServiceAreaPincodesJson",
                table: "DeliveryPartnerProfiles");

            migrationBuilder.DropColumn(
                name: "ServiceAreaPolygonJson",
                table: "DeliveryPartnerProfiles");

            migrationBuilder.DropColumn(
                name: "CautionNotes",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "CautionType",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DistanceSource",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DropAddressName",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DropAlternatePhone",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DropContactEmail",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DropSavedAddressId",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DropWhatsAppNumber",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "IsHazardous",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "PickupAddressName",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "PickupAlternatePhone",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "PickupContactEmail",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "PickupSavedAddressId",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "PickupWhatsAppNumber",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "RequiresSpecialHandling",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "RoutePolyline",
                table: "Deliveries");

            migrationBuilder.AlterColumn<string>(
                name: "SpecialInstructions",
                table: "Deliveries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
