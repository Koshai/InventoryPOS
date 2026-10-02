using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarrantyClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CustomerComplaint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InspectionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyClaimItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarrantyClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ReportedDefect = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OriginalWarrantyEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionType = table.Column<int>(type: "integer", nullable: false),
                    TechnicianNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyClaimItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyClaimItems_SaleLines_SaleLineId",
                        column: x => x.SaleLineId,
                        principalTable: "SaleLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaimItems_WarrantyClaims_WarrantyClaimId",
                        column: x => x.WarrantyClaimId,
                        principalTable: "WarrantyClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaimItems_SaleLineId",
                table: "WarrantyClaimItems",
                column: "SaleLineId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaimItems_SerialNumber",
                table: "WarrantyClaimItems",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaimItems_WarrantyClaimId",
                table: "WarrantyClaimItems",
                column: "WarrantyClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_ClaimDate",
                table: "WarrantyClaims",
                column: "ClaimDate");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_ClaimNumber",
                table: "WarrantyClaims",
                column: "ClaimNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_CustomerId",
                table: "WarrantyClaims",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_LocationId",
                table: "WarrantyClaims",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_SaleId",
                table: "WarrantyClaims",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_Status",
                table: "WarrantyClaims",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarrantyClaimItems");

            migrationBuilder.DropTable(
                name: "WarrantyClaims");
        }
    }
}
