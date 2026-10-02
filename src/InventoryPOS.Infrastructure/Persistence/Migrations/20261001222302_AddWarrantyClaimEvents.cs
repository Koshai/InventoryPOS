using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyClaimEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarrantyClaimEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarrantyClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: true),
                    ToStatus = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyClaimEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyClaimEvents_WarrantyClaims_WarrantyClaimId",
                        column: x => x.WarrantyClaimId,
                        principalTable: "WarrantyClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaimEvents_EventType",
                table: "WarrantyClaimEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaimEvents_UserId",
                table: "WarrantyClaimEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaimEvents_WarrantyClaimId",
                table: "WarrantyClaimEvents",
                column: "WarrantyClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaimEvents_WarrantyClaimId_CreatedAtUtc",
                table: "WarrantyClaimEvents",
                columns: new[] { "WarrantyClaimId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarrantyClaimEvents");
        }
    }
}
