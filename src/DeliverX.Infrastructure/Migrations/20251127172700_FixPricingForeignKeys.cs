using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliverX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPricingForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryPricings_DeliveryPartnerProfiles_DPId",
                table: "DeliveryPricings");

            migrationBuilder.DropForeignKey(
                name: "FK_DPPricingConfigs_DeliveryPartnerProfiles_DPId",
                table: "DPPricingConfigs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DeliveryPartnerProfiles_UserId",
                table: "DeliveryPartnerProfiles");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryPricings_Users_DPId",
                table: "DeliveryPricings",
                column: "DPId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DPPricingConfigs_Users_DPId",
                table: "DPPricingConfigs",
                column: "DPId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryPricings_Users_DPId",
                table: "DeliveryPricings");

            migrationBuilder.DropForeignKey(
                name: "FK_DPPricingConfigs_Users_DPId",
                table: "DPPricingConfigs");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_DeliveryPartnerProfiles_UserId",
                table: "DeliveryPartnerProfiles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryPricings_DeliveryPartnerProfiles_DPId",
                table: "DeliveryPricings",
                column: "DPId",
                principalTable: "DeliveryPartnerProfiles",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DPPricingConfigs_DeliveryPartnerProfiles_DPId",
                table: "DPPricingConfigs",
                column: "DPId",
                principalTable: "DeliveryPartnerProfiles",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
