using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_provider",
                table: "tickets",
                type: "text",
                nullable: true);

            // Tickets linked before OPP-130 all came from Jira.
            migrationBuilder.Sql("UPDATE tickets SET external_provider = 'jira' WHERE external_key IS NOT NULL;");

            migrationBuilder.CreateTable(
                name: "tracker_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    protected_access_token = table.Column<string>(type: "text", nullable: false),
                    protected_refresh_token = table.Column<string>(type: "text", nullable: true),
                    access_token_expires_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    account_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    needs_reconnect = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tracker_connections", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tracker_connections_player_id_provider",
                table: "tracker_connections",
                columns: new[] { "player_id", "provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tracker_connections");

            migrationBuilder.DropColumn(
                name: "external_provider",
                table: "tickets");
        }
    }
}
