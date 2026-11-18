using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecapsAndRoomLook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "celebrations",
                table: "gamesettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "seasonal_cards",
                table: "gamesettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "theme",
                table: "gamesettings",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "classic");

            migrationBuilder.AddColumn<bool>(
                name: "recap_show_names",
                table: "games",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "recap_token",
                table: "games",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "avatar",
                table: "gameplayer",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_games_recap_token",
                table: "games",
                column: "recap_token",
                unique: true,
                filter: "recap_token IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_games_recap_token",
                table: "games");

            migrationBuilder.DropColumn(
                name: "celebrations",
                table: "gamesettings");

            migrationBuilder.DropColumn(
                name: "seasonal_cards",
                table: "gamesettings");

            migrationBuilder.DropColumn(
                name: "theme",
                table: "gamesettings");

            migrationBuilder.DropColumn(
                name: "recap_show_names",
                table: "games");

            migrationBuilder.DropColumn(
                name: "recap_token",
                table: "games");

            migrationBuilder.DropColumn(
                name: "avatar",
                table: "gameplayer");
        }
    }
}
