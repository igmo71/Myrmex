using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDataVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ExternalDataVersion",
                schema: "wms",
                table: "warehouses",
                type: "varbinary(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ExternalDataVersion",
                schema: "wms",
                table: "units_of_measure",
                type: "varbinary(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ExternalDataVersion",
                schema: "wms",
                table: "stock_keeping_units",
                type: "varbinary(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalDataVersion",
                schema: "wms",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "ExternalDataVersion",
                schema: "wms",
                table: "units_of_measure");

            migrationBuilder.DropColumn(
                name: "ExternalDataVersion",
                schema: "wms",
                table: "stock_keeping_units");
        }
    }
}
