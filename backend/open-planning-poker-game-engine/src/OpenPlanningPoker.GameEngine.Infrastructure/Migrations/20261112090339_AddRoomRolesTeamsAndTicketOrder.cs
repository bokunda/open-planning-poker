using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomRolesTeamsAndTicketOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "final_estimate",
                table: "tickets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "position",
                table: "tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "async_voting_ends_at",
                table: "gamesettings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "auto_reveal",
                table: "gamesettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "host_id",
                table: "games",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "team_id",
                table: "games",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_co_host",
                table: "gameplayer",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "role",
                table: "gameplayer",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing games keep their creator as host; existing tickets keep creation order.
            migrationBuilder.Sql("UPDATE games SET host_id = created_by WHERE host_id = '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql(@"
                UPDATE tickets AS t
                SET position = ordered.rn
                FROM (SELECT id, ROW_NUMBER() OVER (PARTITION BY game_id ORDER BY created_on, id) - 1 AS rn FROM tickets) AS ordered
                WHERE t.id = ordered.id;");

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(4080)", maxLength: 4080, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_team_members_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_games_team_id",
                table: "games",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_team_members_player_id",
                table: "team_members",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_team_members_team_id_player_id",
                table: "team_members",
                columns: new[] { "team_id", "player_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team_members");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropIndex(
                name: "ix_games_team_id",
                table: "games");

            migrationBuilder.DropColumn(
                name: "final_estimate",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "position",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "async_voting_ends_at",
                table: "gamesettings");

            migrationBuilder.DropColumn(
                name: "auto_reveal",
                table: "gamesettings");

            migrationBuilder.DropColumn(
                name: "host_id",
                table: "games");

            migrationBuilder.DropColumn(
                name: "team_id",
                table: "games");

            migrationBuilder.DropColumn(
                name: "is_co_host",
                table: "gameplayer");

            migrationBuilder.DropColumn(
                name: "role",
                table: "gameplayer");
        }
    }
}
