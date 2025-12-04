using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliverX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequesterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequesterType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AssignedDPId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PickupLat = table.Column<decimal>(type: "TEXT", precision: 10, scale: 8, nullable: false),
                    PickupLng = table.Column<decimal>(type: "TEXT", precision: 11, scale: 8, nullable: false),
                    PickupAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PickupContactName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    PickupContactPhone = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    PickupInstructions = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DropLat = table.Column<decimal>(type: "TEXT", precision: 10, scale: 8, nullable: false),
                    DropLng = table.Column<decimal>(type: "TEXT", precision: 11, scale: 8, nullable: false),
                    DropAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DropContactName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    DropContactPhone = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    DropInstructions = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    WeightKg = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    PackageType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PackageDimensions = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PackageValue = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    PackageDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "ASAP"),
                    ScheduledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "CREATED"),
                    EstimatedPrice = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    FinalPrice = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    SpecialInstructions = table.Column<string>(type: "TEXT", nullable: true),
                    PreferredDPId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DistanceKm = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    MatchingAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deliveries_Users_AssignedDPId",
                        column: x => x.AssignedDPId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Deliveries_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FromStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ToStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ActorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ActorType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryEvents_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryEvents_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryMatchingHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DPId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MatchingAttempt = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    NotifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResponseType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryMatchingHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryMatchingHistories_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryMatchingHistories_Users_DPId",
                        column: x => x.DPId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DPAvailabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DPId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "OFFLINE"),
                    CurrentDeliveryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LastLocationLat = table.Column<decimal>(type: "TEXT", precision: 10, scale: 8, nullable: true),
                    LastLocationLng = table.Column<decimal>(type: "TEXT", precision: 11, scale: 8, nullable: true),
                    LastLocationUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DPAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DPAvailabilities_Deliveries_CurrentDeliveryId",
                        column: x => x.CurrentDeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DPAvailabilities_Users_DPId",
                        column: x => x.DPId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_AssignedDPId",
                table: "Deliveries",
                column: "AssignedDPId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_CreatedAt",
                table: "Deliveries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_Priority_Status",
                table: "Deliveries",
                columns: new[] { "Priority", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_RequesterId",
                table: "Deliveries",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_Status",
                table: "Deliveries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryEvents_ActorId",
                table: "DeliveryEvents",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryEvents_DeliveryId",
                table: "DeliveryEvents",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryEvents_Timestamp",
                table: "DeliveryEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMatchingHistories_DeliveryId",
                table: "DeliveryMatchingHistories",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMatchingHistories_DPId",
                table: "DeliveryMatchingHistories",
                column: "DPId");

            migrationBuilder.CreateIndex(
                name: "IX_DPAvailabilities_CurrentDeliveryId",
                table: "DPAvailabilities",
                column: "CurrentDeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_DPAvailabilities_DPId",
                table: "DPAvailabilities",
                column: "DPId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DPAvailabilities_Status",
                table: "DPAvailabilities",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryEvents");

            migrationBuilder.DropTable(
                name: "DeliveryMatchingHistories");

            migrationBuilder.DropTable(
                name: "DPAvailabilities");

            migrationBuilder.DropTable(
                name: "Deliveries");
        }
    }
}
