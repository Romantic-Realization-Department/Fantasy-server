using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fantasy.Server.Migrations
{
    /// <inheritdoc />
    public partial class DungeonSystemSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "game_data");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCalculatedAt",
                schema: "player",
                table: "player_stage",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<int>(
                name: "SmithGrade",
                schema: "player",
                table: "player_resource",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "job_base_stat",
                schema: "game_data",
                columns: table => new
                {
                    JobType = table.Column<string>(type: "text", nullable: false),
                    BaseHp = table.Column<long>(type: "bigint", nullable: false),
                    BaseAtk = table.Column<long>(type: "bigint", nullable: false),
                    CritRate = table.Column<double>(type: "double precision", nullable: false),
                    CritDmgMultiplier = table.Column<double>(type: "double precision", nullable: false),
                    HpPerLevel = table.Column<double>(type: "double precision", nullable: false),
                    AtkPerLevel = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_base_stat", x => x.JobType);
                });

            migrationBuilder.CreateTable(
                name: "level_table",
                schema: "game_data",
                columns: table => new
                {
                    Level = table.Column<long>(type: "bigint", nullable: false),
                    RequiredExp = table.Column<long>(type: "bigint", nullable: false),
                    RewardSp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_level_table", x => x.Level);
                });

            migrationBuilder.CreateTable(
                name: "skill_data",
                schema: "game_data",
                columns: table => new
                {
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    JobType = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SpCost = table.Column<long>(type: "bigint", nullable: false),
                    PrereqSkillId = table.Column<int>(type: "integer", nullable: true),
                    EffectType = table.Column<string>(type: "text", nullable: false),
                    EffectValue = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_data", x => x.SkillId);
                });

            migrationBuilder.CreateTable(
                name: "stage_data",
                schema: "game_data",
                columns: table => new
                {
                    Stage = table.Column<long>(type: "bigint", nullable: false),
                    MonsterHp = table.Column<long>(type: "bigint", nullable: false),
                    MonsterAtk = table.Column<long>(type: "bigint", nullable: false),
                    XpPerSecond = table.Column<long>(type: "bigint", nullable: false),
                    GoldPerSecond = table.Column<long>(type: "bigint", nullable: false),
                    IsBossStage = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stage_data", x => x.Stage);
                });

            migrationBuilder.CreateTable(
                name: "weapon_data",
                schema: "game_data",
                columns: table => new
                {
                    WeaponId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Grade = table.Column<string>(type: "text", nullable: false),
                    JobType = table.Column<string>(type: "text", nullable: false),
                    BaseAtk = table.Column<long>(type: "bigint", nullable: false),
                    AtkPerEnhancement = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weapon_data", x => x.WeaponId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_base_stat",
                schema: "game_data");

            migrationBuilder.DropTable(
                name: "level_table",
                schema: "game_data");

            migrationBuilder.DropTable(
                name: "skill_data",
                schema: "game_data");

            migrationBuilder.DropTable(
                name: "stage_data",
                schema: "game_data");

            migrationBuilder.DropTable(
                name: "weapon_data",
                schema: "game_data");

            migrationBuilder.DropColumn(
                name: "LastCalculatedAt",
                schema: "player",
                table: "player_stage");

            migrationBuilder.DropColumn(
                name: "SmithGrade",
                schema: "player",
                table: "player_resource");
        }
    }
}
