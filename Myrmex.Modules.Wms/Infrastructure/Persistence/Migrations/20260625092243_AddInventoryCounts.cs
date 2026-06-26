using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_counts",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByActorId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CompletedByActorId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CancelledByActorId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_inventory_counts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wms_inventory_counts_warehouses_warehouse_id",
                        column: x => x.WarehouseId,
                        principalSchema: "wms",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_count_lines",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryCountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockKeepingUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ExpectedBalanceVersion = table.Column<byte[]>(type: "varbinary(8)", maxLength: 8, nullable: true),
                    CountedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    VarianceQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CountedByActorId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CountedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AppliedByActorId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AppliedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AppliedInventoryTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersedesInventoryCountLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_inventory_count_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wms_inventory_count_lines_inventory_count_lines_supersedes_inventory_count_line_id",
                        column: x => x.SupersedesInventoryCountLineId,
                        principalSchema: "wms",
                        principalTable: "inventory_count_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_count_lines_inventory_counts_inventory_count_id",
                        column: x => x.InventoryCountId,
                        principalSchema: "wms",
                        principalTable: "inventory_counts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_count_lines_inventory_transactions_applied_inventory_transaction_id",
                        column: x => x.AppliedInventoryTransactionId,
                        principalSchema: "wms",
                        principalTable: "inventory_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_count_lines_stock_keeping_units_stock_keeping_unit_id",
                        column: x => x.StockKeepingUnitId,
                        principalSchema: "wms",
                        principalTable: "stock_keeping_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_count_lines_storage_locations_storage_location_id",
                        column: x => x.StorageLocationId,
                        principalSchema: "wms",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_count_lines_inventory_count_id",
                schema: "wms",
                table: "inventory_count_lines",
                column: "InventoryCountId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_count_lines_status",
                schema: "wms",
                table: "inventory_count_lines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_count_lines_stock_keeping_unit_id",
                schema: "wms",
                table: "inventory_count_lines",
                column: "StockKeepingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_count_lines_storage_location_id",
                schema: "wms",
                table: "inventory_count_lines",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "UX_wms_inventory_count_lines_applied_inventory_transaction_id",
                schema: "wms",
                table: "inventory_count_lines",
                column: "AppliedInventoryTransactionId",
                unique: true,
                filter: "[AppliedInventoryTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_wms_inventory_count_lines_current_pair",
                schema: "wms",
                table: "inventory_count_lines",
                columns: new[] { "InventoryCountId", "StockKeepingUnitId", "StorageLocationId" },
                unique: true,
                filter: "[IsCurrent] = CAST(1 AS bit)");

            migrationBuilder.CreateIndex(
                name: "UX_wms_inventory_count_lines_supersedes_inventory_count_line_id",
                schema: "wms",
                table: "inventory_count_lines",
                column: "SupersedesInventoryCountLineId",
                unique: true,
                filter: "[SupersedesInventoryCountLineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_counts_created_at_utc",
                schema: "wms",
                table: "inventory_counts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_counts_status",
                schema: "wms",
                table: "inventory_counts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_counts_warehouse_id",
                schema: "wms",
                table: "inventory_counts",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_count_lines",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "inventory_counts",
                schema: "wms");
        }
    }
}
