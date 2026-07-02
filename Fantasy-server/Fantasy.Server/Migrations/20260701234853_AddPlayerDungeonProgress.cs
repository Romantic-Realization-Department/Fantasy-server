using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fantasy.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerDungeonProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_dungeon_progress",
                schema: "dungeon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    DungeonType = table.Column<string>(type: "text", nullable: false),
                    HighestClearedStage = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    HighScore = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    LastClearedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_dungeon_progress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_dungeon_progress_player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "player",
                        principalTable: "player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_dungeon_progress_PlayerId_DungeonType",
                schema: "dungeon",
                table: "player_dungeon_progress",
                columns: new[] { "PlayerId", "DungeonType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_dungeon_progress",
                schema: "dungeon");
        }
    }
}
