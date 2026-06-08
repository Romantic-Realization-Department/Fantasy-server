using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fantasy.Server.Migrations
{
    /// <inheritdoc />
    public partial class PlayerSinglePerAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_player_AccountId_JobType",
                schema: "player",
                table: "player");

            migrationBuilder.CreateIndex(
                name: "IX_player_AccountId",
                schema: "player",
                table: "player",
                column: "AccountId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_player_AccountId",
                schema: "player",
                table: "player");

            migrationBuilder.CreateIndex(
                name: "IX_player_AccountId_JobType",
                schema: "player",
                table: "player",
                columns: new[] { "AccountId", "JobType" },
                unique: true);
        }
    }
}
