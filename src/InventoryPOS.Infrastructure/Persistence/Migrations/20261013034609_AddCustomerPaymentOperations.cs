using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPaymentOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentRequestId",
                table: "SalePayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerPaymentOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetSaleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPaymentOperations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPaymentOperations_PaymentRequestId",
                table: "CustomerPaymentOperations",
                column: "PaymentRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPaymentOperations");

            migrationBuilder.DropColumn(
                name: "PaymentRequestId",
                table: "SalePayments");
        }
    }
}
