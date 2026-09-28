using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerWarrantySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WarrantyEndDate",
                table: "SaleLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyMonths",
                table: "SaleLines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarrantyStartDate",
                table: "SaleLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultWarrantyMonths",
                table: "ProductVariants",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarrantyEndDate",
                table: "SaleLines");

            migrationBuilder.DropColumn(
                name: "WarrantyMonths",
                table: "SaleLines");

            migrationBuilder.DropColumn(
                name: "WarrantyStartDate",
                table: "SaleLines");

            migrationBuilder.DropColumn(
                name: "DefaultWarrantyMonths",
                table: "ProductVariants");
        }
    }
}
