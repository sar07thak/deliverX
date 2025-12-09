using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryDost.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPincodeMasterTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DistrictMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    DistrictName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistrictMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PincodeMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Pincode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    StateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StateCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    DistrictName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TalukName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AreaName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OfficeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OfficeType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Delivery = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PincodeMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StateMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    StateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateMasters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DistrictMasters_StateCode",
                table: "DistrictMasters",
                column: "StateCode");

            migrationBuilder.CreateIndex(
                name: "IX_DistrictMasters_StateCode_DistrictName",
                table: "DistrictMasters",
                columns: new[] { "StateCode", "DistrictName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PincodeMasters_DistrictName",
                table: "PincodeMasters",
                column: "DistrictName");

            migrationBuilder.CreateIndex(
                name: "IX_PincodeMasters_Pincode",
                table: "PincodeMasters",
                column: "Pincode");

            migrationBuilder.CreateIndex(
                name: "IX_PincodeMasters_Pincode_IsActive",
                table: "PincodeMasters",
                columns: new[] { "Pincode", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PincodeMasters_StateCode",
                table: "PincodeMasters",
                column: "StateCode");

            migrationBuilder.CreateIndex(
                name: "IX_StateMasters_StateCode",
                table: "StateMasters",
                column: "StateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateMasters_StateName",
                table: "StateMasters",
                column: "StateName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DistrictMasters");

            migrationBuilder.DropTable(
                name: "PincodeMasters");

            migrationBuilder.DropTable(
                name: "StateMasters");
        }
    }
}
