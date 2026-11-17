using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "host_claim_expires_on",
                table: "games",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "host_claim_hash",
                table: "games",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "chat_posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    workspace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    channel_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    message_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    service_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_posts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "slack_installations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    workspace_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    protected_bot_token = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slack_installations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chat_posts_game_id",
                table: "chat_posts",
                column: "game_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_slack_installations_workspace_id",
                table: "slack_installations",
                column: "workspace_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_posts");

            migrationBuilder.DropTable(
                name: "slack_installations");

            migrationBuilder.DropColumn(
                name: "host_claim_expires_on",
                table: "games");

            migrationBuilder.DropColumn(
                name: "host_claim_hash",
                table: "games");
        }
    }
}
