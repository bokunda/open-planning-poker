using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The audits table exists since the first migration but nothing ever wrote to it, so the old
            // name/description columns hold no data (GitHub #20).
            migrationBuilder.DropIndex(
                name: "ix_audits_object_id",
                table: "audits");

            migrationBuilder.DropIndex(
                name: "ix_audits_object_type",
                table: "audits");

            migrationBuilder.DropIndex(
                name: "ix_audits_type",
                table: "audits");

            migrationBuilder.DropColumn(
                name: "description",
                table: "audits");

            migrationBuilder.DropColumn(
                name: "name",
                table: "audits");

            migrationBuilder.RenameColumn(
                name: "object_type",
                table: "audits",
                newName: "scope");

            migrationBuilder.RenameColumn(
                name: "object_id",
                table: "audits",
                newName: "scope_id");

            migrationBuilder.AddColumn<string>(
                name: "action",
                table: "audits",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "detail",
                table: "audits",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "target_id",
                table: "audits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ticket_id",
                table: "audits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "value",
                table: "audits",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_audits_created_on",
                table: "audits",
                column: "created_on");

            migrationBuilder.CreateIndex(
                name: "ix_audits_scope_scope_id_created_on",
                table: "audits",
                columns: new[] { "scope", "scope_id", "created_on" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_audits_created_on",
                table: "audits");

            migrationBuilder.DropIndex(
                name: "ix_audits_scope_scope_id_created_on",
                table: "audits");

            migrationBuilder.DropColumn(
                name: "action",
                table: "audits");

            migrationBuilder.DropColumn(
                name: "detail",
                table: "audits");

            migrationBuilder.DropColumn(
                name: "target_id",
                table: "audits");

            migrationBuilder.DropColumn(
                name: "ticket_id",
                table: "audits");

            migrationBuilder.DropColumn(
                name: "value",
                table: "audits");

            migrationBuilder.RenameColumn(
                name: "scope_id",
                table: "audits",
                newName: "object_id");

            migrationBuilder.RenameColumn(
                name: "scope",
                table: "audits",
                newName: "object_type");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "audits",
                type: "character varying(4080)",
                maxLength: 4080,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "audits",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_audits_object_id",
                table: "audits",
                column: "object_id");

            migrationBuilder.CreateIndex(
                name: "ix_audits_object_type",
                table: "audits",
                column: "object_type");

            migrationBuilder.CreateIndex(
                name: "ix_audits_type",
                table: "audits",
                column: "type");
        }
    }
}
