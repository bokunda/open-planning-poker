using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "retros",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    phase = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    show_authors = table.Column<bool>(type: "boolean", nullable: false),
                    dots_per_person = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retros", x => x.id);
                    table.ForeignKey(
                        name: "fk_retros_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_retros_team_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "retro_cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    retro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    column = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retro_cards", x => x.id);
                    table.ForeignKey(
                        name: "fk_retro_cards_retro_retro_id",
                        column: x => x.retro_id,
                        principalTable: "retros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "retro_action_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: true),
                    text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    done = table.Column<bool>(type: "boolean", nullable: false),
                    done_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retro_action_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_retro_action_items_retro_card_card_id",
                        column: x => x.card_id,
                        principalTable: "retro_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_retro_action_items_team_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "retro_votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    retro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retro_votes", x => x.id);
                    table.ForeignKey(
                        name: "fk_retro_votes_retro_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "retro_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_retro_votes_retros_retro_id",
                        column: x => x.retro_id,
                        principalTable: "retros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_retro_action_items_card_id",
                table: "retro_action_items",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "ix_retro_action_items_team_id_done",
                table: "retro_action_items",
                columns: new[] { "team_id", "done" });

            migrationBuilder.CreateIndex(
                name: "ix_retro_cards_retro_id",
                table: "retro_cards",
                column: "retro_id");

            migrationBuilder.CreateIndex(
                name: "ix_retro_votes_card_id",
                table: "retro_votes",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "ix_retro_votes_retro_id_player_id",
                table: "retro_votes",
                columns: new[] { "retro_id", "player_id" });

            migrationBuilder.CreateIndex(
                name: "ix_retros_game_id",
                table: "retros",
                column: "game_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_retros_team_id",
                table: "retros",
                column: "team_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "retro_action_items");

            migrationBuilder.DropTable(
                name: "retro_votes");

            migrationBuilder.DropTable(
                name: "retro_cards");

            migrationBuilder.DropTable(
                name: "retros");
        }
    }
}
