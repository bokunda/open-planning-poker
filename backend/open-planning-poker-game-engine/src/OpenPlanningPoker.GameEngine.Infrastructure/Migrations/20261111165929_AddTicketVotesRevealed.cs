using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketVotesRevealed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "votes_revealed",
                table: "tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Tickets created before this migration exposed their votes to everyone already;
            // keep them readable instead of hiding historical results.
            migrationBuilder.Sql("UPDATE tickets SET votes_revealed = TRUE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "votes_revealed",
                table: "tickets");
        }
    }
}
