using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchExpiryWarrantySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupplierCode",
                table: "Suppliers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryBatchId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyMonths",
                table: "PurchaseOrderLines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "GoodsReceiptLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManufactureDate",
                table: "GoodsReceiptLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierBatchNumber",
                table: "GoodsReceiptLines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyMonths",
                table: "GoodsReceiptLines",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierBatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ManufactureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryBatches_GoodsReceiptLines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalTable: "GoodsReceiptLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryBatches_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBatches_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBatches_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierCode",
                table: "Suppliers",
                column: "SupplierCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_InventoryBatchId",
                table: "StockMovements",
                column: "InventoryBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_ExpiryDate",
                table: "InventoryBatches",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_GoodsReceiptLineId",
                table: "InventoryBatches",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_LocationId",
                table: "InventoryBatches",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_ProductVariantId",
                table: "InventoryBatches",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_ProductVariantId_LocationId_SupplierBatchN~",
                table: "InventoryBatches",
                columns: new[] { "ProductVariantId", "LocationId", "SupplierBatchNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_SupplierBatchNumber",
                table: "InventoryBatches",
                column: "SupplierBatchNumber");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_SupplierId",
                table: "InventoryBatches",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_InventoryBatches_InventoryBatchId",
                table: "StockMovements",
                column: "InventoryBatchId",
                principalTable: "InventoryBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_InventoryBatches_InventoryBatchId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "InventoryBatches");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_SupplierCode",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_InventoryBatchId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "SupplierCode",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "InventoryBatchId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "WarrantyMonths",
                table: "PurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "ManufactureDate",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "SupplierBatchNumber",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "WarrantyMonths",
                table: "GoodsReceiptLines");
        }
    }
}
