using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJiraIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_key",
                table: "tickets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_url",
                table: "tickets",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    friendly_name = table.Column<string>(type: "text", nullable: true),
                    xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_protection_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "jira_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cloud_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    site_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    site_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    protected_access_token = table.Column<string>(type: "text", nullable: false),
                    protected_refresh_token = table.Column<string>(type: "text", nullable: false),
                    access_token_expires_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    needs_reconnect = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jira_connections", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_jira_connections_player_id",
                table: "jira_connections",
                column: "player_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_protection_keys");

            migrationBuilder.DropTable(
                name: "jira_connections");

            migrationBuilder.DropColumn(
                name: "external_key",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "external_url",
                table: "tickets");
        }
    }
}
