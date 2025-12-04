using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliverX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPODTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProofOfDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecipientName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    RecipientRelation = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RecipientOTP = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true),
                    OTPVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    OTPSentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OTPVerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PODPhotoUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PackagePhotoUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SignatureUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DeliveredLat = table.Column<decimal>(type: "TEXT", precision: 10, scale: 8, nullable: true),
                    DeliveredLng = table.Column<decimal>(type: "TEXT", precision: 11, scale: 8, nullable: true),
                    DistanceFromDropLocation = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    PickedUpAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InTransitAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DeliveryCondition = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    VerifiedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProofOfDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProofOfDeliveries_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProofOfDeliveries_Users_VerifiedBy",
                        column: x => x.VerifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfDeliveries_DeliveryId",
                table: "ProofOfDeliveries",
                column: "DeliveryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfDeliveries_VerifiedBy",
                table: "ProofOfDeliveries",
                column: "VerifiedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProofOfDeliveries");
        }
    }
}
