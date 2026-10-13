using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesQuotePricingSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LineDiscountAmount",
                table: "SalesQuotes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderDiscountAmount",
                table: "SalesQuotes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "OrderDiscountType",
                table: "SalesQuotes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderDiscountValue",
                table: "SalesQuotes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SalesTaxRate",
                table: "SalesQuotes",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TransportAmount",
                table: "SalesQuotes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "SalesQuoteLines",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "SalesQuoteLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "SalesQuoteLines",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LineDiscountAmount",
                table: "SalesQuotes");

            migrationBuilder.DropColumn(
                name: "OrderDiscountAmount",
                table: "SalesQuotes");

            migrationBuilder.DropColumn(
                name: "OrderDiscountType",
                table: "SalesQuotes");

            migrationBuilder.DropColumn(
                name: "OrderDiscountValue",
                table: "SalesQuotes");

            migrationBuilder.DropColumn(
                name: "SalesTaxRate",
                table: "SalesQuotes");

            migrationBuilder.DropColumn(
                name: "TransportAmount",
                table: "SalesQuotes");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "SalesQuoteLines");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "SalesQuoteLines");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "SalesQuoteLines");
        }
    }
}
