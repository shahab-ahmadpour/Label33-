using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Label33.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "InventoryReservations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_OrderId",
                table: "InventoryReservations",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryReservations_Orders_OrderId",
                table: "InventoryReservations",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryReservations_Orders_OrderId",
                table: "InventoryReservations");

            migrationBuilder.DropIndex(
                name: "IX_InventoryReservations_OrderId",
                table: "InventoryReservations");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "InventoryReservations");
        }
    }
}
