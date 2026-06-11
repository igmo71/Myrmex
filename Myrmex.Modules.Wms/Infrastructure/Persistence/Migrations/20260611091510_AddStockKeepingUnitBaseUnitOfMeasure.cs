using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockKeepingUnitBaseUnitOfMeasure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BaseUnitOfMeasureId",
                schema: "wms",
                table: "stock_keeping_units",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_wms_stock_keeping_units_base_unit_of_measure_id",
                schema: "wms",
                table: "stock_keeping_units",
                column: "BaseUnitOfMeasureId");

            migrationBuilder.AddForeignKey(
                name: "FK_wms_stock_keeping_units_units_of_measure_base_unit_of_measure_id",
                schema: "wms",
                table: "stock_keeping_units",
                column: "BaseUnitOfMeasureId",
                principalSchema: "wms",
                principalTable: "units_of_measure",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_wms_stock_keeping_units_units_of_measure_base_unit_of_measure_id",
                schema: "wms",
                table: "stock_keeping_units");

            migrationBuilder.DropIndex(
                name: "IX_wms_stock_keeping_units_base_unit_of_measure_id",
                schema: "wms",
                table: "stock_keeping_units");

            migrationBuilder.DropColumn(
                name: "BaseUnitOfMeasureId",
                schema: "wms",
                table: "stock_keeping_units");
        }
    }
}
