using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSortingBoard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sort_column",
                table: "tickets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "sort_contested",
                table: "tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "sort_last_moved_by",
                table: "tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_moves",
                table: "tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "sorting_open",
                table: "gamesettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sort_column",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "sort_contested",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "sort_last_moved_by",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "sort_moves",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "sorting_open",
                table: "gamesettings");
        }
    }
}
