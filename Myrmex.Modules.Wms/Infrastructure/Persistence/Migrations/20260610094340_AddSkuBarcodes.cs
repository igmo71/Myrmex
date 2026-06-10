using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkuBarcodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sku_barcodes",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockKeepingUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Symbology = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_sku_barcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wms_sku_barcodes_stock_keeping_units_stock_keeping_unit_id",
                        column: x => x.StockKeepingUnitId,
                        principalSchema: "wms",
                        principalTable: "stock_keeping_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wms_sku_barcodes_stock_keeping_unit_id",
                schema: "wms",
                table: "sku_barcodes",
                column: "StockKeepingUnitId");

            migrationBuilder.CreateIndex(
                name: "UX_wms_sku_barcodes_value",
                schema: "wms",
                table: "sku_barcodes",
                column: "Value",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sku_barcodes",
                schema: "wms");
        }
    }
}
