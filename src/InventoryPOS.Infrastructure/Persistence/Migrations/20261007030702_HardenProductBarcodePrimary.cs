using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenProductBarcodePrimary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductBarcodes_ProductVariantId",
                table: "ProductBarcodes");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBarcodes_ProductVariantId",
                table: "ProductBarcodes",
                column: "ProductVariantId",
                unique: true,
                filter: "\"IsPrimary\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductBarcodes_ProductVariantId",
                table: "ProductBarcodes");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBarcodes_ProductVariantId",
                table: "ProductBarcodes",
                column: "ProductVariantId");
        }
    }
}
