using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fantasy.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reward_transaction",
                schema: "player",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceRefId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RewardType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RewardRefId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward_transaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reward_transaction_player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "player",
                        principalTable: "player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reward_transaction_PlayerId_CreatedAt",
                schema: "player",
                table: "reward_transaction",
                columns: new[] { "PlayerId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reward_transaction",
                schema: "player");
        }
    }
}
