using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyReplacements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarrantyReplacements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarrantyClaimItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplacementVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    StockMovementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyReplacements", x => x.Id);
                    table.CheckConstraint("CK_WarrantyReplacements_PositiveQuantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_WarrantyReplacements_InventoryBatches_InventoryBatchId",
                        column: x => x.InventoryBatchId,
                        principalTable: "InventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyReplacements_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyReplacements_ProductVariants_ReplacementVariantId",
                        column: x => x.ReplacementVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyReplacements_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyReplacements_WarrantyClaimItems_WarrantyClaimItemId",
                        column: x => x.WarrantyClaimItemId,
                        principalTable: "WarrantyClaimItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyReplacements_InventoryBatchId",
                table: "WarrantyReplacements",
                column: "InventoryBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyReplacements_LocationId",
                table: "WarrantyReplacements",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyReplacements_ReplacementVariantId_LocationId",
                table: "WarrantyReplacements",
                columns: new[] { "ReplacementVariantId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyReplacements_StockMovementId",
                table: "WarrantyReplacements",
                column: "StockMovementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyReplacements_WarrantyClaimItemId",
                table: "WarrantyReplacements",
                column: "WarrantyClaimItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarrantyReplacements");
        }
    }
}
