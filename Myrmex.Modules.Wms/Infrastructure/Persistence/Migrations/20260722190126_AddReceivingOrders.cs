using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivingOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "receiving_orders",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivingLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InventoryTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_receiving_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wms_receiving_orders_inventory_transactions_inventory_transaction_id",
                        column: x => x.InventoryTransactionId,
                        principalSchema: "wms",
                        principalTable: "inventory_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_receiving_orders_storage_locations_receiving_location_id",
                        column: x => x.ReceivingLocationId,
                        principalSchema: "wms",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_receiving_orders_warehouses_warehouse_id",
                        column: x => x.WarehouseId,
                        principalSchema: "wms",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receiving_order_lines",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivingOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockKeepingUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_receiving_order_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wms_receiving_order_lines_receiving_orders_receiving_order_id",
                        column: x => x.ReceivingOrderId,
                        principalSchema: "wms",
                        principalTable: "receiving_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_receiving_order_lines_stock_keeping_units_stock_keeping_unit_id",
                        column: x => x.StockKeepingUnitId,
                        principalSchema: "wms",
                        principalTable: "stock_keeping_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "wms",
                table: "storage_location_types",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "Description", "IsActive", "IsSystem", "Name", "SortOrder", "UpdatedAtUtc" },
                values: new object[] { new Guid("018f0000-0000-7000-8000-000000000008"), "RECEIVING", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Storage location for receiving inventory.", true, true, "Receiving", 80, null });

            migrationBuilder.CreateIndex(
                name: "IX_wms_receiving_order_lines_stock_keeping_unit_id",
                schema: "wms",
                table: "receiving_order_lines",
                column: "StockKeepingUnitId");

            migrationBuilder.CreateIndex(
                name: "UX_wms_receiving_order_lines_receiving_order_id_stock_keeping_unit_id",
                schema: "wms",
                table: "receiving_order_lines",
                columns: new[] { "ReceivingOrderId", "StockKeepingUnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wms_receiving_orders_receiving_location_id",
                schema: "wms",
                table: "receiving_orders",
                column: "ReceivingLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_receiving_orders_warehouse_id",
                schema: "wms",
                table: "receiving_orders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "UX_wms_receiving_orders_inventory_transaction_id",
                schema: "wms",
                table: "receiving_orders",
                column: "InventoryTransactionId",
                unique: true,
                filter: "[InventoryTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_wms_receiving_orders_number",
                schema: "wms",
                table: "receiving_orders",
                column: "Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "receiving_order_lines",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "receiving_orders",
                schema: "wms");

            migrationBuilder.DeleteData(
                schema: "wms",
                table: "storage_location_types",
                keyColumn: "Id",
                keyValue: new Guid("018f0000-0000-7000-8000-000000000008"));
        }
    }
}
