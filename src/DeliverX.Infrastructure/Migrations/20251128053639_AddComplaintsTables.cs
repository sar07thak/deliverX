using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliverX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComplaintsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Complaints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ComplaintNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DeliveryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RaisedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    RaisedByType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    AgainstId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AgainstType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Resolution = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    AssignedToId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Complaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Complaints_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Complaints_Users_AgainstId",
                        column: x => x.AgainstId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Complaints_Users_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Complaints_Users_RaisedById",
                        column: x => x.RaisedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ComplaintSLAConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ResponseTimeHours = table.Column<int>(type: "INTEGER", nullable: false),
                    ResolutionTimeHours = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintSLAConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inspectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InspectorCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Zone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ActiveCases = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalCasesHandled = table.Column<int>(type: "INTEGER", nullable: false),
                    ResolutionRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    AverageResolutionTimeHours = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspectors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComplaintComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsInternal = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintComments_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplaintComments_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ComplaintEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FileUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UploadedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintEvidences_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplaintEvidences_Users_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintComments_AuthorId",
                table: "ComplaintComments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintComments_ComplaintId",
                table: "ComplaintComments",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintEvidences_ComplaintId",
                table: "ComplaintEvidences",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintEvidences_UploadedById",
                table: "ComplaintEvidences",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_AgainstId",
                table: "Complaints",
                column: "AgainstId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_AssignedToId",
                table: "Complaints",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_Category",
                table: "Complaints",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_ComplaintNumber",
                table: "Complaints",
                column: "ComplaintNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_CreatedAt",
                table: "Complaints",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_DeliveryId",
                table: "Complaints",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_RaisedById",
                table: "Complaints",
                column: "RaisedById");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_Status",
                table: "Complaints",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintSLAConfigs_Category_Severity",
                table: "ComplaintSLAConfigs",
                columns: new[] { "Category", "Severity" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inspectors_InspectorCode",
                table: "Inspectors",
                column: "InspectorCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inspectors_UserId",
                table: "Inspectors",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplaintComments");

            migrationBuilder.DropTable(
                name: "ComplaintEvidences");

            migrationBuilder.DropTable(
                name: "ComplaintSLAConfigs");

            migrationBuilder.DropTable(
                name: "Inspectors");

            migrationBuilder.DropTable(
                name: "Complaints");
        }
    }
}
