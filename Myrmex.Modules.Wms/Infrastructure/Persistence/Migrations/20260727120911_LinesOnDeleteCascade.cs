using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinesOnDeleteCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_wms_receiving_order_lines_receiving_orders_receiving_order_id",
                schema: "wms",
                table: "receiving_order_lines");

            migrationBuilder.AddForeignKey(
                name: "FK_wms_receiving_order_lines_receiving_orders_receiving_order_id",
                schema: "wms",
                table: "receiving_order_lines",
                column: "ReceivingOrderId",
                principalSchema: "wms",
                principalTable: "receiving_orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_wms_receiving_order_lines_receiving_orders_receiving_order_id",
                schema: "wms",
                table: "receiving_order_lines");

            migrationBuilder.AddForeignKey(
                name: "FK_wms_receiving_order_lines_receiving_orders_receiving_order_id",
                schema: "wms",
                table: "receiving_order_lines",
                column: "ReceivingOrderId",
                principalSchema: "wms",
                principalTable: "receiving_orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
