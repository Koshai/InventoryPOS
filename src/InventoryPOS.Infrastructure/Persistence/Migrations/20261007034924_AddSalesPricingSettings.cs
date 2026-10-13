using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesPricingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultSalesTaxRate",
                table: "BusinessProfiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "EnableSalesLineDiscount",
                table: "BusinessProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableSalesOrderDiscount",
                table: "BusinessProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableSalesTax",
                table: "BusinessProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableSalesTransportCharge",
                table: "BusinessProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultSalesTaxRate",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "EnableSalesLineDiscount",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "EnableSalesOrderDiscount",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "EnableSalesTax",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "EnableSalesTransportCharge",
                table: "BusinessProfiles");
        }
    }
}
