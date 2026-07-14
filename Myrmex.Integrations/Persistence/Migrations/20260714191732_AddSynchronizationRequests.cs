using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Myrmex.Integrations.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSynchronizationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "integration");

            migrationBuilder.CreateTable(
                name: "synchronization_requests",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SourceInstance = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExternalDataVersion = table.Column<byte[]>(type: "varbinary(128)", maxLength: 128, nullable: false),
                    ExternalDocumentNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExternalDocumentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Trigger = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessingStartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_synchronization_requests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_integration_synchronization_requests_idempotency",
                schema: "integration",
                table: "synchronization_requests",
                columns: new[] { "SourceSystem", "SourceInstance", "EntityType", "ExternalId", "ExternalDataVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "synchronization_requests",
                schema: "integration");
        }
    }
}
