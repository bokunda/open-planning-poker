using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // OPP-114: outlier notes and reactions are on for existing rooms too.
            migrationBuilder.AddColumn<int>(
                name: "confidence",
                table: "votes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "note",
                table: "votes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "estimated_at",
                table: "tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "needs_info",
                table: "tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revealed_at",
                table: "tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "vote_rounds",
                table: "tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "confidence_check",
                table: "gamesettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "discussion_minutes",
                table: "gamesettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "outlier_notes",
                table: "gamesettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "reactions",
                table: "gamesettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "confidence",
                table: "votes");

            migrationBuilder.DropColumn(
                name: "note",
                table: "votes");

            migrationBuilder.DropColumn(
                name: "estimated_at",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "needs_info",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "revealed_at",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "vote_rounds",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "confidence_check",
                table: "gamesettings");

            migrationBuilder.DropColumn(
                name: "discussion_minutes",
                table: "gamesettings");

            migrationBuilder.DropColumn(
                name: "outlier_notes",
                table: "gamesettings");

            migrationBuilder.DropColumn(
                name: "reactions",
                table: "gamesettings");
        }
    }
}
