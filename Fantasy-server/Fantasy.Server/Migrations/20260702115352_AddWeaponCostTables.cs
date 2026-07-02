using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fantasy.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddWeaponCostTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "weapon_awaken_cost",
                schema: "game_data",
                columns: table => new
                {
                    WeaponId = table.Column<int>(type: "integer", nullable: false),
                    AwakeningLevel = table.Column<long>(type: "bigint", nullable: false),
                    RequiredCount = table.Column<int>(type: "integer", nullable: false),
                    RequiredMithril = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weapon_awaken_cost", x => new { x.WeaponId, x.AwakeningLevel });
                });

            migrationBuilder.CreateTable(
                name: "weapon_enhancement_cost",
                schema: "game_data",
                columns: table => new
                {
                    WeaponId = table.Column<int>(type: "integer", nullable: false),
                    EnhancementLevel = table.Column<long>(type: "bigint", nullable: false),
                    RequiredGold = table.Column<long>(type: "bigint", nullable: false),
                    RequiredScroll = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weapon_enhancement_cost", x => new { x.WeaponId, x.EnhancementLevel });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weapon_awaken_cost",
                schema: "game_data");

            migrationBuilder.DropTable(
                name: "weapon_enhancement_cost",
                schema: "game_data");
        }
    }
}
