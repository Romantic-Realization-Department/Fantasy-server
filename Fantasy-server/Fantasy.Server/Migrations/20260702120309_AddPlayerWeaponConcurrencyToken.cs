using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fantasy.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerWeaponConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "player",
                table: "player_weapon",
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
                table: "player_weapon");
        }
    }
}
