using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fantasy.Server.Migrations
{
    /// <inheritdoc />
    public partial class CreateTutorialTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tutorial");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "player",
                table: "player_stage",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "player",
                table: "player_session",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "player",
                table: "player_resource",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "dungeon",
                table: "gold_dungeon_run",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "dungeon",
                table: "account_dungeon_ticket",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "player_tutorial",
                schema: "tutorial",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    TutorialId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_tutorial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_tutorial_player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "player",
                        principalTable: "player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_tutorial_PlayerId_TutorialId",
                schema: "tutorial",
                table: "player_tutorial",
                columns: new[] { "PlayerId", "TutorialId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_tutorial",
                schema: "tutorial");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "player",
                table: "player_stage");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "player",
                table: "player_session");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "player",
                table: "player_resource");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "dungeon",
                table: "gold_dungeon_run");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "dungeon",
                table: "account_dungeon_ticket");
        }
    }
}
