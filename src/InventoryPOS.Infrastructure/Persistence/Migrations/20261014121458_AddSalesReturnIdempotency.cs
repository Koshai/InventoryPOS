using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesReturnIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnRequestFingerprint",
                table: "SalesReturns",
                type: "character varying(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReturnRequestId",
                table: "SalesReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_ReturnRequestId",
                table: "SalesReturns",
                column: "ReturnRequestId",
                unique: true,
                filter: "\"ReturnRequestId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesReturns_ReturnRequestId",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "ReturnRequestFingerprint",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "ReturnRequestId",
                table: "SalesReturns");
        }
    }
}
