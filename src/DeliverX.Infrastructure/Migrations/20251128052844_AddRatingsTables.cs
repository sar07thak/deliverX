using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliverX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BehaviorIndexes",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AverageRating = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: false),
                    TotalRatings = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletionRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    PunctualityRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    ComplaintFreeRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    BehaviorScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BehaviorIndexes", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_BehaviorIndexes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RaterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RaterType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TargetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsAnonymous = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ratings_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ratings_Users_RaterId",
                        column: x => x.RaterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ratings_Users_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_CreatedAt",
                table: "Ratings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_DeliveryId_RaterId_TargetId",
                table: "Ratings",
                columns: new[] { "DeliveryId", "RaterId", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_RaterId",
                table: "Ratings",
                column: "RaterId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_TargetId",
                table: "Ratings",
                column: "TargetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BehaviorIndexes");

            migrationBuilder.DropTable(
                name: "Ratings");
        }
    }
}
