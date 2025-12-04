using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliverX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_DeliveryPartnerProfiles_UserId",
                table: "DeliveryPartnerProfiles",
                column: "UserId");

            migrationBuilder.CreateTable(
                name: "DeliveryPricings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DPId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DistanceKm = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    WeightKg = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    PerKmRate = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    PerKgRate = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    MinCharge = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Surcharges = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    GSTPercentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    GSTAmount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    DPEarning = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    DPCMCommission = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    PlatformFee = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "INR"),
                    CalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryPricings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryPricings_DeliveryPartnerProfiles_DPId",
                        column: x => x.DPId,
                        principalTable: "DeliveryPartnerProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DPCMCommissionConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DPCMId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CommissionType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CommissionValue = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    MinCommissionAmount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    MaxCommissionAmount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DPCMCommissionConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DPCMCommissionConfigs_DPCManagers_DPCMId",
                        column: x => x.DPCMId,
                        principalTable: "DPCManagers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DPPricingConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DPId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PerKmRate = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    PerKgRate = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    MinCharge = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    MaxDistanceKm = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false, defaultValue: 20m),
                    AcceptsPriorityDelivery = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    PrioritySurcharge = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    PeakHourSurcharge = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "INR"),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DPPricingConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DPPricingConfigs_DeliveryPartnerProfiles_DPId",
                        column: x => x.DPId,
                        principalTable: "DeliveryPartnerProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlatformFeeConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FeeType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FeeCalculationType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FeeValue = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    ApplicableRoles = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Conditions = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformFeeConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPricings_DeliveryId",
                table: "DeliveryPricings",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPricings_DPId",
                table: "DeliveryPricings",
                column: "DPId");

            migrationBuilder.CreateIndex(
                name: "IX_DPCMCommissionConfigs_DPCMId",
                table: "DPCMCommissionConfigs",
                column: "DPCMId");

            migrationBuilder.CreateIndex(
                name: "IX_DPPricingConfigs_DPId",
                table: "DPPricingConfigs",
                column: "DPId");

            migrationBuilder.CreateIndex(
                name: "IX_DPPricingConfigs_EffectiveFrom_EffectiveTo",
                table: "DPPricingConfigs",
                columns: new[] { "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformFeeConfigs_EffectiveFrom_EffectiveTo",
                table: "PlatformFeeConfigs",
                columns: new[] { "EffectiveFrom", "EffectiveTo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryPricings");

            migrationBuilder.DropTable(
                name: "DPCMCommissionConfigs");

            migrationBuilder.DropTable(
                name: "DPPricingConfigs");

            migrationBuilder.DropTable(
                name: "PlatformFeeConfigs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DeliveryPartnerProfiles_UserId",
                table: "DeliveryPartnerProfiles");
        }
    }
}
