using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutDatabaseSafeguards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_StockBalances_ValidQuantities",
                table: "StockBalances",
                sql: "\"QuantityOnHand\" >= 0\r\nAND \"QuantityReserved\" >= 0\r\nAND \"QuantityReserved\" <= \"QuantityOnHand\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sales_NonNegativeAmounts",
                table: "Sales",
                sql: "\"Subtotal\" >= 0\r\nAND \"DiscountAmount\" >= 0\r\nAND \"LineDiscountAmount\" >= 0\r\nAND \"OrderDiscountAmount\" >= 0\r\nAND \"TransportAmount\" >= 0\r\nAND \"TaxAmount\" >= 0\r\nAND \"TotalAmount\" >= 0\r\nAND \"PaidAmount\" >= 0\r\nAND \"DueAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sales_TaxRate",
                table: "Sales",
                sql: "\"SalesTaxRate\" >= 0\r\nAND \"SalesTaxRate\" <= 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockBalances_ValidQuantities",
                table: "StockBalances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sales_NonNegativeAmounts",
                table: "Sales");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sales_TaxRate",
                table: "Sales");
        }
    }
}
