using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fantasy.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddDungeonTicketAndGoldRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dungeon");

            migrationBuilder.CreateTable(
                name: "account_dungeon_ticket",
                schema: "dungeon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    TicketCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastDailyGrantDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DailyAdRewardClaimedDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_dungeon_ticket", x => x.Id);
                    table.ForeignKey(
                        name: "FK_account_dungeon_ticket_account_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "account",
                        principalTable: "account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gold_dungeon_run",
                schema: "dungeon",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxClicks = table.Column<int>(type: "integer", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClaimedClicks = table.Column<int>(type: "integer", nullable: true),
                    EarnedGold = table.Column<long>(type: "bigint", nullable: true),
                    EarnedMithril = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gold_dungeon_run", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gold_dungeon_run_account_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "account",
                        principalTable: "account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_dungeon_ticket_AccountId",
                schema: "dungeon",
                table: "account_dungeon_ticket",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gold_dungeon_run_AccountId",
                schema: "dungeon",
                table: "gold_dungeon_run",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_dungeon_ticket",
                schema: "dungeon");

            migrationBuilder.DropTable(
                name: "gold_dungeon_run",
                schema: "dungeon");
        }
    }
}
