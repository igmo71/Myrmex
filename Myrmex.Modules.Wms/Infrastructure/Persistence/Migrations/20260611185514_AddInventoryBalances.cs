using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_balances",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockKeepingUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_inventory_balances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wms_inventory_balances_stock_keeping_units_stock_keeping_unit_id",
                        column: x => x.StockKeepingUnitId,
                        principalSchema: "wms",
                        principalTable: "stock_keeping_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_inventory_balances_storage_locations_storage_location_id",
                        column: x => x.StorageLocationId,
                        principalSchema: "wms",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wms_inventory_balances_storage_location_id",
                schema: "wms",
                table: "inventory_balances",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "UX_wms_inventory_balances_stock_keeping_unit_id_storage_location_id",
                schema: "wms",
                table: "inventory_balances",
                columns: new[] { "StockKeepingUnitId", "StorageLocationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_balances",
                schema: "wms");
        }
    }
}
