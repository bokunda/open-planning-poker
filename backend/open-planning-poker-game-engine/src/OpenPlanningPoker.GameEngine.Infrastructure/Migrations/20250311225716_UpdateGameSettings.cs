using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGameSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_break_allowed",
                table: "gamesettings");

            migrationBuilder.DropColumn(
                name: "voting_time",
                table: "gamesettings");

            migrationBuilder.AddColumn<string>(
                name: "deck_setup",
                table: "gamesettings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deck_setup",
                table: "gamesettings");

            migrationBuilder.AddColumn<bool>(
                name: "is_break_allowed",
                table: "gamesettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "voting_time",
                table: "gamesettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
