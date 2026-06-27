using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExternalRefKey",
                schema: "wms",
                table: "warehouses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastImportedAtUtc",
                schema: "wms",
                table: "warehouses",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExternalRefKey",
                schema: "wms",
                table: "units_of_measure",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastImportedAtUtc",
                schema: "wms",
                table: "units_of_measure",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExternalRefKey",
                schema: "wms",
                table: "stock_keeping_units",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastImportedAtUtc",
                schema: "wms",
                table: "stock_keeping_units",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_wms_warehouses_external_ref_key",
                schema: "wms",
                table: "warehouses",
                column: "ExternalRefKey",
                unique: true,
                filter: "[ExternalRefKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_wms_units_of_measure_external_ref_key",
                schema: "wms",
                table: "units_of_measure",
                column: "ExternalRefKey",
                unique: true,
                filter: "[ExternalRefKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_wms_stock_keeping_units_external_ref_key",
                schema: "wms",
                table: "stock_keeping_units",
                column: "ExternalRefKey",
                unique: true,
                filter: "[ExternalRefKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_wms_warehouses_external_ref_key",
                schema: "wms",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "UX_wms_units_of_measure_external_ref_key",
                schema: "wms",
                table: "units_of_measure");

            migrationBuilder.DropIndex(
                name: "UX_wms_stock_keeping_units_external_ref_key",
                schema: "wms",
                table: "stock_keeping_units");

            migrationBuilder.DropColumn(
                name: "ExternalRefKey",
                schema: "wms",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "LastImportedAtUtc",
                schema: "wms",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "ExternalRefKey",
                schema: "wms",
                table: "units_of_measure");

            migrationBuilder.DropColumn(
                name: "LastImportedAtUtc",
                schema: "wms",
                table: "units_of_measure");

            migrationBuilder.DropColumn(
                name: "ExternalRefKey",
                schema: "wms",
                table: "stock_keeping_units");

            migrationBuilder.DropColumn(
                name: "LastImportedAtUtc",
                schema: "wms",
                table: "stock_keeping_units");
        }
    }
}
