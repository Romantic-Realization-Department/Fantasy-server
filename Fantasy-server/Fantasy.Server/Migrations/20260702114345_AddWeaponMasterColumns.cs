using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fantasy.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddWeaponMasterColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MaxAwakeningLevel",
                schema: "game_data",
                table: "weapon_data",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "MaxEnhancementLevel",
                schema: "game_data",
                table: "weapon_data",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "SynthesizeRequiredCount",
                schema: "game_data",
                table: "weapon_data",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SynthesizeResultWeaponId",
                schema: "game_data",
                table: "weapon_data",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE game_data.weapon_data SET "MaxEnhancementLevel" = 10, "MaxAwakeningLevel" = 3;
                UPDATE game_data.weapon_data SET "SynthesizeRequiredCount" = 3, "SynthesizeResultWeaponId" = 1002 WHERE "WeaponId" = 1001;
                UPDATE game_data.weapon_data SET "SynthesizeRequiredCount" = 3, "SynthesizeResultWeaponId" = 2002 WHERE "WeaponId" = 2001;
                UPDATE game_data.weapon_data SET "SynthesizeRequiredCount" = 3, "SynthesizeResultWeaponId" = 3002 WHERE "WeaponId" = 3001;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxAwakeningLevel",
                schema: "game_data",
                table: "weapon_data");

            migrationBuilder.DropColumn(
                name: "MaxEnhancementLevel",
                schema: "game_data",
                table: "weapon_data");

            migrationBuilder.DropColumn(
                name: "SynthesizeRequiredCount",
                schema: "game_data",
                table: "weapon_data");

            migrationBuilder.DropColumn(
                name: "SynthesizeResultWeaponId",
                schema: "game_data",
                table: "weapon_data");
        }
    }
}
