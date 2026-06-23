using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_transfers",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransitStorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_inventory_transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wms_inventory_transfers_storage_locations_transit_storage_location_id",
                        column: x => x.TransitStorageLocationId,
                        principalSchema: "wms",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_transfers_warehouses_destination_warehouse_id",
                        column: x => x.DestinationWarehouseId,
                        principalSchema: "wms",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_transfers_warehouses_source_warehouse_id",
                        column: x => x.SourceWarehouseId,
                        principalSchema: "wms",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transfer_lines",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryTransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockKeepingUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceStorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationStorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_inventory_transfer_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wms_inventory_transfer_lines_inventory_transfers_inventory_transfer_id",
                        column: x => x.InventoryTransferId,
                        principalSchema: "wms",
                        principalTable: "inventory_transfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_transfer_lines_stock_keeping_units_stock_keeping_unit_id",
                        column: x => x.StockKeepingUnitId,
                        principalSchema: "wms",
                        principalTable: "stock_keeping_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_transfer_lines_storage_locations_destination_storage_location_id",
                        column: x => x.DestinationStorageLocationId,
                        principalSchema: "wms",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_transfer_lines_storage_locations_source_storage_location_id",
                        column: x => x.SourceStorageLocationId,
                        principalSchema: "wms",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transfer_movements",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryTransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryTransferLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToStorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_inventory_transfer_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wms_inventory_transfer_movements_inventory_transactions_inventory_transaction_id",
                        column: x => x.InventoryTransactionId,
                        principalSchema: "wms",
                        principalTable: "inventory_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_transfer_movements_inventory_transfer_lines_inventory_transfer_line_id",
                        column: x => x.InventoryTransferLineId,
                        principalSchema: "wms",
                        principalTable: "inventory_transfer_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_transfer_movements_inventory_transfers_inventory_transfer_id",
                        column: x => x.InventoryTransferId,
                        principalSchema: "wms",
                        principalTable: "inventory_transfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_transfer_movements_storage_locations_from_storage_location_id",
                        column: x => x.FromStorageLocationId,
                        principalSchema: "wms",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_transfer_movements_storage_locations_to_storage_location_id",
                        column: x => x.ToStorageLocationId,
                        principalSchema: "wms",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "wms",
                table: "storage_location_types",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "Description", "IsActive", "IsSystem", "Name", "SortOrder", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("018f0000-0000-7000-8000-000000000006"), "INTERNAL_TRANSIT", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System storage location type for internal inventory transfer transit.", true, true, "Internal transit", 60, null },
                    { new Guid("018f0000-0000-7000-8000-000000000007"), "EXTERNAL_TRANSIT", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System storage location type reserved for external inventory transfer transit.", true, true, "External transit", 70, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transfer_lines_destination_storage_location_id",
                schema: "wms",
                table: "inventory_transfer_lines",
                column: "DestinationStorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transfer_lines_inventory_transfer_id",
                schema: "wms",
                table: "inventory_transfer_lines",
                column: "InventoryTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transfer_lines_source_storage_location_id",
                schema: "wms",
                table: "inventory_transfer_lines",
                column: "SourceStorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transfer_lines_stock_keeping_unit_id",
                schema: "wms",
                table: "inventory_transfer_lines",
                column: "StockKeepingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transfer_movements_from_storage_location_id",
                schema: "wms",
                table: "inventory_transfer_movements",
                column: "FromStorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transfer_movements_inventory_transaction_id",
                schema: "wms",
                table: "inventory_transfer_movements",
                column: "InventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transfer_movements_inventory_transfer_id",
                schema: "wms",
                table: "inventory_transfer_movements",
                column: "InventoryTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transfer_movements_inventory_transfer_line_id",
                schema: "wms",
                table: "inventory_transfer_movements",
                column: "InventoryTransferLineId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transfer_movements_to_storage_location_id",
                schema: "wms",
                table: "inventory_transfer_movements",
                column: "ToStorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transfers_destination_warehouse_id",
                schema: "wms",
                table: "inventory_transfers",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transfers_source_warehouse_id",
                schema: "wms",
                table: "inventory_transfers",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transfers_status",
                schema: "wms",
                table: "inventory_transfers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transfers_transit_storage_location_id",
                schema: "wms",
                table: "inventory_transfers",
                column: "TransitStorageLocationId");

            migrationBuilder.CreateIndex(
                name: "UX_wms_inventory_transfers_code",
                schema: "wms",
                table: "inventory_transfers",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_transfer_movements",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "inventory_transfer_lines",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "inventory_transfers",
                schema: "wms");

            migrationBuilder.DeleteData(
                schema: "wms",
                table: "storage_location_types",
                keyColumn: "Id",
                keyValue: new Guid("018f0000-0000-7000-8000-000000000006"));

            migrationBuilder.DeleteData(
                schema: "wms",
                table: "storage_location_types",
                keyColumn: "Id",
                keyValue: new Guid("018f0000-0000-7000-8000-000000000007"));
        }
    }
}
