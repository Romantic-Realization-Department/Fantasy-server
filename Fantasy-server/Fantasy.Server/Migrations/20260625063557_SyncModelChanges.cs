using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fantasy.Server.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
