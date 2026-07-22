using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkuCharacteristics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AreaSquareMetres",
                schema: "wms",
                table: "stock_keeping_units",
                type: "decimal(28,12)",
                precision: 28,
                scale: 12,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthMetres",
                schema: "wms",
                table: "stock_keeping_units",
                type: "decimal(28,12)",
                precision: 28,
                scale: 12,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VolumeCubicMetres",
                schema: "wms",
                table: "stock_keeping_units",
                type: "decimal(28,12)",
                precision: 28,
                scale: 12,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKilograms",
                schema: "wms",
                table: "stock_keeping_units",
                type: "decimal(28,12)",
                precision: 28,
                scale: 12,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaSquareMetres",
                schema: "wms",
                table: "stock_keeping_units");

            migrationBuilder.DropColumn(
                name: "LengthMetres",
                schema: "wms",
                table: "stock_keeping_units");

            migrationBuilder.DropColumn(
                name: "VolumeCubicMetres",
                schema: "wms",
                table: "stock_keeping_units");

            migrationBuilder.DropColumn(
                name: "WeightKilograms",
                schema: "wms",
                table: "stock_keeping_units");
        }
    }
}
