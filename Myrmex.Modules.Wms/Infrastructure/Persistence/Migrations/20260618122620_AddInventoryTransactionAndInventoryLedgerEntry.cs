using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryTransactionAndInventoryLedgerEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "wms",
                table: "inventory_balances",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "inventory_transactions",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_inventory_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_ledger_entries",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockKeepingUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_inventory_ledger_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wms_inventory_ledger_entries_inventory_transactions_inventory_transaction_id",
                        column: x => x.InventoryTransactionId,
                        principalSchema: "wms",
                        principalTable: "inventory_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_ledger_entries_stock_keeping_units_stock_keeping_unit_id",
                        column: x => x.StockKeepingUnitId,
                        principalSchema: "wms",
                        principalTable: "stock_keeping_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_ledger_entries_storage_locations_storage_location_id",
                        column: x => x.StorageLocationId,
                        principalSchema: "wms",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ledger_entries_InventoryTransactionId",
                schema: "wms",
                table: "inventory_ledger_entries",
                column: "InventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_ledger_entries_stock_keeping_unit_id",
                schema: "wms",
                table: "inventory_ledger_entries",
                column: "StockKeepingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_ledger_entries_storage_location_id",
                schema: "wms",
                table: "inventory_ledger_entries",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_transactions_occurred_at_utc",
                schema: "wms",
                table: "inventory_transactions",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_ledger_entries",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "inventory_transactions",
                schema: "wms");

            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "wms",
                table: "inventory_balances");
        }
    }
}
