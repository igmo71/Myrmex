using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "storage_location_statuses",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_storage_location_statuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "storage_location_types",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_storage_location_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "storage_locations",
                schema: "wms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageLocationTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageLocationStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsPickable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wms_storage_locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wms_storage_locations_storage_location_statuses_storage_location_status_id",
                        column: x => x.StorageLocationStatusId,
                        principalSchema: "wms",
                        principalTable: "storage_location_statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_storage_locations_storage_location_types_storage_location_type_id",
                        column: x => x.StorageLocationTypeId,
                        principalSchema: "wms",
                        principalTable: "storage_location_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_storage_locations_warehouses_warehouse_id",
                        column: x => x.WarehouseId,
                        principalSchema: "wms",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wms_storage_locations_zones_zone_id",
                        column: x => x.ZoneId,
                        principalSchema: "wms",
                        principalTable: "zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_wms_storage_location_statuses_code",
                schema: "wms",
                table: "storage_location_statuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_wms_storage_location_types_code",
                schema: "wms",
                table: "storage_location_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wms_storage_locations_storage_location_status_id",
                schema: "wms",
                table: "storage_locations",
                column: "StorageLocationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_storage_locations_storage_location_type_id",
                schema: "wms",
                table: "storage_locations",
                column: "StorageLocationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_storage_locations_warehouse_id",
                schema: "wms",
                table: "storage_locations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_wms_storage_locations_zone_id",
                schema: "wms",
                table: "storage_locations",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "UX_wms_storage_locations_warehouse_id_code",
                schema: "wms",
                table: "storage_locations",
                columns: new[] { "WarehouseId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "storage_locations",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "storage_location_statuses",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "storage_location_types",
                schema: "wms");
        }
    }
}
