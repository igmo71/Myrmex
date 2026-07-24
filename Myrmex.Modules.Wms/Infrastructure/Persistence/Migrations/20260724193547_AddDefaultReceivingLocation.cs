using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultReceivingLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultReceivingLocationId",
                schema: "wms",
                table: "warehouses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ExternalDataVersion",
                schema: "wms",
                table: "receiving_orders",
                type: "varbinary(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExternalRefKey",
                schema: "wms",
                table: "receiving_orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastImportedAtUtc",
                schema: "wms",
                table: "receiving_orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_DefaultReceivingLocationId",
                schema: "wms",
                table: "warehouses",
                column: "DefaultReceivingLocationId");

            migrationBuilder.CreateIndex(
                name: "UX_wms_receiving_orders_external_ref_key",
                schema: "wms",
                table: "receiving_orders",
                column: "ExternalRefKey",
                unique: true,
                filter: "[ExternalRefKey] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_wms_warehouses_storage_locations_default_receiving_location_id",
                schema: "wms",
                table: "warehouses",
                column: "DefaultReceivingLocationId",
                principalSchema: "wms",
                principalTable: "storage_locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_wms_warehouses_storage_locations_default_receiving_location_id",
                schema: "wms",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_warehouses_DefaultReceivingLocationId",
                schema: "wms",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "UX_wms_receiving_orders_external_ref_key",
                schema: "wms",
                table: "receiving_orders");

            migrationBuilder.DropColumn(
                name: "DefaultReceivingLocationId",
                schema: "wms",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "ExternalDataVersion",
                schema: "wms",
                table: "receiving_orders");

            migrationBuilder.DropColumn(
                name: "ExternalRefKey",
                schema: "wms",
                table: "receiving_orders");

            migrationBuilder.DropColumn(
                name: "LastImportedAtUtc",
                schema: "wms",
                table: "receiving_orders");
        }
    }
}
