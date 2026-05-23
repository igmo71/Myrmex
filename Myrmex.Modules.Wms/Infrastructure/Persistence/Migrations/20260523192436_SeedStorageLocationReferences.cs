using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedStorageLocationReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "wms",
                table: "storage_location_statuses",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "Description", "IsActive", "IsSystem", "Name", "SortOrder", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("018f0000-0000-7000-8000-000000000101"), "AVAILABLE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Storage location is available for operations.", true, true, "Available", 10, null },
                    { new Guid("018f0000-0000-7000-8000-000000000102"), "BLOCKED", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Storage location is blocked for operations.", true, true, "Blocked", 20, null },
                    { new Guid("018f0000-0000-7000-8000-000000000103"), "MAINTENANCE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Storage location is under maintenance.", true, true, "Maintenance", 30, null },
                    { new Guid("018f0000-0000-7000-8000-000000000104"), "INVENTORY_CHECK", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Storage location is under inventory check.", true, true, "Inventory check", 40, null }
                });

            migrationBuilder.InsertData(
                schema: "wms",
                table: "storage_location_types",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "Description", "IsActive", "IsSystem", "Name", "SortOrder", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("018f0000-0000-7000-8000-000000000001"), "PALLET_RACK", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Pallet rack storage location.", true, true, "Pallet rack", 10, null },
                    { new Guid("018f0000-0000-7000-8000-000000000002"), "SHELF", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Shelf or bin storage location.", true, true, "Shelf", 20, null },
                    { new Guid("018f0000-0000-7000-8000-000000000003"), "FLOOR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Floor storage location.", true, true, "Floor", 30, null },
                    { new Guid("018f0000-0000-7000-8000-000000000004"), "STAGING", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Temporary staging location.", true, true, "Staging", 40, null },
                    { new Guid("018f0000-0000-7000-8000-000000000005"), "DOCK", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Receiving or shipping dock.", true, true, "Dock", 50, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "wms",
                table: "storage_location_statuses",
                keyColumn: "Id",
                keyValue: new Guid("018f0000-0000-7000-8000-000000000101"));

            migrationBuilder.DeleteData(
                schema: "wms",
                table: "storage_location_statuses",
                keyColumn: "Id",
                keyValue: new Guid("018f0000-0000-7000-8000-000000000102"));

            migrationBuilder.DeleteData(
                schema: "wms",
                table: "storage_location_statuses",
                keyColumn: "Id",
                keyValue: new Guid("018f0000-0000-7000-8000-000000000103"));

            migrationBuilder.DeleteData(
                schema: "wms",
                table: "storage_location_statuses",
                keyColumn: "Id",
                keyValue: new Guid("018f0000-0000-7000-8000-000000000104"));

            migrationBuilder.DeleteData(
                schema: "wms",
                table: "storage_location_types",
                keyColumn: "Id",
                keyValue: new Guid("018f0000-0000-7000-8000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "wms",
                table: "storage_location_types",
                keyColumn: "Id",
                keyValue: new Guid("018f0000-0000-7000-8000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "wms",
                table: "storage_location_types",
                keyColumn: "Id",
                keyValue: new Guid("018f0000-0000-7000-8000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "wms",
                table: "storage_location_types",
                keyColumn: "Id",
                keyValue: new Guid("018f0000-0000-7000-8000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "wms",
                table: "storage_location_types",
                keyColumn: "Id",
                keyValue: new Guid("018f0000-0000-7000-8000-000000000005"));
        }
    }
}
