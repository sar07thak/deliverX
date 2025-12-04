using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliverX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceAreaTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceAreas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserRole = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "DP"),
                    AreaType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "CIRCLE"),
                    CenterLat = table.Column<decimal>(type: "TEXT", precision: 10, scale: 8, nullable: false),
                    CenterLng = table.Column<decimal>(type: "TEXT", precision: 11, scale: 8, nullable: false),
                    RadiusKm = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    AreaName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    AllowDropOutsideArea = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceAreas_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAreas_CenterLat_CenterLng",
                table: "ServiceAreas",
                columns: new[] { "CenterLat", "CenterLng" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAreas_IsActive",
                table: "ServiceAreas",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAreas_UserId",
                table: "ServiceAreas",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAreas_UserId_IsActive",
                table: "ServiceAreas",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceAreas");
        }
    }
}
