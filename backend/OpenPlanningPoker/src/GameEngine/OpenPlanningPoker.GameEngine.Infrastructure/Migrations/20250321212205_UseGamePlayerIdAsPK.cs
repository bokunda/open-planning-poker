using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseGamePlayerIdAsPK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_gameplayer",
                table: "gameplayer");

            migrationBuilder.AddPrimaryKey(
                name: "pk_gameplayer",
                table: "gameplayer",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_gameplayer_game_id",
                table: "gameplayer",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "ix_gameplayer_player_id",
                table: "gameplayer",
                column: "player_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_gameplayer",
                table: "gameplayer");

            migrationBuilder.DropIndex(
                name: "ix_gameplayer_game_id",
                table: "gameplayer");

            migrationBuilder.DropIndex(
                name: "ix_gameplayer_player_id",
                table: "gameplayer");

            migrationBuilder.AddPrimaryKey(
                name: "pk_gameplayer",
                table: "gameplayer",
                columns: new[] { "game_id", "player_id" });
        }
    }
}
